# Disci'yi kaldırır: hizmeti, güvenlik duvarı kurallarını ve yedek görevini siler.
# Veritabanı, fotoğraflar ve yedekler SİLİNMEZ; isterseniz onları elle silebilirsiniz.
param(
    [string]$Hedef = 'C:\Disci'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ortak.ps1')

if (-not (Yonetici-Mi)) { Write-Host 'Bu betik yönetici olarak çalıştırılmalı.' -ForegroundColor Red; exit 1 }
if (-not (Evet-Mi 'Disci hizmeti, güvenlik duvarı kuralları ve yedek görevi kaldırılsın mı?' $false)) { exit 0 }

if (Get-Service $Script:ServisAdi -ErrorAction SilentlyContinue) {
    Stop-Service $Script:ServisAdi -Force -ErrorAction SilentlyContinue
    sc.exe delete $Script:ServisAdi | Out-Null
    Tamam 'Hizmet kaldırıldı.'
}
Get-NetFirewallRule -DisplayName 'Disci *' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Tamam 'Güvenlik duvarı kuralları kaldırıldı.'
Unregister-ScheduledTask -TaskName 'Disci Gece Yedegi' -Confirm:$false -ErrorAction SilentlyContinue
Tamam 'Yedek görevi kaldırıldı.'

Write-Host ''
Write-Host 'Silinmeyenler (gerekirse elle silin):'
Write-Host "   Program dosyaları  : $Hedef"
Write-Host '   Veriler ve yedekler: C:\DisciVeri (veya kurulumda verdiğiniz klasör)'
Write-Host "   Veritabanı         : SQL Server Express içindeki '$($Script:VeritabaniAdi)'"
Write-Host '   SQL Server Express : Ayarlar > Uygulamalar bölümünden kaldırılabilir'
