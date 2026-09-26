@echo off
title ScrcpyDeX - Samsung DeX for PC (USB)
cd /d "%~dp0"

where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    pwsh.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0run-scrcpydex.ps1"
) else (
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0run-scrcpydex.ps1"
)
