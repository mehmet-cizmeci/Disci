# Geliştirme bilgisayarında bir kez çalışır: SQL Server 2022 Express'in internetsiz kurulum dosyasını (SQLEXPR_x64_ENU.exe,
# yaklaşık 700 MB) deploy\sql-express klasörüne indirir. paket-olustur.ps1 bu dosyayı bulursa kurulum paketinin sql\
# klasörüne koyar; klinik bilgisayarında kur.ps1 SQL Server'ı internet olmadan bu dosyadan kurar.
#
#   powershell -ExecutionPolicy Bypass -File deploy\sql-indir.ps1
#
# Klasör Git'e girmez (.gitignore). SQL Server Express Microsoft'un ücretsiz ama lisanslı yazılımıdır; dağıtım koşulları:
# https://go.microsoft.com/fwlink/?LinkId=2104078
param(
    [string]$Klasor = (Join-Path $PSScriptRoot 'sql-express'),
    # Microsoft'un SQL Server 2022 Express indirme yardımcısı (SSEI).
    [string]$Adres = 'https://go.microsoft.com/fwlink/p/?linkid=2216019&clcid=0x409&culture=en-us&country=us'
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Force -Path $Klasor | Out-Null

$varolan = Get-ChildItem $Klasor -Filter 'SQLEXPR_x64_*.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($varolan) { Write-Host "Zaten var: $($varolan.FullName) ($([math]::Round($varolan.Length / 1MB)) MB)" -ForegroundColor Green; exit 0 }

$yardimci = Join-Path $env:TEMP 'SQL2022-SSEI-Expr.exe'
Write-Host 'SQL Server Express indirme yardımcısı indiriliyor...'
$eski = $ProgressPreference; $ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest -Uri $Adres -OutFile $yardimci -UseBasicParsing
$ProgressPreference = $eski

$imza = Get-AuthenticodeSignature $yardimci
if ($imza.Status -ne 'Valid' -or $imza.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
    throw "İndirilen yardımcı Microsoft imzalı değil ($($imza.Status)); kullanılmadı."
}

Write-Host "Çevrimdışı kurulum dosyası indiriliyor (yaklaşık 700 MB): $Klasor"
$p = Start-Process -FilePath $yardimci -ArgumentList "/ACTION=Download /MEDIAPATH=`"$Klasor`" /MEDIATYPE=Core /QUIET" -Wait -PassThru
if ($p.ExitCode -ne 0) { throw "İndirme başarısız (çıkış kodu $($p.ExitCode))." }

$dosya = Get-ChildItem $Klasor -Filter 'SQLEXPR_x64_*.exe' | Select-Object -First 1
if (-not $dosya) { throw "İndirme bitti ama $Klasor içinde SQLEXPR_x64_*.exe yok." }
$imza = Get-AuthenticodeSignature $dosya.FullName
if ($imza.Status -ne 'Valid') { throw "$($dosya.Name) imzası geçersiz ($($imza.Status))." }
Write-Host "Hazır: $($dosya.FullName) ($([math]::Round($dosya.Length / 1MB)) MB)" -ForegroundColor Green
