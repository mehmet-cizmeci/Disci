# Disci kurulum betiği: klinik bilgisayarına programı ve gereken her şeyi kurar.
# Kullanım: KUR.bat dosyasına çift tıklayın. Seçenekler için OKUBENI.md dosyasına bakın.
param(
    [string]$Hedef = 'C:\Disci',
    [string]$VeriKlasoru = 'C:\DisciVeri',
    [int]$HttpPort = 5111,
    [int]$HttpsPort = 5443,
    [string]$YedekSaati = '14:00',
    # Var olan bir SQL Server: '.\SQLEXPRESS', '.' (varsayılan örnek), '.\BASKA', 'SUNUCU\ORNEK' ya da '192.168.1.10,1433'.
    # Boşsa kurulum sorar (yeni SQL Server Express kur / bu bilgisayardaki / başka bilgisayardaki SQL Server).
    [string]$SqlSunucu = '',
    # SQL kullanıcı adı ve parolası; boşsa Windows kimlik doğrulaması.
    [string]$SqlKullaniciAdi = '',
    [string]$SqlParolasi = '',
    # Veritabanı adı (varsayılan Disci).
    [string]$Veritabani = '',
    # SQL Server kurma seçeneği hiç sunulmasın (yalnızca var olan bir sunucu kullanılır).
    [switch]$SqlKurma,
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
$betikler = Join-Path $Hedef 'betikler'
$dosyalar = Join-Path $VeriKlasoru 'files'
$anahtarlar = Join-Path $VeriKlasoru 'keys'
$yedekler = Join-Path $VeriKlasoru 'Yedekler'
$sertifikaKlasoru = Join-Path $VeriKlasoru 'sertifika'
$guncellemeKlasoru = Join-Path $VeriKlasoru 'Guncelleme'
$bilgisayar = $env:COMPUTERNAME

# Güncellemede önceki kurulumun veritabanı bağlantısı (sunucu, kullanıcı, veritabanı adı, sunucu yedek klasörü) korunur.
$oncekiBaglanti = $false
$oncekiAyar = Join-Path $uygulama 'appsettings.Production.json'
if (Test-Path $oncekiAyar) {
    try {
        $eskiAyar = Get-Content $oncekiAyar -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($eskiAyar.ConnectionStrings.DefaultConnection) { Baglanti-Ayarla $eskiAyar.ConnectionStrings.DefaultConnection; $oncekiBaglanti = $true }
        if ($eskiAyar.Backup.ServerFolder) { $Script:SunucuYedekKlasoru = $eskiAyar.Backup.ServerFolder }
    } catch { }
}

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
    foreach ($gerekli in @('app\Disci.Web.exe')) {
        if (-not (Test-Path (Join-Path $Paket $gerekli))) {
            if ($Deneme) { Uyari "Pakette '$gerekli' yok (deneme modunda sorun değil)." }
            else { throw "Pakette '$gerekli' bulunamadı. Paketi paket-olustur.ps1 ile yeniden oluşturun." }
        }
    }
    Tamam 'Tamam.'

    # ------------------------------------------------------------------
    Baslik '2. SQL Server (veritabanı)'
    # SQL Server kurmak zorunlu değil. Sıra: komut satırı (-SqlSunucu ...) > önceki Disci kurulumunun bağlantısı >
    # soru: yeni SQL Server Express kur / bu bilgisayardaki bir SQL Server / başka bilgisayardaki bir SQL Server.
    $sqlKur = $false
    if ($SqlSunucu) {
        $Script:SqlOrnek = $SqlSunucu
        $Script:SqlKullanici = $SqlKullaniciAdi
        $Script:SqlParola = $SqlParolasi
        if ($Veritabani) { $Script:VeritabaniAdi = $Veritabani }
        Bilgi "Komut satırında verilen SQL Server kullanılacak: $SqlSunucu ($(Sql-Kimlik-Metni), veritabanı '$($Script:VeritabaniAdi)')"
    } elseif ($oncekiBaglanti) {
        Bilgi "Önceki kurulumun veritabanı bağlantısı kullanılacak: $($Script:SqlOrnek) ($(Sql-Kimlik-Metni), veritabanı '$($Script:VeritabaniAdi)')"
        Bilgi "Değiştirmek için kurulumdan sonra: $betikler\veritabani-ayarla.ps1"
    } elseif ($Deneme) {
        if (@(Sql-Ornekleri) -contains '.\SQLEXPRESS') { $Script:SqlOrnek = '.\SQLEXPRESS' } else { $sqlKur = -not $SqlKurma }
    } else {
        $sqlKur = Sql-Baglantisi-Sor -KurulumSecenegi:(-not $SqlKurma)
    }

    if ($sqlKur) {
        $Script:SqlOrnek = '.\SQLEXPRESS'
        $cevrimdisi = @(Get-ChildItem (Join-Path $Paket 'sql') -Filter 'SQLEXPR*.exe' -ErrorAction SilentlyContinue) | Select-Object -First 1
        Bilgi 'Lisans koşulları: https://go.microsoft.com/fwlink/?LinkId=2104078'
        if (-not (Evet-Mi 'Microsoft SQL Server Express lisans koşullarını kabul ediyor musunuz?' $true)) {
            throw 'Lisans kabul edilmedi; kurulum durduruldu.'
        }
        if ($cevrimdisi) {
            Bilgi "SQL Server Express paketteki dosyadan (internetsiz) kuruluyor: $($cevrimdisi.Name). Bu 5-15 dakika sürebilir."
            Yap 'SQL Server Express kuruluyor' { SqlExpress-Cevrimdisi-Kur $cevrimdisi.FullName }
        } else {
            Uyari 'Pakette SQL Server kurulum dosyası (sql\SQLEXPR_x64_*.exe) yok; internetten (winget) kurulacak.'
            Bilgi 'İnternetsiz kurmak için dosyayı Microsoft''tan indirip paketin sql klasörüne koyabilirsiniz:'
            Bilgi '   https://go.microsoft.com/fwlink/p/?linkid=2216019  (Medya indir > Express Core)'
            if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
                throw 'winget bulunamadı. SQL Server kurulum dosyasını yukarıdaki adresten indirip paketin sql klasörüne koyun ve betiği tekrar çalıştırın.'
            }
            foreach ($paketKimligi in 'Microsoft.SQLServer.2022.Express', 'Microsoft.SQLServer.2025.Express') {
                Yap "winget install $paketKimligi" {
                    winget install --id $paketKimligi --exact --silent --accept-package-agreements --accept-source-agreements | Out-Host
                }
                if ($Deneme -or (Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue)) { break }
            }
        }
        if (-not $Deneme -and -not (Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue)) {
            throw 'SQL Server Express kurulamadı. Ayrıntı için %ProgramFiles%\Microsoft SQL Server\*\Setup Bootstrap\Log klasörüne bakın.'
        }
    }

    $sqlServisAdi = Sql-Servis-Adi
    if ($sqlServisAdi) {
        if (-not $Deneme -and -not (Get-Service $sqlServisAdi -ErrorAction SilentlyContinue)) { throw "SQL Server hizmeti bulunamadı: $sqlServisAdi ($($Script:SqlOrnek))" }
        Yap "SQL Server hizmeti ($sqlServisAdi) otomatik başlayacak şekilde ayarlanıyor ve başlatılıyor" {
            Set-Service $sqlServisAdi -StartupType Automatic
            Start-Service $sqlServisAdi
        }
    }
    if (-not $Deneme) {
        Sql-Baglantisini-Dogrula -SorabilirMi:(-not $SqlSunucu)
        $sqlServisAdi = Sql-Servis-Adi   # bilgiler yeniden girildiyse
    }
    if (-not $sqlServisAdi) {
        if (-not $Script:SqlKullanici) {
            Uyari "$($Script:SqlOrnek) başka bir bilgisayarda: o sunucuda bu bilgisayarın hesabına ($env:USERDOMAIN\$env:COMPUTERNAME$) izin verilecek (yalnızca etki alanı ağlarında çalışır)."
        }
        # Yedek, SQL Server'ın kendi diskine yazılır; bu bilgisayardan görülmeyebilir.
        if (-not $Script:SunucuYedekKlasoru) { $Script:SunucuYedekKlasoru = if ($Deneme) { '' } else { Sunucu-Varsayilan-Yedek-Klasoru } }
        if (-not $Deneme -and -not $SqlSunucu) {
            $k = Read-Host "   Günlük veritabanı yedeği SUNUCUNUN hangi klasörüne yazılsın? [$($Script:SunucuYedekKlasoru)]"
            if (-not [string]::IsNullOrWhiteSpace($k)) { $Script:SunucuYedekKlasoru = $k.Trim() }
        }
        Bilgi "Veritabanı yedekleri sunucuda: $($Script:SunucuYedekKlasoru) (fotoğraflar bu bilgisayarda $yedekler)"
    } else {
        $Script:SunucuYedekKlasoru = ''
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
    Yap 'Eski sürümlerden kalan kullanılmayan dosyalar siliniyor' {
        # 1.3.0 ve öncesinin ayrı araç klasörü ve betiği (parola sıfırlama artık programın içinde).
        Remove-Item (Join-Path $Hedef 'import') -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $betikler 'veri-aktar.ps1') -Force -ErrorAction SilentlyContinue
    }
    Yap "Yardımcı betikler kopyalanıyor: $betikler" {
        New-Item -ItemType Directory -Force -Path $betikler | Out-Null
        foreach ($b in 'ortak.ps1', 'yedekle.ps1', 'geri-yukle.ps1', 'parola-sifirla.ps1', 'kaldir.ps1', 'guncelle.ps1', 'veritabani-ayarla.ps1', 'OKUBENI.md', 'disci.ico') {
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
        # A local SQL Server must be running before Disci starts; with a server on another computer there is nothing to wait for.
        $bagimlilik = if ($sqlServisAdi) { $sqlServisAdi } else { '/' }
        sc.exe config $Script:ServisAdi obj= $Script:ServisHesabi start= delayed-auto depend= $bagimlilik | Out-Null
        sc.exe failure $Script:ServisAdi reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    }
    Yap 'Klasör izinleri veriliyor' {
        icacls $Hedef /grant "$($Script:ServisHesabi):(OI)(CI)RX" /T /C /Q | Out-Null
        icacls $VeriKlasoru /grant "$($Script:ServisHesabi):(OI)(CI)M" /T /C /Q | Out-Null
        # SQL Server yedek dosyasını kendi hizmet hesabıyla yazar.
        if ($sqlServisAdi) { icacls $yedekler /grant "NT SERVICE\$($sqlServisAdi):(OI)(CI)M" /C /Q | Out-Null }
        # The update task (SYSTEM) and the program exchange requests here.
        New-Item -ItemType Directory -Force -Path $guncellemeKlasoru | Out-Null
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
        Update = @{ Folder = $guncellemeKlasoru; Repository = $Script:Depo }
        Backup = @{ ServerFolder = $Script:SunucuYedekKlasoru }
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
        Ayar-Dosyasini-Koru (Join-Path $uygulama 'appsettings.Production.json')
    }

    # ------------------------------------------------------------------
    Baslik '7. Veritabanı'
    if ($Deneme) {
        Bilgi "[DENEME] '$($Script:VeritabaniAdi)' veritabanı hazırlanacak."
    } else {
        Veritabani-Yetkileri
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
    Baslik '12. Güncelleme görevi'
    # Program kendini güncelleyemez (kısıtlı hesapla çalışır). SYSTEM hesabıyla 10 dakikada bir çalışan görev, programın
    # bıraktığı isteği (Güncelleme sayfası) ya da otomatik güncelleme ayarını görürse yeni sürümü GitHub'dan indirip kurar.
    Yap "Güncelleme görevi kaydediliyor ('$($Script:GuncellemeGorevi)', 10 dakikada bir)" {
        $islem = New-ScheduledTaskAction -Execute 'powershell.exe' `
            -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$betikler\guncelle.ps1`" -Hedef `"$Hedef`" -VeriKlasoru `"$VeriKlasoru`""
        $tetik = New-ScheduledTaskTrigger -Once -At (Get-Date).Date -RepetitionInterval (New-TimeSpan -Minutes 10)
        $hesap = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
        $ayar = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 1)
        Register-ScheduledTask -TaskName $Script:GuncellemeGorevi -Action $islem -Trigger $tetik -Principal $hesap -Settings $ayar -Force | Out-Null
    }

    # ------------------------------------------------------------------
    Baslik '13. Güç ayarı'
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
