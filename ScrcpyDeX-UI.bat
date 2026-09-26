@echo off
title ScrcpyDeX — Control Center
cd /d "%~dp0"

if exist "%~dp0ScrcpyDeX.exe" (
    start "" "%~dp0ScrcpyDeX.exe" %*
    exit /b 0
)

where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    start "" pwsh.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "%~dp0client\ScrcpyDeX-WinUI.ps1" %*
) else (
    start "" powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "%~dp0client\ScrcpyDeX-WinUI.ps1" %*
)
