# Geliştirme bilgisayarında çalışır: Disci'yi derler ve klinik bilgisayarına götürülecek kurulum paketini hazırlar.
#
# Genel paket (GitHub'a konan, veri içermez):
#   powershell -ExecutionPolicy Bypass -File deploy\paket-olustur.ps1
param(
    [string]$Cikti = (Join-Path (Split-Path $PSScriptRoot -Parent) 'dist'),
    [switch]$TestleriAtla,
    # SQL Server Express çevrimdışı dosyası (deploy\sql-express, sql-indir.ps1 indirir) varsa ayrıca SQL'li paket de yapılır; bunu kapatır.
    [switch]$SqlsizPaket,
    # Linux ve macOS paketi de yapılsın (Docker ile çalışır; deploy\unix).
    [switch]$Unix,
    # Kod imzalama sertifikası (docs\IMZALAMA.md) yoksa paketi imzasız bırakmak yerine hata ver.
    [switch]$ImzaZorunlu
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

Adim 'Kurulum betikleri kopyalanıyor'
Copy-Item (Join-Path $PSScriptRoot 'kurulum\*') $paket -Recurse -Force
foreach ($belge in 'Kurulum-Rehberi.md', 'Kullanim-Kilavuzu.pdf', 'Kullanim-Kilavuzu.md') {
    Copy-Item (Join-Path $depo "docs\$belge") $paket -Force
}
Copy-Item (Join-Path $depo 'LICENSE') (Join-Path $paket 'LISANS.txt') -Force

# Kendi programlarımız ve kurulum betikleri imzalanır (Microsoft DLL'leri zaten imzalı). Sertifika yoksa
# imzala.ps1 uyarıp imzasız bırakır; -ImzaZorunlu ile hata verir. Ayrıntı: docs\IMZALAMA.md
Adim 'Kod imzalama'
$imzalanacak = @((Join-Path $paket 'app\Disci.Web.exe'), (Join-Path $paket '*.ps1'))
$imzaArg = @{ Aciklama = 'Disci'; Adres = 'https://github.com/mehmet-cizmeci/Disci' }
if ($ImzaZorunlu) { $imzaArg.Zorunlu = $true }
& (Join-Path $PSScriptRoot 'imzala.ps1') @imzaArg @imzalanacak
if ($LASTEXITCODE -ne 0) { throw "Kod imzalama başarısız (çıkış kodu $LASTEXITCODE)." }

$surum = ([xml](Get-Content (Join-Path $depo 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
if (-not $surum) { throw 'Directory.Build.props içinde <Version> yok.' }
$uretilen = @()
# Güncelleme görevi tam olarak bu adı arar: Disci-Kurulum-<sürüm>-win-x64.zip (SQL'siz, küçük).
$zip = Join-Path $Cikti "Disci-Kurulum-$surum-win-x64.zip"
Adim "Zip oluşturuluyor: $zip"
Compress-Archive -Path (Join-Path $paket '*') -DestinationPath $zip -Force
$uretilen += $zip

# SQL Server Express'in internetsiz kurulum dosyası varsa ayrıca SQL'li paket (ilk kurulum için; ~700 MB büyük).
$sqlDosyasi = Get-ChildItem (Join-Path $PSScriptRoot 'sql-express') -Filter 'SQLEXPR_x64_*.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $SqlsizPaket -and $sqlDosyasi) {
    Adim "SQL Server Express dosyası ekleniyor: $($sqlDosyasi.Name)"
    New-Item -ItemType Directory -Force -Path (Join-Path $paket 'sql') | Out-Null
    Copy-Item $sqlDosyasi.FullName (Join-Path $paket 'sql') -Force
    $sqlZip = Join-Path $Cikti "Disci-Kurulum-$surum-win-x64-SQL.zip"
    Compress-Archive -Path (Join-Path $paket '*') -DestinationPath $sqlZip -Force
    $uretilen += $sqlZip
} elseif (-not $SqlsizPaket) {
    Write-Host '   SQL Server çevrimdışı dosyası yok; yalnızca SQL''siz paket yapıldı (SQL internetten kurulur). İndirmek için: deploy\sql-indir.ps1' -ForegroundColor Yellow
}

if ($Unix) {
    # Linux/macOS: programın her işlemcide çalışan (framework-dependent) hali Docker içinde .NET 10 ile çalışır.
    Adim 'Linux ve macOS paketi'
    $unixPaket = Join-Path $Cikti 'Disci-Kurulum-unix'
    if (Test-Path $unixPaket) { Remove-Item $unixPaket -Recurse -Force }
    Calistir 'Linux/macOS derlemesi' {
        dotnet publish (Join-Path $depo 'src\Disci.Web') -c Release --self-contained false -o (Join-Path $unixPaket 'app') --nologo -v quiet
    }
    Remove-Item (Join-Path $unixPaket 'app\appsettings.Development.json') -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $PSScriptRoot 'unix\*') $unixPaket -Recurse -Force
    foreach ($belge in 'Kurulum-Linux.md', 'Kurulum-macOS.md', 'Kullanim-Kilavuzu.pdf') { Copy-Item (Join-Path $depo "docs\$belge") $unixPaket -Force }
    Copy-Item (Join-Path $depo 'LICENSE') (Join-Path $unixPaket 'LISANS.txt') -Force
    [IO.File]::WriteAllText((Join-Path $unixPaket 'SURUM'), $surum, (New-Object Text.UTF8Encoding $false))
    $tar = Join-Path $Cikti "Disci-Kurulum-$surum-linux-macos.tar.gz"
    # Windows 10+ tar (bsdtar). Betiklerin çalıştırma izni kur.sh içinde yeniden verilir.
    Push-Location $Cikti
    try { tar -czf $tar 'Disci-Kurulum-unix' } finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw "tar başarısız ($LASTEXITCODE)." }
    $uretilen += $tar
}

# Güncelleme görevi indirdiği paketi bu listeyle doğrular; GitHub sürümüne paketlerle birlikte yüklenmelidir.
$ozet = Join-Path $Cikti 'SHA256SUMS.txt'
$satirlar = $uretilen | ForEach-Object { "$((Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant())  $(Split-Path $_ -Leaf)" }
[IO.File]::WriteAllLines($ozet, [string[]]$satirlar, (New-Object Text.UTF8Encoding $false))
$uretilen += $ozet

Write-Host ''
foreach ($f in $uretilen) { Write-Host "Hazır ($([math]::Round((Get-Item $f).Length / 1MB, 1)) MB): $f" -ForegroundColor Green }
Write-Host 'Windows: zip dosyasını klinik bilgisayarına kopyalayıp açın ve KUR.bat dosyasını çalıştırın.'
# SQL'li paket GitHub'a yüklenmez (Microsoft'un dosyası); yalnızca USB ile kliniğe götürülür.
$yayin = $uretilen | Where-Object { $_ -notlike '*-SQL.zip' }
Write-Host "GitHub sürümü (SQL'li paket hariç): gh release create v$surum $($yayin -join ' ') --title ""Disci $surum"" --notes-file <notlar.md>"
