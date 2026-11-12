<#
.SYNOPSIS
    Windows dosyalarını (exe, dll, msi, ps1) kod imzalama sertifikasıyla imzalar ve imzayı doğrular.

.DESCRIPTION
    Bulut tabanlı sertifikalarla (Certum SimplySign vb.) ve USB anahtarlarla çalışır: sertifika Windows
    sertifika deposunda (Geçerli Kullanıcı\Kişisel) göründüğü sürece signtool onu kullanır.

    Sertifika seçimi sırası:
      1. -Parmakizi parametresi
      2. KOD_IMZA_PARMAKIZI ortam değişkeni
      3. %USERPROFILE%\.imza\kod-imza.txt dosyasındaki parmak izi
      4. Depodaki geçerli, özel anahtarlı tek "Code Signing" sertifikası (birden fazlaysa Certum tercih edilir)

    Sertifika bulunamazsa: -Zorunlu verilmişse hata verir; verilmemişse uyarı yazıp dosyaları imzasız bırakır
    (böylece sertifikası olmayan katkıcılar da derleme yapabilir).

    Bu betik üç projede ortaktır (video-muzik-indirici, Disci, ...). Değiştirirseniz diğer kopyaları da güncelleyin.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\imzala.ps1 dist\VideoMuzikIndirici.exe

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\imzala.ps1 -Zorunlu "paket\app\*.exe" "paket\*.ps1"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Dosyalar,
    [string]$Parmakizi = '',
    [string]$Aciklama = '',
    [string]$Adres = '',
    [switch]$Zorunlu,
    [switch]$ZatenImzaliysaAtla
)

$ErrorActionPreference = 'Stop'
$ZamanDamgalari = @('http://time.certum.pl', 'http://timestamp.digicert.com', 'http://timestamp.sectigo.com')
$KodImzaEku = '1.3.6.1.5.5.7.3.3'

function Bilgi([string]$m) { Write-Host "[imza] $m" -ForegroundColor Cyan }
function Uyari([string]$m) { Write-Host "[imza] $m" -ForegroundColor Yellow }

function Bul-Signtool {
    $s = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($s) { return $s.Source }
    $kok = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (Test-Path $kok) {
        $aday = Get-ChildItem $kok -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\' } |
            Sort-Object { [version]($_.FullName -replace '.*\\bin\\([\d\.]+)\\.*', '$1') } -Descending |
            Select-Object -First 1
        if ($aday) { return $aday.FullName }
    }
    return $null
}

function Bul-Sertifika([string]$istenen) {
    if (-not $istenen) { $istenen = $env:KOD_IMZA_PARMAKIZI }
    $dosya = Join-Path $env:USERPROFILE '.imza\kod-imza.txt'
    if (-not $istenen -and (Test-Path $dosya)) { $istenen = (Get-Content $dosya -TotalCount 1).Trim() }
    $istenen = ($istenen -replace '\s', '').ToUpperInvariant()

    $simdi = Get-Date
    $hepsi = Get-ChildItem Cert:\CurrentUser\My | Where-Object {
        $_.HasPrivateKey -and $_.NotBefore -le $simdi -and $_.NotAfter -gt $simdi -and
        ($_.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq $KodImzaEku })
    }
    if ($istenen) {
        $c = $hepsi | Where-Object { $_.Thumbprint -eq $istenen } | Select-Object -First 1
        if (-not $c) { throw "Parmak izi $istenen olan geçerli kod imzalama sertifikası bulunamadı. SimplySign Desktop bağlı mı?" }
        return $c
    }
    $hepsi = @($hepsi)
    if ($hepsi.Count -eq 1) { return $hepsi[0] }
    if ($hepsi.Count -gt 1) {
        $certum = @($hepsi | Where-Object { $_.Issuer -match 'Certum' })
        if ($certum.Count -eq 1) { return $certum[0] }
        $liste = ($hepsi | ForEach-Object { "  $($_.Thumbprint)  $($_.Subject)" }) -join "`n"
        throw "Birden fazla kod imzalama sertifikası var; -Parmakizi ile seçin veya parmak izini $dosya dosyasına yazın:`n$liste"
    }
    return $null
}

# --- dosyaları topla
$hedefler = @()
foreach ($d in $Dosyalar) {
    $bulunan = @(Get-ChildItem -Path $d -File -ErrorAction SilentlyContinue)
    if (-not $bulunan) { throw "Dosya bulunamadı: $d" }
    $hedefler += $bulunan.FullName
}
$hedefler = @($hedefler | Select-Object -Unique)

if ($ZatenImzaliysaAtla) {
    $hedefler = @($hedefler | Where-Object { (Get-AuthenticodeSignature $_).Status -ne 'Valid' })
    if (-not $hedefler) { Bilgi 'Tüm dosyalar zaten geçerli imzalı.'; exit 0 }
}

# --- sertifika
$sertifika = Bul-Sertifika $Parmakizi
if (-not $sertifika) {
    $m = "Kod imzalama sertifikası bulunamadı (SimplySign Desktop bağlı değil veya sertifika yok). $($hedefler.Count) dosya İMZASIZ bırakıldı."
    if ($Zorunlu) { throw $m }
    Uyari $m
    exit 0
}
$signtool = Bul-Signtool
if (-not $signtool) {
    throw "signtool.exe bulunamadı. Windows SDK'yı 'Signing Tools for Desktop Apps' bileşeniyle kurun (ör. winget install Microsoft.WindowsSDK.10.0.26100)."
}

Bilgi "Sertifika : $($sertifika.Subject)"
Bilgi "Geçerlilik: $($sertifika.NotAfter.ToString('dd.MM.yyyy')) tarihine kadar"
Bilgi "Dosyalar  : $($hedefler.Count) adet"

# --- imzala (tek signtool çağrısı: bulut sertifikada tek onay yeterli olsun)
$ortak = @('sign', '/fd', 'sha256', '/td', 'sha256', '/sha1', $sertifika.Thumbprint)
if ($Aciklama) { $ortak += @('/d', $Aciklama) }
if ($Adres) { $ortak += @('/du', $Adres) }

$basarili = $false
foreach ($ts in $ZamanDamgalari) {
    & $signtool @ortak '/tr' $ts @hedefler
    if ($LASTEXITCODE -eq 0) { $basarili = $true; break }
    Uyari "Zaman damgası sunucusu başarısız oldu ($ts), sıradaki deneniyor…"
    Start-Sleep -Seconds 3
}
if (-not $basarili) { throw 'İmzalama başarısız oldu.' }

# --- doğrula
$hatali = @()
foreach ($h in $hedefler) {
    $imza = Get-AuthenticodeSignature $h
    if ($imza.Status -ne 'Valid' -or $imza.SignerCertificate.Thumbprint -ne $sertifika.Thumbprint -or -not $imza.TimeStamperCertificate) {
        $hatali += "$h ($($imza.Status): $($imza.StatusMessage))"
    }
}
if ($hatali) {
    $m = "İmza doğrulanamadı:`n  " + ($hatali -join "`n  ")
    if ($Zorunlu) { throw $m }
    Uyari $m
    exit 1
}
Bilgi "Tamam: $($hedefler.Count) dosya imzalandı, zaman damgalandı ve doğrulandı."
