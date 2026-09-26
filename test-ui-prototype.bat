@echo off
title ScrcpyDeX — Automated UI Verification Suite (Stage 4)
cd /d "%~dp0"

where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    pwsh.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0test-ui-prototype.ps1"
) else (
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0test-ui-prototype.ps1"
)

pause
