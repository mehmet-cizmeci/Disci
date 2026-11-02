# Geçiş günü için: Disci'deki verileri silip DentaLineSuit'ten baştan aktarır.
# Önce bir yedek alır. Kullanıcı hesapları da silinir; sonunda ilk yönetici yeniden açılır.
# Kullanım (yönetici PowerShell): .\veri-aktar.ps1 -EskiVeri "D:\DentaLineSuit\DL2500DB"
param(
    [Parameter(Mandatory = $true)][string]$EskiVeri,
    [string]$Hedef = 'C:\Disci',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [int]$HttpPort = 5111
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')

try {
    if (-not (Yonetici-Mi)) { throw 'Bu betik yönetici olarak çalıştırılmalı.' }
    if (-not (Test-Path (Join-Path $EskiVeri 'DLHASTA.DB'))) { throw "Bu klasörde DLHASTA.DB yok: $EskiVeri" }
    $aktarimAraci = Join-Path $Hedef 'import\Disci.LegacyImport.exe'
    if (-not (Test-Path $aktarimAraci)) { throw "Aktarım aracı bulunamadı: $aktarimAraci" }

    Write-Host 'DİKKAT: Disci''deki bütün hasta kayıtları ve kullanıcı hesapları silinip eski programdan yeniden aktarılacak.' -ForegroundColor Yellow
    Write-Host '         DentaLineSuit''i kapatın; aktarım sırasında kimse kayıt girmesin.' -ForegroundColor Yellow
    if (-not (Evet-Mi 'Devam edilsin mi?' $false)) { Write-Host 'Vazgeçildi.'; exit 0 }
    $yonetici = Yonetici-Bilgisi-Sor

    Baslik '1. Güvenlik yedeği'
    & (Join-Path $PSScriptRoot 'yedekle.ps1') -YedekKlasoru (Join-Path $VeriKlasoru 'Yedekler') -DosyaKlasoru (Join-Path $VeriKlasoru 'files')
    if ($LASTEXITCODE -ne 0) { throw 'Yedek alınamadı; aktarım yapılmadı.' }

    Baslik '2. Aktarım'
    Stop-Service $Script:ServisAdi -Force
    & $aktarimAraci --source $EskiVeri --connection (Baglanti-Metni) --files (Join-Path $VeriKlasoru 'files') --reset | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Aktarım başarısız (çıkış kodu $LASTEXITCODE). Önceki veriler '$VeriKlasoru\Yedekler' içindeki son yedekte." }

    Baslik '3. Yetkiler ve başlatma'
    Servis-Veritabani-Yetkisi
    Yedek-Veritabani-Yetkisi
    Start-Service $Script:ServisAdi
    if (-not (Program-Yanit-Veriyor-Mu $HttpPort)) { throw 'Program yanıt vermiyor; Olay Görüntüleyicisi''ne bakın.' }
    Ilk-Yoneticiyi-Ac $yonetici $HttpPort
    $yonetici = $null

    Tamam "Aktarım tamamlandı. Rapor: $(Join-Path $VeriKlasoru 'reports')"
    Write-Host '   Diğer kullanıcı hesaplarını Kullanıcılar sayfasından yeniden açın.'
    exit 0
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
