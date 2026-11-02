# Disci kurulum betiklerinin ortak işlevleri. Doğrudan çalıştırılmaz; diğer betikler bunu yükler.

$Script:ServisAdi = 'Disci'
$Script:ServisHesabi = 'NT SERVICE\Disci'
$Script:SqlOrnek = '.\SQLEXPRESS'
$Script:VeritabaniAdi = 'Disci'

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

function Baglanti-Metni([string]$veritabani = $Script:VeritabaniAdi) {
    "Server=$($Script:SqlOrnek);Database=$veritabani;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}

# sqlcmd gerektirmeden T-SQL çalıştırır (PowerShell 5.1'deki .NET Framework SqlClient ile).
function Sql-Calistir([string]$sql, [string]$veritabani = 'master', [switch]$Skaler) {
    $baglanti = New-Object System.Data.SqlClient.SqlConnection "Server=$($Script:SqlOrnek);Database=$veritabani;Integrated Security=True;TrustServerCertificate=True"
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
    Sql-Calistir @"
IF DB_ID(N'$($Script:VeritabaniAdi)') IS NULL CREATE DATABASE [$($Script:VeritabaniAdi)];
IF SUSER_ID(N'$($Script:ServisHesabi)') IS NULL CREATE LOGIN [$($Script:ServisHesabi)] FROM WINDOWS;
"@
    Sql-Calistir @"
IF USER_ID(N'$($Script:ServisHesabi)') IS NULL CREATE USER [$($Script:ServisHesabi)] FOR LOGIN [$($Script:ServisHesabi)];
ALTER ROLE db_owner ADD MEMBER [$($Script:ServisHesabi)];
"@ -veritabani $Script:VeritabaniAdi
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
    $eposta = Read-Host '   İlk yöneticinin e-postası (giriş adı)'
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

# İlk yönetici, hizmet bir kez başlarken okunan geçici ortam değişkenleriyle açılır; sonra bu değişkenler silinir.
function Ilk-Yoneticiyi-Ac($yonetici, [int]$port) {
    $anahtar = "HKLM:\SYSTEM\CurrentControlSet\Services\$($Script:ServisAdi)"
    Set-ItemProperty -Path $anahtar -Name Environment -Type MultiString -Value @(
        "Seed__AdminEmail=$($yonetici.Eposta)", "Seed__AdminPassword=$($yonetici.Parola)")
    try {
        Restart-Service $Script:ServisAdi -Force
        $bitis = (Get-Date).AddSeconds(120)
        while ((Get-Date) -lt $bitis -and (Kullanici-Sayisi) -eq 0) { Start-Sleep -Seconds 3 }
    } finally {
        Remove-ItemProperty -Path $anahtar -Name Environment -ErrorAction SilentlyContinue
    }
    if ((Kullanici-Sayisi) -eq 0) {
        throw "İlk yönetici oluşturulamadı. Olay Görüntüleyicisi > Windows Günlükleri > Uygulama altında 'Disci' kayıtlarına bakın."
    }
    Tamam "İlk yönetici oluşturuldu: $($yonetici.Eposta)"
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
