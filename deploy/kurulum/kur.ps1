# Disci kurulum betiği: klinik bilgisayarına programı ve gereken her şeyi kurar.
# Kullanım: KUR.bat dosyasına çift tıklayın. Seçenekler için OKUBENI.md dosyasına bakın.
param(
    [string]$Hedef = 'C:\Disci',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [string]$EskiVeri = '',
    [string]$Dl2001Veri = '',
    [int]$HttpPort = 5111,
    [int]$HttpsPort = 5443,
    [string]$YedekSaati = '14:00',
    [switch]$YeniSertifika,
    [switch]$Deneme
)

$ErrorActionPreference = 'Stop'
$Paket = $PSScriptRoot
. (Join-Path $Paket 'ortak.ps1')
$Script:Deneme = [bool]$Deneme

$zaman = Get-Date -Format 'yyyyMMdd_HHmmss'
$logKlasoru = if ($Deneme) { $env:TEMP } else { $Paket }
Start-Transcript -Path (Join-Path $logKlasoru "kurulum_log_$zaman.txt") -Force | Out-Null

$uygulama = Join-Path $Hedef 'app'
$aktarim = Join-Path $Hedef 'import'
$betikler = Join-Path $Hedef 'betikler'
$dosyalar = Join-Path $VeriKlasoru 'files'
$anahtarlar = Join-Path $VeriKlasoru 'keys'
$yedekler = Join-Path $VeriKlasoru 'Yedekler'
$sertifikaKlasoru = Join-Path $VeriKlasoru 'sertifika'
$bilgisayar = $env:COMPUTERNAME

try {
    Write-Host 'Disci kurulumu' -ForegroundColor Green
    if ($Deneme) { Write-Host 'DENEME MODU: hiçbir değişiklik yapılmayacak.' -ForegroundColor Yellow }
    Bilgi "Program klasörü : $Hedef"
    Bilgi "Veri klasörü    : $VeriKlasoru"
    Bilgi "Adresler        : http://${bilgisayar}:$HttpPort  ve  https://${bilgisayar}:$HttpsPort"

    # ------------------------------------------------------------------
    Baslik '1. Ön kontroller'
    if (-not (Yonetici-Mi) -and -not $Deneme) { throw 'Betik yönetici olarak çalışmıyor. KUR.bat dosyasını kullanın.' }
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Disci 64 bit Windows gerektirir.' }
    foreach ($gerekli in 'app\Disci.Web.exe', 'import\Disci.LegacyImport.exe') {
        if (-not (Test-Path (Join-Path $Paket $gerekli))) {
            if ($Deneme) { Uyari "Pakette '$gerekli' yok (deneme modunda sorun değil)." }
            else { throw "Pakette '$gerekli' bulunamadı. Paketi paket-olustur.ps1 ile yeniden oluşturun." }
        }
    }
    if ($EskiVeri -and -not (Test-Path (Join-Path $EskiVeri 'DLHASTA.DB'))) {
        throw "Eski veri klasöründe DLHASTA.DB bulunamadı: $EskiVeri (DentaLineSuit\DL2500DB klasörünü gösterin)"
    }
    # SQL Server bu aşamada henüz kurulu olmayabilir: o zaman ilk kurulum sayılır.
    $kullaniciVar = $false
    if (-not $Deneme) { try { $kullaniciVar = (Kullanici-Sayisi) -gt 0 } catch { $kullaniciVar = $false } }
    # Kliniğe özel paket (paket-olustur.ps1 -EskiVeri ...) eski verileri eski-veri klasöründe taşır: sormadan aktarılır.
    $paketVerisi = Join-Path $Paket 'eski-veri'
    if (-not $EskiVeri -and (Test-Path (Join-Path $paketVerisi 'DentaLineSuit\DLHASTA.DB'))) {
        $EskiVeri = Join-Path $paketVerisi 'DentaLineSuit'
        if (-not $Dl2001Veri -and (Test-Path (Join-Path $paketVerisi 'DentaLine2001\DLHASTA.DB'))) { $Dl2001Veri = Join-Path $paketVerisi 'DentaLine2001' }
        Bilgi "Pakette eski DentaLine verileri var; kurulumda aktarılacak$(if ($Dl2001Veri) { ' (DentaLineSuit + DentaLine 2001)' })."
        if ($kullaniciVar) { Bilgi 'Disci zaten kurulu ve kullanılıyor; veriler yeniden aktarılmayacak (yalnızca program güncellenir).' }
    }
    if (-not $EskiVeri -and -not $Deneme -and -not $kullaniciVar -and (Evet-Mi 'Eski DentaLineSuit verileri bu kurulumda aktarılsın mı? (Eski program kullanmadıysanız hayır deyin)' $false)) {
        $EskiVeri = (Read-Host '   DL2500DB klasörünün yolu (ör. D:\DentaLineSuit\DL2500DB)').Trim('" ')
        if (-not (Test-Path (Join-Path $EskiVeri 'DLHASTA.DB'))) { throw "Bu klasörde DLHASTA.DB yok: $EskiVeri" }
        if (-not $Dl2001Veri -and (Evet-Mi 'DentaLine 2001 (2.3) programına girilmiş kayıtlar da var mı?' $false)) {
            $Dl2001Veri = (Read-Host '   DentaLine 2001 veri klasörünün yolu (ör. D:\DL2001\DB)').Trim('" ')
        }
    }
    if ($Dl2001Veri -and -not (Test-Path (Join-Path $Dl2001Veri 'DLHASTA.DB'))) { throw "DentaLine 2001 klasöründe DLHASTA.DB yok: $Dl2001Veri" }
    if ($Dl2001Veri -and -not $EskiVeri) { throw 'DentaLine 2001 verisi, DentaLineSuit verisiyle birlikte aktarılır (-EskiVeri de verin).' }
    Tamam 'Tamam.'

    # ------------------------------------------------------------------
    Baslik '2. SQL Server Express'
    $sqlServis = Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
    if ($sqlServis) {
        Bilgi "Kurulu: $($sqlServis.DisplayName)"
    } else {
        Bilgi 'SQL Server Express kurulu değil; winget ile kurulacak (birkaç dakika sürebilir).'
        if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
            throw 'winget bulunamadı. SQL Server Express''i https://www.microsoft.com/sql-server/sql-server-downloads adresinden elle kurup betiği tekrar çalıştırın.'
        }
        Bilgi 'Lisans koşulları: https://go.microsoft.com/fwlink/?LinkId=2104078'
        if (-not (Evet-Mi 'Microsoft SQL Server Express lisans koşullarını kabul ediyor musunuz?' $true)) {
            throw 'Lisans kabul edilmedi; kurulum durduruldu.'
        }
        foreach ($paketKimligi in 'Microsoft.SQLServer.2025.Express', 'Microsoft.SQLServer.2022.Express') {
            Yap "winget install $paketKimligi" {
                winget install --id $paketKimligi --exact --silent --accept-package-agreements --accept-source-agreements | Out-Host
            }
            if ($Deneme -or (Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue)) { break }
        }
        if (-not $Deneme -and -not (Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue)) {
            throw 'SQL Server Express kurulamadı. https://www.microsoft.com/sql-server/sql-server-downloads adresinden "Express" sürümünü varsayılan ayarlarla (örnek adı SQLEXPRESS) kurup betiği tekrar çalıştırın.'
        }
    }
    Yap 'SQL Server hizmeti otomatik başlayacak şekilde ayarlanıyor ve başlatılıyor' {
        Set-Service 'MSSQL$SQLEXPRESS' -StartupType Automatic
        Start-Service 'MSSQL$SQLEXPRESS'
    }
    if (-not $Deneme) {
        try { [void](Sql-Calistir 'SELECT 1' -Skaler) }
        catch { throw "SQL Server'a bağlanılamadı. Kurulumu yapan Windows kullanıcısının SQL Server yöneticisi olması gerekir. Ayrıntı: $($_.Exception.Message)" }
        Tamam 'SQL Server bağlantısı tamam.'
    }

    # ------------------------------------------------------------------
    Baslik '3. Program dosyaları'
    if (Get-Service $Script:ServisAdi -ErrorAction SilentlyContinue) {
        Yap 'Çalışan Disci hizmeti güncelleme için durduruluyor' { Stop-Service $Script:ServisAdi -Force }
    }
    Yap "Program kopyalanıyor: $uygulama" {
        robocopy (Join-Path $Paket 'app') $uygulama /MIR /XF appsettings.Production.json /R:2 /W:2 /NFL /NDL /NP /NJH /NJS | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Kopyalama başarısız (robocopy $LASTEXITCODE)" }
    }
    Yap "Aktarım aracı kopyalanıyor: $aktarim" {
        robocopy (Join-Path $Paket 'import') $aktarim /MIR /R:2 /W:2 /NFL /NDL /NP /NJH /NJS | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Kopyalama başarısız (robocopy $LASTEXITCODE)" }
    }
    Yap "Yardımcı betikler kopyalanıyor: $betikler" {
        New-Item -ItemType Directory -Force -Path $betikler | Out-Null
        foreach ($b in 'ortak.ps1', 'yedekle.ps1', 'geri-yukle.ps1', 'parola-sifirla.ps1', 'veri-aktar.ps1', 'kaldir.ps1', 'OKUBENI.md', 'disci.ico') {
            Copy-Item (Join-Path $Paket $b) $betikler -Force
        }
    }
    Yap 'Veri klasörleri oluşturuluyor' {
        foreach ($k in $dosyalar, $anahtarlar, $yedekler, $sertifikaKlasoru) { New-Item -ItemType Directory -Force -Path $k | Out-Null }
    }

    # ------------------------------------------------------------------
    Baslik '4. Windows hizmeti'
    if (-not (Get-Service $Script:ServisAdi -ErrorAction SilentlyContinue)) {
        Yap 'Disci hizmeti oluşturuluyor' {
            New-Service -Name $Script:ServisAdi -BinaryPathName "`"$uygulama\Disci.Web.exe`"" -DisplayName 'Disci Klinik Programı' `
                -Description 'Disci diş kliniği programı (web sunucusu).' -StartupType Automatic | Out-Null
        }
    } else {
        Bilgi 'Hizmet zaten var; ayarları güncelleniyor.'
    }
    Yap "Hizmet '$($Script:ServisHesabi)' hesabıyla, SQL Server'dan sonra ve hata olursa yeniden başlayacak şekilde ayarlanıyor" {
        sc.exe config $Script:ServisAdi obj= $Script:ServisHesabi start= delayed-auto depend= 'MSSQL$SQLEXPRESS' | Out-Null
        sc.exe failure $Script:ServisAdi reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    }
    Yap 'Klasör izinleri veriliyor' {
        icacls $Hedef /grant "$($Script:ServisHesabi):(OI)(CI)RX" /T /C /Q | Out-Null
        icacls $VeriKlasoru /grant "$($Script:ServisHesabi):(OI)(CI)M" /T /C /Q | Out-Null
        # SQL Server yedek dosyasını kendi hizmet hesabıyla yazar.
        icacls $yedekler /grant 'NT SERVICE\MSSQL$SQLEXPRESS:(OI)(CI)M' /C /Q | Out-Null
    }

    # ------------------------------------------------------------------
    Baslik '5. HTTPS sertifikası'
    $ipler = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notmatch '^(127\.|169\.254\.)' } | Select-Object -ExpandProperty IPAddress)
    $sertifika = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -eq 'Disci' -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
        Sort-Object NotAfter -Descending | Select-Object -First 1
    if ($sertifika -and -not $YeniSertifika) {
        Bilgi "Var olan sertifika kullanılıyor (bitiş: $($sertifika.NotAfter.ToString('dd.MM.yyyy')))."
    } else {
        $san = (@("DNS=$bilgisayar", 'DNS=localhost') + ($ipler | ForEach-Object { "IPAddress=$_" })) -join '&'
        Yap "Kendinden imzalı sertifika oluşturuluyor ($bilgisayar, $($ipler -join ', '))" {
            $Script:sertifika = New-SelfSignedCertificate -Subject "CN=$bilgisayar" -FriendlyName 'Disci' `
                -TextExtension @("2.5.29.17={text}$san", '2.5.29.37={text}1.3.6.1.5.5.7.3.1') `
                -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(5) `
                -CertStoreLocation Cert:\LocalMachine\My
        }
        if (-not $Deneme) { $sertifika = $Script:sertifika }
    }
    if (-not $Deneme) {
        $cerYolu = Join-Path $sertifikaKlasoru 'Disci.cer'
        Export-Certificate -Cert $sertifika -FilePath $cerYolu -Force | Out-Null
        # Bu bilgisayarın tarayıcısı sertifikaya güvensin.
        Import-Certificate -FilePath $cerYolu -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
        # Hizmet hesabı sertifikanın gizli anahtarını okuyabilmeli.
        $rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($sertifika)
        $anahtarAdi = $rsa.Key.UniqueName
        $anahtarYolu = @("$env:ProgramData\Microsoft\Crypto\Keys\$anahtarAdi", "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys\$anahtarAdi") |
            Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $anahtarYolu) { throw 'Sertifikanın gizli anahtar dosyası bulunamadı.' }
        icacls $anahtarYolu /grant "$($Script:ServisHesabi):R" /Q | Out-Null
        Tamam "Sertifika hazır. Diğer cihazlara kurulacak dosya: $cerYolu"
    }

    # ------------------------------------------------------------------
    Baslik '6. Program ayarları'
    $ayarlar = [ordered]@{
        ConnectionStrings = @{ DefaultConnection = (Baglanti-Metni) }
        FileStorage = @{ Root = $dosyalar }
        DataProtection = @{ KeysPath = $anahtarlar }
        Database = @{ MigrateOnStartup = $true }
        https_port = $HttpsPort
        Kestrel = @{
            Endpoints = [ordered]@{
                Http = @{ Url = "http://0.0.0.0:$HttpPort" }
                Https = @{
                    Url = "https://0.0.0.0:$HttpsPort"
                    Certificate = @{ Subject = $bilgisayar; Store = 'My'; Location = 'LocalMachine'; AllowInvalid = $true }
                }
            }
        }
    }
    Yap "Ayar dosyası yazılıyor: $uygulama\appsettings.Production.json" {
        $json = $ayarlar | ConvertTo-Json -Depth 8
        [IO.File]::WriteAllText((Join-Path $uygulama 'appsettings.Production.json'), $json, (New-Object Text.UTF8Encoding $false))
    }

    # ------------------------------------------------------------------
    Baslik '7. Veritabanı'
    if ($Deneme) {
        Bilgi "[DENEME] '$($Script:VeritabaniAdi)' veritabanı hazırlanacak$(if ($EskiVeri) { " ve $EskiVeri aktarılacak" })."
    } else {
        $hastaSayisi = Hasta-Sayisi
        if ($EskiVeri -and $hastaSayisi -eq 0) {
            Bilgi "Eski veriler aktarılıyor: $EskiVeri"
            & (Join-Path $aktarim 'Disci.LegacyImport.exe') --source $EskiVeri --connection (Baglanti-Metni) --files $dosyalar | Out-Host
            if ($LASTEXITCODE -ne 0) { throw "Veri aktarımı başarısız (çıkış kodu $LASTEXITCODE). Yukarıdaki mesaja bakın." }
            if ($Dl2001Veri) {
                Bilgi "DentaLine 2001 kayıtları ekleniyor (aynı hastalar birleştirilir): $Dl2001Veri"
                & (Join-Path $aktarim 'Disci.LegacyImport.exe') --dl2001 $Dl2001Veri --connection (Baglanti-Metni) --files $dosyalar | Out-Host
                if ($LASTEXITCODE -ne 0) { throw "DentaLine 2001 aktarımı başarısız (çıkış kodu $LASTEXITCODE). Yukarıdaki mesaja bakın." }
            }
            Tamam "Aktarım tamam. Rapor: $(Join-Path $VeriKlasoru 'reports')"
        } elseif ($EskiVeri) {
            Uyari "Veritabanında zaten $hastaSayisi hasta var; aktarım atlandı. Yeniden aktarmak için: $betikler\veri-aktar.ps1"
        }
        Servis-Veritabani-Yetkisi
        Yedek-Veritabani-Yetkisi
        Tamam "Veritabanı hazır ve '$($Script:ServisHesabi)' yetkilendirildi."
    }

    # ------------------------------------------------------------------
    Baslik '8. Güvenlik duvarı ve ağ'
    Yap "Gelen bağlantılara $HttpPort ve $HttpsPort portları açılıyor (yalnızca Özel ve Etki alanı ağları)" {
        Get-NetFirewallRule -DisplayName 'Disci *' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
        New-NetFirewallRule -DisplayName "Disci (HTTP $HttpPort)" -Direction Inbound -Protocol TCP -LocalPort $HttpPort -Action Allow -Profile Domain, Private | Out-Null
        New-NetFirewallRule -DisplayName "Disci (HTTPS $HttpsPort)" -Direction Inbound -Protocol TCP -LocalPort $HttpsPort -Action Allow -Profile Domain, Private | Out-Null
    }
    foreach ($ag in @(Get-NetConnectionProfile -ErrorAction SilentlyContinue | Where-Object NetworkCategory -eq 'Public')) {
        Uyari "'$($ag.Name)' ağı 'Ortak' olarak işaretli; bu ağdaki diğer cihazlar Disci'ye bağlanamaz."
        if (Evet-Mi "'$($ag.Name)' ağı 'Özel' yapılsın mı? (Yalnızca klinik ağıysa evet deyin)" $false) {
            Yap "'$($ag.Name)' Özel ağ yapılıyor" { Set-NetConnectionProfile -InterfaceIndex $ag.InterfaceIndex -NetworkCategory Private }
        }
    }

    # ------------------------------------------------------------------
    Baslik '9. Programı başlatma'
    Yap 'Disci hizmeti başlatılıyor' {
        Start-Service $Script:ServisAdi
        if (-not (Program-Yanit-Veriyor-Mu $HttpPort)) {
            throw "Program yanıt vermiyor. Olay Görüntüleyicisi > Windows Günlükleri > Uygulama altında 'Disci' kayıtlarına bakın."
        }
    }
    if (-not $Deneme) { Tamam 'Program çalışıyor.' }

    # ------------------------------------------------------------------
    Baslik '10. Masaüstü kısayolu ve diğer bilgisayarlar için bağlantı paketi'
    $baglantiKlasoru = Join-Path $VeriKlasoru 'Disci-Baglanti'
    Yap 'Bu bilgisayarın masaüstüne (bütün kullanıcılar) Disci kısayolu ekleniyor' {
        $masaustu = [Environment]::GetFolderPath('CommonDesktopDirectory')
        $metin = "[InternetShortcut]`r`nURL=https://localhost:$HttpsPort/`r`nIconFile=$betikler\disci.ico`r`nIconIndex=0`r`n"
        [IO.File]::WriteAllText((Join-Path $masaustu 'Disci.url'), $metin, (New-Object Text.UTF8Encoding $false))
    }
    Yap "Diğer bilgisayarlar için bağlantı paketi hazırlanıyor: $baglantiKlasoru" {
        New-Item -ItemType Directory -Force -Path $baglantiKlasoru | Out-Null
        Copy-Item (Join-Path $Paket 'baglanti\*') $baglantiKlasoru -Force
        Copy-Item (Join-Path $Paket 'disci.ico') $baglantiKlasoru -Force
        Copy-Item (Join-Path $sertifikaKlasoru 'Disci.cer') $baglantiKlasoru -Force
        $ilkIp = @($ipler | Where-Object { $_ -match '^(192\.168|10\.|172\.(1[6-9]|2\d|3[01]))' })[0]
        $sunucuTxt = "# Disci sunucusu (kurulum yazdı)`r`nSunucu=$bilgisayar`r`nIP=$ilkIp`r`nHttpsPort=$HttpsPort`r`n"
        [IO.File]::WriteAllText((Join-Path $baglantiKlasoru 'sunucu.txt'), $sunucuTxt, (New-Object Text.UTF8Encoding $false))
    }
    if (-not $Deneme -and (Evet-Mi "Bağlantı paketi ağda paylaşılsın mı? (Diğer bilgisayarlar \\$bilgisayar\Disci-Baglanti adresinden çalıştırır; içinde hasta bilgisi yoktur)" $true)) {
        Yap 'Bağlantı paketi salt okunur paylaşılıyor: Disci-Baglanti' {
            if (-not (Get-SmbShare -Name 'Disci-Baglanti' -ErrorAction SilentlyContinue)) {
                $herkes = (New-Object Security.Principal.SecurityIdentifier 'S-1-1-0').Translate([Security.Principal.NTAccount]).Value
                New-SmbShare -Name 'Disci-Baglanti' -Path $baglantiKlasoru -ReadAccess $herkes -Description 'Disci bağlantı paketi' | Out-Null
            }
        }
    }

    # ------------------------------------------------------------------
    Baslik '11. Günlük yedek'
    Yap "Her gün $YedekSaati'te veritabanı ve fotoğraflar $yedekler klasörüne yedeklenecek (son 14 gün saklanır; bilgisayar o saatte kapalıysa açılınca alınır)" {
        # Eski sürümlerin gece görevi varsa yerine bu geçer.
        Unregister-ScheduledTask -TaskName 'Disci Gece Yedegi' -Confirm:$false -ErrorAction SilentlyContinue
        $islem = New-ScheduledTaskAction -Execute 'powershell.exe' `
            -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$betikler\yedekle.ps1`" -YedekKlasoru `"$yedekler`" -DosyaKlasoru `"$dosyalar`""
        $tetik = New-ScheduledTaskTrigger -Daily -At $YedekSaati
        $hesap = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
        $ayar = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2)
        Register-ScheduledTask -TaskName $Script:YedekGorevi -Action $islem -Trigger $tetik -Principal $hesap -Settings $ayar -Force | Out-Null
    }
    if (-not $Deneme -and (Evet-Mi 'Yedeklemenin çalıştığını görmek için şimdi bir deneme yedeği alınsın mı?' $true)) {
        $once = @(Get-ChildItem $yedekler -Filter *.bak -ErrorAction SilentlyContinue).Count
        Start-ScheduledTask -TaskName $Script:YedekGorevi
        $bitis = (Get-Date).AddMinutes(5)
        do { Start-Sleep -Seconds 5 } while ((Get-ScheduledTask -TaskName $Script:YedekGorevi).State -eq 'Running' -and (Get-Date) -lt $bitis)
        if (@(Get-ChildItem $yedekler -Filter *.bak -ErrorAction SilentlyContinue).Count -gt $once) { Tamam 'Deneme yedeği alındı.' }
        else { Uyari "Deneme yedeği oluşmadı. Ayrıntı: $yedekler\yedek.log" }
    }

    # ------------------------------------------------------------------
    Baslik '12. Güç ayarı'
    if (Evet-Mi 'Bilgisayar prize takılıyken uyku moduna geçmesin mi? (Program sürekli erişilebilir kalır)' $true) {
        Yap 'Uyku ve hazırda bekleme kapatılıyor (prizdeyken)' {
            powercfg /change standby-timeout-ac 0
            powercfg /change hibernate-timeout-ac 0
        }
    }

    # ------------------------------------------------------------------
    Baslik 'Kurulum tamamlandı'
    Bilgi "Bu bilgisayardan          : masaüstündeki 'Disci' kısayolu (https://localhost:$HttpsPort)"
    Bilgi "Klinik ağındaki cihazlardan: https://${bilgisayar}:$HttpsPort"
    foreach ($ip in $ipler) { Bilgi "                           https://${ip}:$HttpsPort" }
    Bilgi ''
    Bilgi 'Diğer bilgisayarlarda bir kez şunu çalıştırın (kısayol ve sertifika kurar):'
    Bilgi "   \\$bilgisayar\Disci-Baglanti\Baglanti-Kur.bat   (paylaşım yoksa $baglantiKlasoru klasörünü USB ile götürün)"
    Bilgi "Tablet ve telefonlar için sertifika: $sertifikaKlasoru\Disci.cer (ayrıntı: OKUBENI.md)"
    Bilgi "Yedekler : $yedekler (her gün $YedekSaati; haftada bir harici diske de kopyalayın)"
    if ($EskiVeri -and $EskiVeri.StartsWith($Paket, [StringComparison]::OrdinalIgnoreCase)) {
        Uyari "Kurulum paketinde hasta verisi var ($Paket\eski-veri). Aktarım bitti; paketi ve USB bellekteki kopyasını silin."
    }
    Bilgi "Kayıt    : $(Join-Path $logKlasoru "kurulum_log_$zaman.txt")"
    if ($Deneme) {
        Bilgi 'Deneme modu bitti; hiçbir değişiklik yapılmadı.'
    } elseif ((Kullanici-Sayisi) -eq 0) {
        Write-Host ''
        Tamam 'Şimdi tarayıcıda İLK KURULUM FORMU açılıyor: klinik bilgilerini ve yönetici hesabını girin.'
        Start-Process "https://localhost:$HttpsPort/kurulum"
    } else {
        Start-Process "https://localhost:$HttpsPort/"
    }
    $sonuc = 0
} catch {
    Write-Host ''
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Kurulum yarıda kaldı. Sorunu giderip betiği tekrar çalıştırabilirsiniz; tamamlanan adımlar atlanır.' -ForegroundColor Red
    $sonuc = 1
} finally {
    Stop-Transcript | Out-Null
}
exit $sonuc
