# Disci'yi bir yedekten geri yükler. Önce şu anki halin güvenlik yedeğini alır.
# Kullanım (yönetici PowerShell):  C:\Disci\betikler\geri-yukle.ps1            (yedek listeden seçilir)
#                                  C:\Disci\betikler\geri-yukle.ps1 -Yedek "D:\Disci_20261104_2330.bak"
# Başka bir bilgisayardan getirilen yedek de geri yüklenebilir (bilgisayar değişimi): önce yeni bilgisayara Disci'yi kurun.
param(
    [string]$Yedek = '',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [int]$HttpPort = 5111
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
$yedekler = Join-Path $VeriKlasoru 'Yedekler'
$dosyalar = Join-Path $VeriKlasoru 'files'

try {
    if (-not (Yonetici-Mi)) { throw 'Bu betik yönetici olarak çalıştırılmalı.' }

    if (-not $Yedek) {
        $liste = @(Get-ChildItem $yedekler -Filter 'Disci_*.bak' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
        if ($liste.Count -eq 0) { throw "Yedek bulunamadı: $yedekler. -Yedek ile yedek dosyasının yolunu verin." }
        Baslik 'Yedekler (en yenisi üstte)'
        for ($i = 0; $i -lt [Math]::Min($liste.Count, 20); $i++) {
            Bilgi ("{0,2}) {1}  {2:dd.MM.yyyy HH:mm}  {3} MB" -f ($i + 1), $liste[$i].Name, $liste[$i].LastWriteTime, [math]::Round($liste[$i].Length / 1MB, 1))
        }
        $secim = Read-Host '   Geri yüklenecek yedeğin numarası [1]'
        $no = if ([string]::IsNullOrWhiteSpace($secim)) { 1 } else { [int]$secim }
        $Yedek = $liste[$no - 1].FullName
    }
    if (-not (Test-Path $Yedek)) { throw "Yedek dosyası yok: $Yedek" }

    Write-Host ''
    Write-Host "DİKKAT: Disci, '$([IO.Path]::GetFileName($Yedek))' yedeğindeki hale döndürülecek." -ForegroundColor Yellow
    Write-Host '         Bu yedekten SONRA girilen kayıtlar silinir. Şu anki hal önce ayrıca yedeklenir.' -ForegroundColor Yellow
    if (-not (Evet-Mi 'Devam edilsin mi?' $false)) { Write-Host 'Vazgeçildi.'; exit 0 }

    Baslik '1. Şu anki halin güvenlik yedeği'
    if (Veritabani-Var-Mi) {
        $guvenlik = Join-Path $yedekler "Disci_GERI-YUKLEME-ONCESI_$(Get-Date -Format 'yyyyMMdd_HHmm').bak"
        Sql-Calistir "BACKUP DATABASE [$($Script:VeritabaniAdi)] TO DISK = N'$guvenlik' WITH INIT, CHECKSUM"
        Tamam "Alındı: $guvenlik"
    }

    Baslik '2. Geri yükleme'
    Stop-Service $Script:ServisAdi -Force -ErrorAction SilentlyContinue
    # SQL Server dosyayı kendi hesabıyla okur: yedeği onun okuyabildiği klasöre kopyala.
    $kopya = $Yedek
    if ((Split-Path $Yedek -Parent) -ne $yedekler) {
        $kopya = Join-Path $yedekler ([IO.Path]::GetFileName($Yedek))
        Copy-Item $Yedek $kopya -Force
    }
    $db = $Script:VeritabaniAdi
    Sql-Calistir "IF DB_ID(N'$db') IS NOT NULL ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE"
    try {
        Sql-Calistir "RESTORE DATABASE [$db] FROM DISK = N'$kopya' WITH REPLACE, CHECKSUM"
    } finally {
        Sql-Calistir "IF DB_ID(N'$db') IS NOT NULL ALTER DATABASE [$db] SET MULTI_USER"
    }
    Tamam 'Veritabanı geri yüklendi.'

    $fotoYedek = Join-Path (Split-Path $Yedek -Parent) 'files'
    if (Test-Path $fotoYedek) {
        # Silmeden kopyalar: yedekten sonra eklenen fotoğraflar diskte kalır.
        robocopy $fotoYedek $dosyalar /E /R:2 /W:2 /NFL /NDL /NP /NJH /NJS | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Fotoğraflar kopyalanamadı (robocopy $LASTEXITCODE)" }
        Tamam 'Fotoğraflar geri kopyalandı.'
    }

    Baslik '3. Yetkiler ve başlatma'
    Servis-Veritabani-Yetkisi
    Yedek-Veritabani-Yetkisi
    Start-Service $Script:ServisAdi
    if (-not (Program-Yanit-Veriyor-Mu $HttpPort)) { throw 'Program yanıt vermiyor; Olay Görüntüleyicisi''ne bakın.' }
    Tamam 'Geri yükleme tamamlandı; program çalışıyor.'
    exit 0
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Program başlatılmamış olabilir: Hizmetler içinden "Disci Klinik Programı"nı başlatın.' -ForegroundColor Red
    exit 1
}
