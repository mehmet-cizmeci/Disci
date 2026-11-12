# Disci kurulum betiklerinin ortak işlevleri. Doğrudan çalıştırılmaz; diğer betikler bunu yükler.

$Script:ServisAdi = 'Disci'
$Script:ServisHesabi = 'NT SERVICE\Disci'
$Script:SqlOrnek = '.\SQLEXPRESS'
$Script:VeritabaniAdi = 'Disci'
$Script:YedekGorevi = 'Disci Gunluk Yedek'
$Script:GuncellemeGorevi = 'Disci Guncelleme'
$Script:Depo = 'mehmet-cizmeci/Disci'

# Veritabanı bağlantısı: SQL kullanıcı adı boşsa Windows kimlik doğrulaması kullanılır.
$Script:SqlKullanici = ''
$Script:SqlParola = ''
# Başka bilgisayardaki SQL Server'da yedeğin yazılacağı klasör (sunucunun kendi diskinde, sunucunun gördüğü yol).
$Script:SunucuYedekKlasoru = ''

# Bağlantı metnini çözüp yukarıdaki değişkenlere yazar.
function Baglanti-Ayarla([string]$baglantiMetni) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $baglantiMetni
    # PowerShell bu nesnede özellik yerine sözlük anahtarı kullanır: anahtar adları ('Data Source'...) yazılmalı.
    if ($b['Data Source']) { $Script:SqlOrnek = [string]$b['Data Source'] }
    if ($b['Initial Catalog']) { $Script:VeritabaniAdi = [string]$b['Initial Catalog'] }
    if ([bool]$b['Integrated Security']) { $Script:SqlKullanici = ''; $Script:SqlParola = '' }
    else { $Script:SqlKullanici = [string]$b['User ID']; $Script:SqlParola = [string]$b['Password'] }
}

# Kurulumdan sonra betikler C:\Disci\betikler altındadır; veritabanı bağlantısını programın ayar dosyasından okur
# (kurulumda başka bir SQL Server veya kullanıcı seçilmiş olabilir).
$Script:ProgramAyari = Join-Path (Split-Path $PSScriptRoot -Parent) 'app\appsettings.Production.json'
if (Test-Path $Script:ProgramAyari) {
    try {
        $ayarJson = Get-Content $Script:ProgramAyari -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($ayarJson.ConnectionStrings.DefaultConnection) { Baglanti-Ayarla $ayarJson.ConnectionStrings.DefaultConnection }
        if ($ayarJson.Backup.ServerFolder) { $Script:SunucuYedekKlasoru = $ayarJson.Backup.ServerFolder }
    } catch { }
}

function Sql-Kimlik-Metni {
    if ($Script:SqlKullanici) { "SQL kullanıcısı '$($Script:SqlKullanici)'" } else { 'Windows kimlik doğrulaması' }
}

# Bu bilgisayardaki SQL Server örnekleri: '.\SQLEXPRESS', '.' (varsayılan örnek MSSQLSERVER), '.\BASKA' ...
function Sql-Ornekleri {
    $anahtar = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
    if (-not (Test-Path $anahtar)) { return @() }
    @((Get-Item $anahtar).GetValueNames() | ForEach-Object { if ($_ -eq 'MSSQLSERVER') { '.' } else { ".\$_" } })
}

# Örneğin Windows hizmet adı; başka bilgisayardaki bir sunucu için $null.
function Sql-Servis-Adi([string]$ornek = $Script:SqlOrnek) {
    $yerel = '^(\.|\(local\)|localhost|127\.0\.0\.1|' + [regex]::Escape($env:COMPUTERNAME) + ')$'
    $parca = $ornek.Split('\', 2)
    if ($parca[0] -notmatch $yerel) { return $null }
    if ($parca.Count -eq 1 -or $parca[1] -eq 'MSSQLSERVER') { return 'MSSQLSERVER' }
    return 'MSSQL$' + $parca[1]
}

function Baslik([string]$metin) { Write-Host ''; Write-Host "== $metin" -ForegroundColor Cyan }
function Bilgi([string]$metin) { Write-Host "   $metin" }
function Uyari([string]$metin) { Write-Host "   UYARI: $metin" -ForegroundColor Yellow }
function Tamam([string]$metin) { Write-Host "   $metin" -ForegroundColor Green }

# Deneme modunda değişiklik yapmadan ne yapılacağını yazar.
function Yap([string]$aciklama, [scriptblock]$islem) {
    if ($Script:Deneme) { Write-Host "   [DENEME] $aciklama" -ForegroundColor DarkGray; return }
    Write-Host "   $aciklama"
    & $islem
}

function Yonetici-Mi {
    ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Evet-Mi([string]$soru, [bool]$varsayilan = $true) {
    if ($Script:Deneme) { return $varsayilan }
    $ek = if ($varsayilan) { '[E/h]' } else { '[e/H]' }
    $cevap = Read-Host "   $soru $ek"
    if ([string]::IsNullOrWhiteSpace($cevap)) { return $varsayilan }
    return $cevap.Trim().ToLower() -in 'e', 'evet', 'y', 'yes'
}

# Kurulum paketindeki SQLEXPR_x64_*.exe (çevrimdışı kurulum dosyası) ile internet olmadan SQLEXPRESS örneğini kurar.
# Kurulumu yapan yöneticiler (BUILTIN\Administrators) SQL yöneticisi olur; ağdan TCP erişimi kapalı kalır.
function SqlExpress-Cevrimdisi-Kur([string]$kurulumDosyasi) {
    $acilan = Join-Path $env:TEMP ("DisciSql_" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    $p = Start-Process -FilePath $kurulumDosyasi -ArgumentList "/QUIET /X:`"$acilan`"" -Wait -PassThru
    if ($p.ExitCode -ne 0 -or -not (Test-Path (Join-Path $acilan 'setup.exe'))) { throw "SQL Server kurulum dosyası açılamadı (çıkış kodu $($p.ExitCode))." }
    try {
        $arg = '/Q /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /IACCEPTSQLSERVERLICENSETERMS ' +
               '/SQLSYSADMINACCOUNTS="BUILTIN\Administrators" /SQLSVCSTARTUPTYPE=Automatic /TCPENABLED=0 /NPENABLED=0 ' +
               '/UPDATEENABLED=False /SUPPRESSPRIVACYSTATEMENTNOTICE /ENU'
        $p = Start-Process -FilePath (Join-Path $acilan 'setup.exe') -ArgumentList $arg -Wait -PassThru
        # 3010: kurulum tamam, yeniden başlatma öneriliyor.
        if ($p.ExitCode -notin 0, 3010) { throw "SQL Server Express kurulamadı (çıkış kodu $($p.ExitCode)). Günlük: $env:ProgramFiles\Microsoft SQL Server\*\Setup Bootstrap\Log" }
        if ($p.ExitCode -eq 3010) { Uyari 'SQL Server kuruldu; Windows bir yeniden başlatma öneriyor (kurulum bittikten sonra yapın).' }
    } finally {
        Remove-Item $acilan -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Baglanti-Metni([string]$veritabani = $Script:VeritabaniAdi, [switch]$Mars = $true) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $b['Data Source'] = $Script:SqlOrnek
    $b['Initial Catalog'] = $veritabani
    if ($Script:SqlKullanici) {
        $b['Integrated Security'] = $false
        $b['User ID'] = $Script:SqlKullanici
        $b['Password'] = $Script:SqlParola
    } else {
        $b['Integrated Security'] = $true
    }
    $b['TrustServerCertificate'] = $true
    $b['MultipleActiveResultSets'] = [bool]$Mars
    $b['Connect Timeout'] = 30
    $b.ConnectionString
}

# sqlcmd gerektirmeden T-SQL çalıştırır (PowerShell 5.1'deki .NET Framework SqlClient ile).
function Sql-Calistir([string]$sql, [string]$veritabani = 'master', [switch]$Skaler) {
    $baglanti = New-Object System.Data.SqlClient.SqlConnection (Baglanti-Metni $veritabani -Mars:$false)
    try {
        $baglanti.Open()
        $komut = $baglanti.CreateCommand()
        $komut.CommandText = $sql
        $komut.CommandTimeout = 600
        if ($Skaler) { return $komut.ExecuteScalar() }
        [void]$komut.ExecuteNonQuery()
    } finally { $baglanti.Dispose() }
}

function Veritabani-Var-Mi {
    [bool](Sql-Calistir "SELECT DB_ID(N'$($Script:VeritabaniAdi)')" -Skaler | Where-Object { $_ -isnot [DBNull] })
}

function Hasta-Sayisi {
    if (-not (Veritabani-Var-Mi)) { return 0 }
    $tablo = Sql-Calistir "SELECT OBJECT_ID(N'dbo.Patients')" -veritabani $Script:VeritabaniAdi -Skaler
    if ($tablo -is [DBNull]) { return 0 }
    [int](Sql-Calistir 'SELECT COUNT(*) FROM dbo.Patients' -veritabani $Script:VeritabaniAdi -Skaler)
}

function Kullanici-Sayisi {
    if (-not (Veritabani-Var-Mi)) { return 0 }
    $tablo = Sql-Calistir "SELECT OBJECT_ID(N'dbo.AspNetUsers')" -veritabani $Script:VeritabaniAdi -Skaler
    if ($tablo -is [DBNull]) { return 0 }
    [int](Sql-Calistir 'SELECT COUNT(*) FROM dbo.AspNetUsers' -veritabani $Script:VeritabaniAdi -Skaler)
}

# Hizmet hesabı (NT SERVICE\Disci) yalnızca Disci veritabanının sahibi olur; sunucunun geri kalanına erişemez.
function Servis-Veritabani-Yetkisi {
    # Bu bilgisayardaki sunucuda hizmetin sanal hesabı; başka bilgisayardaki sunucuda bu bilgisayarın etki alanı hesabı
    # (DOMAIN\BILGISAYAR$) bağlanır. Etki alanı olmayan ağlarda uzak sunucu için SQL kullanıcısı kullanılmalı.
    $hesap = if (Sql-Servis-Adi) { $Script:ServisHesabi } else { "$env:USERDOMAIN\$env:COMPUTERNAME$" }
    Veritabani-Olustur
    Sql-Calistir "IF SUSER_ID(N'$hesap') IS NULL CREATE LOGIN [$hesap] FROM WINDOWS;"
    Sql-Calistir @"
-- Başka bilgisayardan gelen bir yedek geri yüklendiyse kullanıcı bu bilgisayarın hesabına yeniden bağlanır.
IF USER_ID(N'$hesap') IS NULL CREATE USER [$hesap] FOR LOGIN [$hesap]
ELSE ALTER USER [$hesap] WITH LOGIN = [$hesap];
ALTER ROLE db_owner ADD MEMBER [$hesap];
"@ -veritabani $Script:VeritabaniAdi
}

# Veritabanı yoksa oluşturur. SQL kullanıcısının buna yetkisi yoksa ne yapılacağını söyler.
function Veritabani-Olustur {
    if (Veritabani-Var-Mi) { return }
    try { Sql-Calistir "CREATE DATABASE [$($Script:VeritabaniAdi)]" }
    catch {
        throw "'$($Script:VeritabaniAdi)' veritabanı yok ve $(Sql-Kimlik-Metni) ile oluşturulamadı. SQL Server yöneticisinden bu adla boş bir " +
              "veritabanı açıp bu kullanıcıyı onun sahibi (db_owner) yapmasını isteyin, sonra yeniden deneyin. Ayrıntı: $($_.Exception.Message)"
    }
}

# Kuruluma ve geri yüklemeye göre gereken veritabanı yetkileri. SQL kullanıcısıyla bağlanılıyorsa yetkiler o kullanıcıdadır.
function Veritabani-Yetkileri {
    if ($Script:SqlKullanici) {
        Veritabani-Olustur
        return
    }
    Servis-Veritabani-Yetkisi
    if (Sql-Servis-Adi) { Yedek-Veritabani-Yetkisi }
}

# Bağlantıyı dener: başarılıysa $null, değilse hata metni.
function Sql-Baglanti-Dene {
    try { [void](Sql-Calistir 'SELECT 1' -Skaler); return $null }
    catch { return $_.Exception.Message }
}

# Sunucunun varsayılan yedek klasörü (sunucunun kendi diskinde).
function Sunucu-Varsayilan-Yedek-Klasoru {
    try { [string](Sql-Calistir "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))" -Skaler) } catch { '' }
}

function Sql-Kimligi-Sor([string]$varsayilanKullanici = '') {
    $k = Read-Host "   SQL kullanıcı adı$(if ($varsayilanKullanici) { " [$varsayilanKullanici]" })"
    if ([string]::IsNullOrWhiteSpace($k)) { $k = $varsayilanKullanici }
    $p = Read-Host '   Parola' -AsSecureString
    $Script:SqlKullanici = $k.Trim()
    $Script:SqlParola = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p))
}

# Veritabanının nerede olacağını sorar ve bağlantı değişkenlerini ayarlar. Yeni SQL Server Express kurulacaksa $true döner.
#   -KurulumSecenegi: "yeni SQL Server Express kur" seçeneği de listelenir (KUR.bat).
function Sql-Baglantisi-Sor([switch]$KurulumSecenegi) {
    $ornekler = @(Sql-Ornekleri)
    $secenekler = @()
    if ($KurulumSecenegi -and -not ($ornekler -contains '.\SQLEXPRESS')) {
        $secenekler += , @('kur', 'Bu bilgisayara yeni SQL Server Express kur (ücretsiz; önerilen)', '')
    }
    foreach ($o in $ornekler) { $secenekler += , @('yerel', "Bu bilgisayarda kurulu SQL Server'ı kullan: $o", $o) }
    $secenekler += , @('uzak', "Başka bir bilgisayardaki SQL Server'ı kullan (bağlantı bilgilerini girin)", '')

    Write-Host ''
    Bilgi 'Disci verilerini hangi SQL Server''da tutsun?'
    for ($i = 0; $i -lt $secenekler.Count; $i++) { Bilgi "  $($i + 1)) $($secenekler[$i][1])" }
    do {
        $secim = Read-Host '   Seçiminiz [1]'
        if ([string]::IsNullOrWhiteSpace($secim)) { $secim = '1' }
    } until ($secim -match '^\d+$' -and [int]$secim -ge 1 -and [int]$secim -le $secenekler.Count)
    $tur, $ad, $ornek = $secenekler[[int]$secim - 1]

    switch ($tur) {
        'kur' {
            $Script:SqlOrnek = '.\SQLEXPRESS'; $Script:SqlKullanici = ''; $Script:SqlParola = ''
            return $true
        }
        'yerel' {
            $Script:SqlOrnek = $ornek
            if (Evet-Mi 'Windows kimlik doğrulaması kullanılsın mı? (Hayır derseniz SQL kullanıcı adı ve parolası sorulur)' $true) {
                $Script:SqlKullanici = ''; $Script:SqlParola = ''
            } else { Sql-Kimligi-Sor 'sa' }
        }
        'uzak' {
            do { $s = Read-Host '   Sunucu adı (ör. SUNUCU\SQLEXPRESS, SUNUCU ya da 192.168.1.10,1433)' } until (-not [string]::IsNullOrWhiteSpace($s))
            $Script:SqlOrnek = $s.Trim()
            Bilgi 'Kimlik doğrulama:'
            Bilgi '  1) SQL kullanıcı adı ve parolası (önerilen)'
            Bilgi '  2) Windows (yalnızca etki alanı/domain ağlarında; bu bilgisayarın hesabına sunucuda izin verilmeli)'
            $k = Read-Host '   Seçiminiz [1]'
            if ($k -eq '2') { $Script:SqlKullanici = ''; $Script:SqlParola = '' } else { Sql-Kimligi-Sor }
        }
    }
    $v = Read-Host "   Veritabanı adı [$($Script:VeritabaniAdi)]"
    if (-not [string]::IsNullOrWhiteSpace($v)) {
        if ($v.Trim() -notmatch '^[A-Za-z0-9_]{1,100}$') { Uyari 'Veritabanı adında yalnızca harf, rakam ve _ olabilir; varsayılan kullanılıyor.' }
        else { $Script:VeritabaniAdi = $v.Trim() }
    }
    return $false
}

# Seçilen sunucuya bağlanılana kadar sorar (vazgeçilirse hata). Bağlantı kurulunca döner.
function Sql-Baglantisini-Dogrula([switch]$SorabilirMi) {
    while ($true) {
        $hata = Sql-Baglanti-Dene
        if (-not $hata) { Tamam "SQL Server bağlantısı tamam: $($Script:SqlOrnek), $(Sql-Kimlik-Metni), veritabanı '$($Script:VeritabaniAdi)'."; return }
        Uyari "SQL Server'a bağlanılamadı: $hata"
        if (-not $SorabilirMi -or -not (Evet-Mi 'Bağlantı bilgilerini yeniden girmek ister misiniz?' $true)) {
            throw "SQL Server'a bağlanılamadı ($($Script:SqlOrnek), $(Sql-Kimlik-Metni))."
        }
        [void](Sql-Baglantisi-Sor)
    }
}

# Ayar dosyasında SQL parolası varsa dosyayı yalnızca yöneticiler, SYSTEM ve Disci hizmeti okuyabilir.
function Ayar-Dosyasini-Koru([string]$dosya) {
    if (-not $Script:SqlKullanici -or -not (Test-Path $dosya)) { return }
    icacls $dosya /inheritance:r /grant:r '*S-1-5-32-544:F' '*S-1-5-18:F' "$($Script:ServisHesabi):R" /Q | Out-Null
}

# Gece yedeğini SYSTEM hesabıyla çalışan zamanlanmış görev alır; yalnızca yedekleme yetkisi verilir.
function Yedek-Veritabani-Yetkisi {
    Sql-Calistir "IF SUSER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;"
    Sql-Calistir @"
IF USER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
ALTER ROLE db_backupoperator ADD MEMBER [NT AUTHORITY\SYSTEM];
"@ -veritabani $Script:VeritabaniAdi
}

function Parola-Uygun-Mu([string]$p) {
    $p.Length -ge 8 -and $p -cmatch '[a-zçğıöşü]' -and $p -cmatch '[A-ZÇĞİÖŞÜ]' -and $p -match '\d'
}

function Yonetici-Bilgisi-Sor {
    $eposta = Read-Host '   Yöneticinin e-postası (giriş adı)'
    while ($true) {
        $p1 = Read-Host '   Parola (en az 8 karakter, büyük/küçük harf ve rakam)' -AsSecureString
        $p2 = Read-Host '   Parola (tekrar)' -AsSecureString
        $d1 = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p1))
        $d2 = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($p2))
        if ($d1 -cne $d2) { Uyari 'Parolalar aynı değil.'; continue }
        if (-not (Parola-Uygun-Mu $d1)) { Uyari 'Parola kurala uymuyor.'; continue }
        return @{ Eposta = $eposta.Trim(); Parola = $d1 }
    }
}

function Program-Yanit-Veriyor-Mu([int]$port) {
    $bitis = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $bitis) {
        try {
            $r = Invoke-WebRequest "http://localhost:$port/Account/Login" -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -in 200, 301, 302, 307, 308) { return $true }
        } catch {
            $kod = $_.Exception.Response.StatusCode.value__
            if ($kod -in 301, 302, 307, 308) { return $true }
        }
        Start-Sleep -Seconds 3
    }
    return $false
}
