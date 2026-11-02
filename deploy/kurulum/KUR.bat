@echo off
rem Disci kurulumu - cift tiklayin. Yonetici izni istenir.
rem Secenekler (Komut Isteminden):  KUR.bat -EskiVeri "D:\DentaLineSuit\DL2500DB"
rem Deneme (degisiklik yapmaz):     KUR.bat -Deneme
chcp 65001 >nul
set "ARGS=%*"
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%ARGS%' -Verb RunAs"
    exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0kur.ps1" %*
echo.
pause
