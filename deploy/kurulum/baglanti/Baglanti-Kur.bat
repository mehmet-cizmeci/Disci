@echo off
rem Disci'ye bu bilgisayardan baglanmak icin cift tiklayin (bir kez yeterli).
rem Sertifikayi kurar ve masaustune "Disci" kisayolu koyar. Yonetici izni istenir.
chcp 65001 >nul
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0baglanti-kur.ps1"
echo.
pause
