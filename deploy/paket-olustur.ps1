# Geliştirme bilgisayarında çalışır: Disci'yi derler ve klinik bilgisayarına götürülecek kurulum paketini hazırlar.
# Kullanım (depo klasöründe): powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1
param(
    [string]$Cikti = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist'),
    [switch]$TestleriAtla
)

$ErrorActionPreference = 'Stop'
$depo = Split-Path $PSScriptRoot -Parent
$paket = Join-Path $Cikti 'Disci-Kurulum'

function Adim([string]$metin) { Write-Host ''; Write-Host "== $metin" -ForegroundColor Cyan }
function Calistir([string]$aciklama, [scriptblock]$komut) {
    & $komut
    if ($LASTEXITCODE -ne 0) { throw "$aciklama başarısız (çıkış kodu $LASTEXITCODE)." }
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

$surum = Get-Date -Format 'yyyyMMdd-HHmm'
$zip = Join-Path $Cikti "Disci-Kurulum-$surum.zip"
Adim "Zip oluşturuluyor: $zip"
Compress-Archive -Path (Join-Path $paket '*') -DestinationPath $zip -Force

$boyut = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ''
Write-Host "Paket hazır ($boyut MB): $zip" -ForegroundColor Green
Write-Host 'Bu zip dosyasını klinik bilgisayarına kopyalayıp açın ve KUR.bat dosyasını çalıştırın.'
