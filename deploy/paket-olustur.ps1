# Geliştirme bilgisayarında çalışır: Disci'yi derler ve klinik bilgisayarına götürülecek kurulum paketini hazırlar.
#
# Genel paket (GitHub'a konan, veri içermez):
#   powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1
#
# Bir kliniğe özel, eski DentaLine verilerini İÇEREN paket (kurulum bunları kendiliğinden aktarır).
# GİZLİDİR: hasta verisi içerir; GitHub'a veya başka bir yere yüklenmez, yalnızca USB bellekle götürülür.
#   powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1 -EskiVeri "C:\...\DL2500DB" -Dl2001Veri "C:\...\DL2001\DB"
param(
    [string]$Cikti = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist'),
    [string]$EskiVeri = '',
    [string]$Dl2001Veri = '',
    [switch]$TestleriAtla
)

$ErrorActionPreference = 'Stop'
$depo = Split-Path $PSScriptRoot -Parent
$verili = [bool]$EskiVeri
$paket = Join-Path $Cikti $(if ($verili) { 'Disci-Kurulum-Verili' } else { 'Disci-Kurulum' })

function Adim([string]$metin) { Write-Host ''; Write-Host "== $metin" -ForegroundColor Cyan }
function Calistir([string]$aciklama, [scriptblock]$komut) {
    & $komut
    if ($LASTEXITCODE -ne 0) { throw "$aciklama başarısız (çıkış kodu $LASTEXITCODE)." }
}

if ($verili) {
    if (-not (Test-Path (Join-Path $EskiVeri 'DLHASTA.DB'))) { throw "DentaLineSuit klasöründe DLHASTA.DB yok: $EskiVeri" }
    if ($Dl2001Veri -and -not (Test-Path (Join-Path $Dl2001Veri 'DLHASTA.DB'))) { throw "DentaLine 2001 klasöründe DLHASTA.DB yok: $Dl2001Veri" }
    # Hasta verisi içeren paket asla Git'in göreceği bir yere yazılmamalı.
    $tamCikti = [IO.Path]::GetFullPath($Cikti)
    if ($tamCikti.StartsWith([IO.Path]::GetFullPath($depo), [StringComparison]::OrdinalIgnoreCase)) {
        Push-Location $depo
        try { git check-ignore -q (Join-Path $tamCikti 'deneme.zip') 2>$null; $gizli = $LASTEXITCODE -eq 0 } finally { Pop-Location }
        if (-not $gizli) { throw "Çıktı klasörü Git tarafından yok sayılmıyor: $Cikti. Verili paket bu klasöre yazılamaz." }
    }
    foreach ($p in 'DentaLineSuit', 'DentaLine2001') {
        if (Get-Process $p -ErrorAction SilentlyContinue) { throw "$p açık. Verilerin tutarlı kopyalanması için programı kapatın." }
    }
}

if (-not $TestleriAtla) {
    Adim 'Testler'
    Calistir 'Testler' { dotnet test (Join-Path $depo 'tests\Disci.Web.Tests') --nologo -v quiet }
}

if (Test-Path $paket) { Remove-Item $paket -Recurse -Force }

# Hedef bilgisayara .NET kurulmasın diye program kendi çalışma zamanıyla (self-contained) yayınlanır.
Adim 'Web uygulaması derleniyor'
Calistir 'Web uygulaması derlemesi' {
    dotnet publish (Join-Path $depo 'src\Disci.Web') -c Release -r win-x64 --self-contained true -o (Join-Path $paket 'app') --nologo -v quiet
}
Remove-Item (Join-Path $paket 'app\appsettings.Development.json') -ErrorAction SilentlyContinue

Adim 'Aktarım aracı derleniyor'
Calistir 'Aktarım aracı derlemesi' {
    dotnet publish (Join-Path $depo 'tools\Disci.LegacyImport') -c Release -r win-x64 --self-contained true -o (Join-Path $paket 'import') --nologo -v quiet
}

Adim 'Kurulum betikleri kopyalanıyor'
Copy-Item (Join-Path $PSScriptRoot 'kurulum\*') $paket -Recurse -Force
foreach ($belge in 'Kurulum-Rehberi.md', 'Kullanim-Kilavuzu.pdf', 'Kullanim-Kilavuzu.md') {
    Copy-Item (Join-Path $depo "docs\$belge") $paket -Force
}
Copy-Item (Join-Path $depo 'LICENSE') (Join-Path $paket 'LISANS.txt') -Force

if ($verili) {
    # kur.ps1 paketin içindeki eski-veri klasörünü görürse soru sormadan aktarır.
    Adim 'Eski DentaLine verileri pakete ekleniyor (GİZLİ: hasta verisi)'
    $eski = Join-Path $paket 'eski-veri'
    robocopy $EskiVeri (Join-Path $eski 'DentaLineSuit') /E /R:2 /W:2 /NFL /NDL /NP /NJH /NJS /XF '*.lck' 'PDOXUSRS.*' | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "DentaLineSuit verisi kopyalanamadı (robocopy $LASTEXITCODE)" }
    if ($Dl2001Veri) {
        robocopy $Dl2001Veri (Join-Path $eski 'DentaLine2001') /E /R:2 /W:2 /NFL /NDL /NP /NJH /NJS /XF '*.lck' 'PDOXUSRS.*' | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "DentaLine 2001 verisi kopyalanamadı (robocopy $LASTEXITCODE)" }
    }
    $not = "Bu klasörde gerçek hasta verisi vardır. Kurulumdan sonra USB bellekten silin; kimseyle paylaşmayın.`r`n" +
        "Kopyalanma: $(Get-Date -Format 'dd.MM.yyyy HH:mm')`r`nDentaLineSuit: $EskiVeri`r`nDentaLine 2001: $Dl2001Veri`r`n"
    [IO.File]::WriteAllText((Join-Path $eski 'GIZLI-HASTA-VERISI.txt'), $not, (New-Object Text.UTF8Encoding $true))
}

$surum = Get-Date -Format 'yyyyMMdd-HHmm'
$zip = Join-Path $Cikti $(if ($verili) { "Disci-Kurulum-VERILI-GIZLI-$surum.zip" } else { "Disci-Kurulum-$surum.zip" })
Adim "Zip oluşturuluyor: $zip"
Compress-Archive -Path (Join-Path $paket '*') -DestinationPath $zip -Force

$boyut = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ''
Write-Host "Paket hazır ($boyut MB): $zip" -ForegroundColor Green
Write-Host 'Bu zip dosyasını klinik bilgisayarına kopyalayıp açın ve KUR.bat dosyasını çalıştırın.'
if ($verili) {
    Write-Host 'DİKKAT: Bu paket hasta verisi içerir. GitHub''a veya internete yüklemeyin; kurulumdan sonra USB bellekten silin.' -ForegroundColor Yellow
}
