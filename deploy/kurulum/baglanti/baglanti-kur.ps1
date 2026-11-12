# Klinik ağındaki bir bilgisayarı Disci'ye bağlar: sunucunun sertifikasını güvenilenlere ekler ve
# masaüstüne kısayol koyar. Sunucu bilgileri aynı klasördeki sunucu.txt dosyasındadır (kurulum yazar).
$ErrorActionPreference = 'Stop'
$klasor = $PSScriptRoot
try {
    $ayar = @{}
    Get-Content (Join-Path $klasor 'sunucu.txt') -Encoding UTF8 | Where-Object { $_ -match '^\s*(\w+)\s*=\s*(.*)$' } |
        ForEach-Object { $ayar[$Matches[1]] = $Matches[2].Trim() }
    $sunucu = $ayar['Sunucu']; $port = $ayar['HttpsPort']; $ip = $ayar['IP']
    if (-not $sunucu) { throw 'sunucu.txt içinde sunucu adı yok.' }

    $cer = Join-Path $klasor 'Disci.cer'
    if (Test-Path $cer) {
        Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
        Write-Host "   Sertifika kuruldu (tarayıcı artık 'güvenli değil' demeyecek)." -ForegroundColor Green
    }

    # Ad çözülemezse (bazı ev modemleri) IP adresiyle bağlanılır.
    $adres = "https://${sunucu}:$port/"
    try { [void][Net.Dns]::GetHostAddresses($sunucu) } catch { if ($ip) { $adres = "https://${ip}:$port/" } }

    $masaustu = [Environment]::GetFolderPath('CommonDesktopDirectory')
    $ikon = Join-Path $env:ProgramData 'Disci\disci.ico'
    New-Item -ItemType Directory -Force -Path (Split-Path $ikon) | Out-Null
    if (Test-Path (Join-Path $klasor 'disci.ico')) { Copy-Item (Join-Path $klasor 'disci.ico') $ikon -Force }
    $metin = "[InternetShortcut]`r`nURL=$adres`r`nIconFile=$ikon`r`nIconIndex=0`r`n"
    [IO.File]::WriteAllText((Join-Path $masaustu 'Disci.url'), $metin, (New-Object Text.UTF8Encoding $false))
    Write-Host "   Masaüstüne 'Disci' kısayolu eklendi: $adres" -ForegroundColor Green

    try {
        $r = Invoke-WebRequest "$adres/Account/Login" -UseBasicParsing -TimeoutSec 10 -MaximumRedirection 0 -ErrorAction Stop
        Write-Host '   Sunucuya ulaşıldı.' -ForegroundColor Green
    } catch {
        if ($_.Exception.Response) { Write-Host '   Sunucuya ulaşıldı.' -ForegroundColor Green }
        else { Write-Host "   UYARI: Sunucuya şu an ulaşılamadı ($adres). Sunucu bilgisayar açık ve aynı ağda mı?" -ForegroundColor Yellow }
    }
    Start-Process $adres
} catch {
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
