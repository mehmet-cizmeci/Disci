# Disci'nin veritabanı bağlantısını değiştirir: bu bilgisayardaki ya da başka bilgisayardaki bir SQL Server, Windows
# kimlik doğrulaması ya da SQL kullanıcı adı/parolası, veritabanı adı. İsterseniz mevcut verileri yeni yere taşır.
# Kullanım (yönetici PowerShell):  C:\Disci\betikler\veritabani-ayarla.ps1
param(
    [string]$Hedef = 'C:\Disci',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [int]$HttpPort = 5111
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')
$ayarDosyasi = Join-Path $Hedef 'app\appsettings.Production.json'
$yedekler = Join-Path $VeriKlasoru 'Yedekler'

try {
    if (-not (Yonetici-Mi)) { throw 'Bu betik yönetici olarak çalıştırılmalı (PowerShell''e sağ tık > Yönetici olarak çalıştır).' }
    if (-not (Test-Path $ayarDosyasi)) { throw "Disci kurulu görünmüyor: $ayarDosyasi yok." }
    $ayar = Get-Content $ayarDosyasi -Raw -Encoding UTF8 | ConvertFrom-Json
    Baglanti-Ayarla $ayar.ConnectionStrings.DefaultConnection

    Baslik 'Şu anki veritabanı bağlantısı'
    Bilgi "Sunucu      : $($Script:SqlOrnek)"
    Bilgi "Kimlik      : $(Sql-Kimlik-Metni)"
    Bilgi "Veritabanı  : $($Script:VeritabaniAdi)"
    $eski = @{ Ornek = $Script:SqlOrnek; Kullanici = $Script:SqlKullanici; Parola = $Script:SqlParola; Veritabani = $Script:VeritabaniAdi }
    $eskiVeriVar = $false
    try { $eskiVeriVar = (Hasta-Sayisi) -gt 0 -or (Kullanici-Sayisi) -gt 0 } catch { Uyari "Şu anki veritabanına bağlanılamadı: $($_.Exception.Message)" }

    Baslik 'Yeni bağlantı'
    [void](Sql-Baglantisi-Sor)
    Sql-Baglantisini-Dogrula -SorabilirMi
    $yeniServis = Sql-Servis-Adi
    $ayni = $Script:SqlOrnek -eq $eski.Ornek -and $Script:VeritabaniAdi -eq $eski.Veritabani

    $sunucuYedek = ''
    if (-not $yeniServis) {
        $sunucuYedek = Sunucu-Varsayilan-Yedek-Klasoru
        $k = Read-Host "   Günlük veritabanı yedeği SUNUCUNUN hangi klasörüne yazılsın? [$sunucuYedek]"
        if (-not [string]::IsNullOrWhiteSpace($k)) { $sunucuYedek = $k.Trim() }
    }

    # ---- Verileri taşıma -------------------------------------------------------
    $yeniVeriVar = (Veritabani-Var-Mi) -and ((Hasta-Sayisi) -gt 0)
    if (-not $ayni -and $eskiVeriVar) {
        if ($yeniVeriVar) {
            Uyari "Yeni veritabanında zaten kayıt var; veriler taşınmayacak, program bundan sonra o veritabanını kullanacak."
        } elseif ($yeniServis -and (Evet-Mi 'Mevcut veriler (hastalar, kullanıcılar...) yeni veritabanına taşınsın mı?' $true)) {
            Baslik 'Veriler taşınıyor'
            Stop-Service $Script:ServisAdi -Force -ErrorAction SilentlyContinue
            New-Item -ItemType Directory -Force -Path $yedekler | Out-Null
            icacls $yedekler /grant "NT SERVICE\$($yeniServis):(OI)(CI)M" /C /Q | Out-Null
            $yeni = @{ Ornek = $Script:SqlOrnek; Kullanici = $Script:SqlKullanici; Parola = $Script:SqlParola; Veritabani = $Script:VeritabaniAdi }
            $tasima = Join-Path $yedekler "Disci_TASIMA_$(Get-Date -Format 'yyyyMMdd_HHmm').bak"

            # Eski sunucudan yedek (eski sunucu başka bilgisayardaysa bu klasöre yazamaz: o durumda yedeği elle taşıyın).
            $Script:SqlOrnek = $eski.Ornek; $Script:SqlKullanici = $eski.Kullanici; $Script:SqlParola = $eski.Parola; $Script:VeritabaniAdi = $eski.Veritabani
            Sql-Calistir "BACKUP DATABASE [$($eski.Veritabani)] TO DISK = N'$tasima' WITH INIT, CHECKSUM, COPY_ONLY"
            Tamam "Eski veritabanının yedeği alındı: $tasima"

            $Script:SqlOrnek = $yeni.Ornek; $Script:SqlKullanici = $yeni.Kullanici; $Script:SqlParola = $yeni.Parola; $Script:VeritabaniAdi = $yeni.Veritabani
            $veriYolu = [string](Sql-Calistir "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000))" -Skaler)
            $logYolu = [string](Sql-Calistir "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000))" -Skaler)
            $tasi = @()
            $baglanti = New-Object System.Data.SqlClient.SqlConnection (Baglanti-Metni 'master' -Mars:$false)
            try {
                $baglanti.Open()
                $komut = $baglanti.CreateCommand()
                $komut.CommandText = "RESTORE FILELISTONLY FROM DISK = N'$tasima'"
                $okuyucu = $komut.ExecuteReader()
                while ($okuyucu.Read()) {
                    $mantiksal = $okuyucu['LogicalName']; $tur = $okuyucu['Type']
                    $yol = if ($tur -eq 'L') { Join-Path $logYolu "$($yeni.Veritabani)_log.ldf" } else { Join-Path $veriYolu "$($yeni.Veritabani).mdf" }
                    $tasi += "MOVE N'$mantiksal' TO N'$yol'"
                }
                $okuyucu.Close()
            } finally { $baglanti.Dispose() }
            Sql-Calistir "RESTORE DATABASE [$($yeni.Veritabani)] FROM DISK = N'$tasima' WITH REPLACE, CHECKSUM, $($tasi -join ', ')"
            Tamam 'Veriler yeni veritabanına taşındı. Eski veritabanı silinmedi.'
        } elseif (-not $yeniServis) {
            Uyari 'Yeni sunucu başka bir bilgisayarda: veriler kendiliğinden taşınamaz. Taşımak için önce yedek alın'
            Uyari "(yedekle.ps1), .bak dosyasını sunucuya kopyalayın ve bu bağlantıyla geri-yukle.ps1 çalıştırın."
            if (-not (Evet-Mi 'Boş veritabanıyla devam edilsin mi? (Program ilk açılışta kurulum formunu gösterir)' $false)) { exit 1 }
        }
    }

    # ---- Ayarları yaz -----------------------------------------------------------
    Baslik 'Ayarlar kaydediliyor'
    Veritabani-Yetkileri
    $ayar.ConnectionStrings.DefaultConnection = Baglanti-Metni
    if (-not $ayar.PSObject.Properties['Backup']) { $ayar | Add-Member -NotePropertyName Backup -NotePropertyValue ([pscustomobject]@{ ServerFolder = '' }) }
    $ayar.Backup.ServerFolder = $sunucuYedek
    [IO.File]::WriteAllText($ayarDosyasi, ($ayar | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
    Ayar-Dosyasini-Koru $ayarDosyasi
    $bagimlilik = if ($yeniServis) { $yeniServis } else { '/' }
    sc.exe config $Script:ServisAdi depend= $bagimlilik | Out-Null
    if ($yeniServis) { icacls $yedekler /grant "NT SERVICE\$($yeniServis):(OI)(CI)M" /C /Q | Out-Null }
    Tamam "Kaydedildi: $($Script:SqlOrnek), $(Sql-Kimlik-Metni), veritabanı '$($Script:VeritabaniAdi)'."

    Baslik 'Program yeniden başlatılıyor'
    Restart-Service $Script:ServisAdi -Force
    if (-not (Program-Yanit-Veriyor-Mu $HttpPort)) { throw 'Program yanıt vermiyor; Olay Görüntüleyicisi > Uygulama altındaki "Disci" kayıtlarına bakın.' }
    Tamam 'Program yeni veritabanıyla çalışıyor.'
    exit 0
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
