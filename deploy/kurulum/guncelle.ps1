# Disci güncellemesi (Windows). Kurulum bunu SYSTEM hesabıyla 10 dakikada bir çalışan "Disci Guncelleme" görevine bağlar.
#
# Ne zaman kurar:
#   - Program "Güncelleme" sayfasında istek bıraktıysa (Guncelleme\istek.json): istenen sürümü,
#   - "otomatik güncelle" açıksa (Guncelleme\ayar.json): gece 03:00-05:00 arasında en yeni sürümü,
#   - elle: guncelle.ps1 -Surum v1.2.0   (ya da -Surum son)
# Sürümü her zaman kendisi, sabit GitHub adresinden indirir; programdan yalnızca sürüm adını alır. İndirilen paketin
# SHA256 özeti, aynı sürümdeki SHA256SUMS.txt dosyasıyla karşılaştırılır. Önce yedek alınır; yeni sürüm açılmazsa eski
# sürüme dönülür. Sonuç Guncelleme\durum.json (programın Güncelleme sayfası gösterir) ve Guncelleme\guncelle.log'a yazılır.
param(
    [string]$Hedef = 'C:\Disci',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [string]$Surum = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$klasor = Join-Path $VeriKlasoru 'Guncelleme'
$istekDosyasi = Join-Path $klasor 'istek.json'
$ayarDosyasi = Join-Path $klasor 'ayar.json'
$durumDosyasi = Join-Path $klasor 'durum.json'
$sonDenetim = Join-Path $klasor 'son-otomatik-denetim.txt'
$log = Join-Path $klasor 'guncelle.log'
$uygulama = Join-Path $Hedef 'app'
$onceki = Join-Path $Hedef 'app.onceki'
$utf8 = New-Object Text.UTF8Encoding $false
New-Item -ItemType Directory -Force -Path $klasor | Out-Null

function Kaydet([string]$metin) {
    $satir = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $metin"
    [IO.File]::AppendAllText($log, $satir + "`r`n", $utf8)
    Write-Host $satir
}

function Durum([string]$durum, [string]$mesaj, [string]$surum) {
    $json = [ordered]@{ state = $durum; message = $mesaj; version = $surum; time = (Get-Date).ToString('o') } | ConvertTo-Json
    [IO.File]::WriteAllText($durumDosyasi, $json, $utf8)
    Kaydet "[$durum] $mesaj"
}

function Surum-Coz([string]$metin) {
    if ($metin -match '^v?(\d+)\.(\d+)\.(\d+)') { return [version]"$($Matches[1]).$($Matches[2]).$($Matches[3])" }
    return $null
}

function Kurulu-Surum {
    $exe = Join-Path $uygulama 'Disci.Web.exe'
    if (-not (Test-Path $exe)) { return [version]'0.0.0' }
    $v = Surum-Coz (Get-Item $exe).VersionInfo.ProductVersion
    if ($v) { $v } else { [version]'0.0.0' }
}

function GitHub([string]$yol) {
    Invoke-RestMethod -Uri "https://api.github.com/repos/$($Script:Depo)/$yol" -Headers @{ 'User-Agent' = 'Disci-Updater'; Accept = 'application/vnd.github+json' } -TimeoutSec 30
}

function Http-Port {
    try {
        $ayar = Get-Content (Join-Path $uygulama 'appsettings.Production.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($ayar.Kestrel.Endpoints.Http.Url -match ':(\d+)\s*$') { return [int]$Matches[1] }
    } catch { }
    return 5111
}

# ---- Hangi sürüm kurulacak? ---------------------------------------------------
$etiket = $null
$isteyen = 'elle'
if ($Surum) {
    $etiket = $Surum
} elseif (Test-Path $istekDosyasi) {
    try { $istek = Get-Content $istekDosyasi -Raw -Encoding UTF8 | ConvertFrom-Json } catch { $istek = $null }
    Remove-Item $istekDosyasi -Force
    if (-not $istek -or -not $istek.tag) { Durum 'hata' 'Güncelleme isteği okunamadı.' $null; exit 1 }
    $etiket = [string]$istek.tag
    $isteyen = [string]$istek.requestedBy
} else {
    $otomatik = $false
    try { $otomatik = [bool](Get-Content $ayarDosyasi -Raw -Encoding UTF8 | ConvertFrom-Json).automatic } catch { }
    $saat = (Get-Date).Hour
    $sonZaman = if (Test-Path $sonDenetim) { (Get-Item $sonDenetim).LastWriteTime } else { [datetime]::MinValue }
    if (-not $otomatik -or $saat -lt 3 -or $saat -ge 5 -or $sonZaman -gt (Get-Date).AddHours(-20)) { exit 0 }
    [IO.File]::WriteAllText($sonDenetim, (Get-Date).ToString('o'), $utf8)
    $etiket = 'son'
    $isteyen = 'otomatik'
}

try {
    $kurulu = Kurulu-Surum
    $surumBilgisi = if ($etiket -eq 'son') { GitHub 'releases/latest' } else {
        if ($etiket -notmatch '^v?\d+\.\d+\.\d+$') { throw "Geçersiz sürüm adı: $etiket" }
        if (-not $etiket.StartsWith('v')) { $etiket = "v$etiket" }
        GitHub "releases/tags/$etiket"
    }
    $yeni = Surum-Coz $surumBilgisi.tag_name
    if (-not $yeni) { throw "GitHub'daki sürüm adı anlaşılamadı: $($surumBilgisi.tag_name)" }
    if ($isteyen -eq 'otomatik' -and $yeni -le $kurulu) { Kaydet "Otomatik denetim: kurulu $kurulu, en yeni $yeni; güncelleme yok."; exit 0 }
    if ($yeni -eq $kurulu) { Durum 'tamam' "Disci $kurulu zaten kurulu." "$kurulu"; exit 0 }

    $paketVarligi = $surumBilgisi.assets | Where-Object { $_.name -like 'Disci-Kurulum-*-win-x64.zip' -and $_.name -notlike '*VERILI*' } | Select-Object -First 1
    $ozetVarligi = $surumBilgisi.assets | Where-Object { $_.name -eq 'SHA256SUMS.txt' } | Select-Object -First 1
    if (-not $paketVarligi) { throw "$($surumBilgisi.tag_name) sürümünde Windows paketi (Disci-Kurulum-*-win-x64.zip) yok." }
    if (-not $ozetVarligi) { throw "$($surumBilgisi.tag_name) sürümünde SHA256SUMS.txt yok; paket doğrulanamayacağı için kurulmadı." }

    # ---- İndir ve doğrula --------------------------------------------------------
    Durum 'indiriliyor' "Disci $yeni indiriliyor ($isteyen istedi)." "$yeni"
    $indir = Join-Path $klasor 'indir'
    if (Test-Path $indir) { Remove-Item $indir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $indir | Out-Null
    $zip = Join-Path $indir $paketVarligi.name
    $ozetler = Join-Path $indir 'SHA256SUMS.txt'
    $eskiTercih = $ProgressPreference; $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $paketVarligi.browser_download_url -OutFile $zip -UseBasicParsing -Headers @{ 'User-Agent' = 'Disci-Updater' }
    Invoke-WebRequest -Uri $ozetVarligi.browser_download_url -OutFile $ozetler -UseBasicParsing -Headers @{ 'User-Agent' = 'Disci-Updater' }
    $ProgressPreference = $eskiTercih
    $beklenen = (Get-Content $ozetler | Where-Object { $_ -match "^\s*([0-9a-fA-F]{64})\s+\*?$([regex]::Escape($paketVarligi.name))\s*$" } | ForEach-Object { $Matches[1] }) | Select-Object -First 1
    $gercek = (Get-FileHash $zip -Algorithm SHA256).Hash
    if (-not $beklenen -or $beklenen -ne $gercek) { throw "İndirilen paketin SHA256 özeti tutmuyor; paket kurulmadı (beklenen $beklenen, gelen $gercek)." }
    $acik = Join-Path $indir 'paket'
    Expand-Archive -Path $zip -DestinationPath $acik -Force
    if (-not (Test-Path (Join-Path $acik 'app\Disci.Web.exe'))) { throw 'Paketin içinde app\Disci.Web.exe yok.' }

    # ---- Kur --------------------------------------------------------------------
    Durum 'kuruluyor' "Disci $kurulu → $yeni kuruluyor. Önce yedek alınıyor." "$yeni"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'yedekle.ps1') -YedekKlasoru (Join-Path $VeriKlasoru 'Yedekler') -DosyaKlasoru (Join-Path $VeriKlasoru 'files')
    if ($LASTEXITCODE -ne 0) { throw 'Yedek alınamadı; güvenlik için güncelleme yapılmadı. Ayrıntı: Yedekler\yedek.log' }

    Stop-Service $Script:ServisAdi -Force
    if (Test-Path $onceki) { Remove-Item $onceki -Recurse -Force }
    Rename-Item $uygulama $onceki
    robocopy (Join-Path $acik 'app') $uygulama /E /R:2 /W:2 /NFL /NDL /NP /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Program dosyaları kopyalanamadı (robocopy $LASTEXITCODE)." }
    Copy-Item (Join-Path $onceki 'appsettings.Production.json') $uygulama -Force
    Ayar-Dosyasini-Koru (Join-Path $uygulama 'appsettings.Production.json')
    # 1.3.0 ve öncesinin ayrı araç klasörü ve betiği artık kullanılmıyor (parola sıfırlama programın içinde).
    Remove-Item (Join-Path $Hedef 'import') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $Hedef 'betikler\veri-aktar.ps1') -Force -ErrorAction SilentlyContinue
    foreach ($b in 'ortak.ps1', 'yedekle.ps1', 'geri-yukle.ps1', 'parola-sifirla.ps1', 'kaldir.ps1', 'guncelle.ps1', 'veritabani-ayarla.ps1', 'OKUBENI.md', 'disci.ico') {
        $kaynak = Join-Path $acik $b
        if (Test-Path $kaynak) { Copy-Item $kaynak (Join-Path $Hedef 'betikler') -Force }
    }

    Start-Service $Script:ServisAdi
    if (Program-Yanit-Veriyor-Mu (Http-Port)) {
        Durum 'tamam' "Disci $yeni kuruldu (önceki sürüm $kurulu, $isteyen istedi)." "$yeni"
        Remove-Item $indir -Recurse -Force -ErrorAction SilentlyContinue
        exit 0
    }

    # ---- Yeni sürüm açılmadı: geri dön -------------------------------------------
    Kaydet 'Yeni sürüm yanıt vermedi; eski sürüme dönülüyor.'
    Stop-Service $Script:ServisAdi -Force -ErrorAction SilentlyContinue
    Remove-Item $uygulama -Recurse -Force
    Rename-Item $onceki $uygulama
    Start-Service $Script:ServisAdi
    Durum 'hata' "Disci $yeni açılmadı; $kurulu sürümüne geri dönüldü. Güncelleme öncesi yedek: $VeriKlasoru\Yedekler. Olay Görüntüleyicisi'nde 'Disci' kayıtlarına bakın." "$kurulu"
    exit 1
} catch {
    Durum 'hata' $_.Exception.Message "$(Kurulu-Surum)"
    # Program durdurulduysa yeniden başlatılır (eski sürüm yerindeyse).
    if (-not (Test-Path $uygulama) -and (Test-Path $onceki)) { Rename-Item $onceki $uygulama }
    if ((Get-Service $Script:ServisAdi -ErrorAction SilentlyContinue).Status -ne 'Running') { Start-Service $Script:ServisAdi -ErrorAction SilentlyContinue }
    exit 1
}
