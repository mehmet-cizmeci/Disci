# Kullanım kılavuzunun PDF'ini üretir: docs\Kullanim-Kilavuzu.md → docs\Kullanim-Kilavuzu.pdf
# Gerekenler: .NET 10 SDK ve Google Chrome. Kullanım: powershell -ExecutionPolicy Bypass -File docs\kilavuz-pdf.ps1
$ErrorActionPreference = 'Stop'
$docs = $PSScriptRoot
$chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) { throw 'Chrome veya Edge bulunamadı.' }

Push-Location $docs
try {
    dotnet run (Join-Path $docs 'kilavuz-pdf.cs') -- 'Kullanim-Kilavuzu.md' 'Kullanim-Kilavuzu.html' | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'HTML üretilemedi.' }
    $html = (Join-Path $docs 'Kullanim-Kilavuzu.html')
    $pdf = (Join-Path $docs 'Kullanim-Kilavuzu.pdf')
    $profil = Join-Path $env:TEMP "disci-pdf-$(Get-Random)"
    & $chrome --headless=new --disable-gpu --no-pdf-header-footer "--user-data-dir=$profil" "--print-to-pdf=$pdf" ("file:///" + $html.Replace('\', '/')) | Out-Null
    Start-Sleep -Seconds 2
    Remove-Item $html -ErrorAction SilentlyContinue
    if (-not (Test-Path $pdf)) { throw 'PDF oluşmadı.' }
    Write-Host "PDF hazır: $pdf ($([math]::Round((Get-Item $pdf).Length / 1MB, 1)) MB)" -ForegroundColor Green
} finally { Pop-Location }
