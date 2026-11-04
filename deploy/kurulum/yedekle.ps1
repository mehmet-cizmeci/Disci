# Disci yedeği: veritabanının tam yedeğini ve fotoğrafların kopyasını alır, eski yedekleri siler.
# Kurulum bunu her gün (varsayılan 14:00) SYSTEM hesabıyla çalışan "Disci Gunluk Yedek" görevine bağlar; elle de çalıştırılabilir.
param(
    [string]$YedekKlasoru = 'C:\DisciVeri\Yedekler',
    [string]$DosyaKlasoru = 'C:\DisciVeri\files',
    [int]$Gun = 14
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')

$log = Join-Path $YedekKlasoru 'yedek.log'
function Kaydet([string]$metin) {
    $satir = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $metin"
    Add-Content -Path $log -Value $satir -Encoding UTF8
    Write-Host $satir
}

try {
    New-Item -ItemType Directory -Force -Path $YedekKlasoru | Out-Null
    $dosya = Join-Path $YedekKlasoru "Disci_$(Get-Date -Format 'yyyyMMdd_HHmm').bak"
    Sql-Calistir "BACKUP DATABASE [$($Script:VeritabaniAdi)] TO DISK = N'$dosya' WITH INIT, CHECKSUM, NAME = N'Disci yedeği'"
    Kaydet "Veritabanı yedeklendi: $dosya ($([math]::Round((Get-Item $dosya).Length / 1MB, 1)) MB)"

    if (Test-Path $DosyaKlasoru) {
        robocopy $DosyaKlasoru (Join-Path $YedekKlasoru 'files') /MIR /R:2 /W:2 /NFL /NDL /NP /NJH /NJS | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Fotoğraf kopyalama başarısız (robocopy $LASTEXITCODE)" }
        Kaydet 'Fotoğraflar kopyalandı.'
    }

    $eskiler = Get-ChildItem $YedekKlasoru -Filter 'Disci_*.bak' | Where-Object LastWriteTime -lt (Get-Date).AddDays(-$Gun)
    foreach ($e in $eskiler) { Remove-Item $e.FullName; Kaydet "Eski yedek silindi: $($e.Name)" }
    exit 0
} catch {
    Kaydet "HATA: $($_.Exception.Message)"
    exit 1
}
