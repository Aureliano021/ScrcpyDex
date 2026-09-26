@echo off
title ScrcpyDeX - Samsung DeX Compatible Client (USB)
cd /d "%~dp0"

if exist "%~dp0ScrcpyDeX.exe" (
    start "" "%~dp0ScrcpyDeX.exe" %*
    exit /b 0
)

where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    pwsh.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0run-scrcpydex.ps1"
) else (
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0run-scrcpydex.ps1"
)
