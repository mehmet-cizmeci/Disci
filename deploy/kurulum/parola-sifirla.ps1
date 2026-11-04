# Yönetici parolasını unuttuysanız: verilen e-postadaki hesabın parolasını değiştirir, kilidini açar ve yönetici yapar.
# O e-postada hesap yoksa yeni bir yönetici hesabı açar.
# Kullanım (programın kurulu olduğu bilgisayarda, yönetici PowerShell): C:\Disci\betikler\parola-sifirla.ps1
param(
    [string]$Hedef = 'C:\Disci'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')

try {
    if (-not (Yonetici-Mi)) { throw 'Bu betik yönetici olarak çalıştırılmalı.' }
    $arac = Join-Path $Hedef 'import\Disci.LegacyImport.exe'
    if (-not (Test-Path $arac)) { throw "Araç bulunamadı: $arac" }

    $bilgi = Yonetici-Bilgisi-Sor
    # Parola komut satırında görünmesin diye yalnızca bu işlem için ortam değişkeniyle verilir.
    $env:DISCI_YENI_PAROLA = $bilgi.Parola
    try {
        & $arac --parola-sifirla $bilgi.Eposta --connection (Baglanti-Metni) | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Parola değiştirilemedi (çıkış kodu $LASTEXITCODE)." }
    } finally {
        Remove-Item Env:\DISCI_YENI_PAROLA -ErrorAction SilentlyContinue
        $bilgi = $null
    }
    exit 0
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
