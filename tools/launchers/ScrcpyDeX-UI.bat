@echo off
title ScrcpyDeX — Control Center
set "ROOT_DIR=%~dp0..\..\"
if exist "%~dp0client\ScrcpyDeX-WinUI.ps1" set "ROOT_DIR=%~dp0"
cd /d "%ROOT_DIR%"

if exist "%ROOT_DIR%ScrcpyDeX.exe" (
    start "" "%ROOT_DIR%ScrcpyDeX.exe" %*
    exit /b 0
)

set "UI_SCRIPT=%ROOT_DIR%src\ScrcpyDex.App.Wpf\ScrcpyDeX-WinUI.ps1"
if not exist "%UI_SCRIPT%" set "UI_SCRIPT=%ROOT_DIR%client\ScrcpyDeX-WinUI.ps1"

where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    start "" pwsh.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "%UI_SCRIPT%" %*
) else (
    start "" powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "%UI_SCRIPT%" %*
)
