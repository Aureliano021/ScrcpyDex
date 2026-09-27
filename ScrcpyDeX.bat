@echo off
title ScrcpyDeX - Samsung DeX Compatible Client (USB)
cd /d "%~dp0"

REM 1. If native executable exists, launch it
if exist "%~dp0ScrcpyDeX.exe" (
    start "" "%~dp0ScrcpyDeX.exe" %*
    exit /b 0
)

REM 2. Determine PowerShell executable (pwsh preferred, fallback to powershell)
set "PS_EXE=powershell.exe"
where pwsh.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 set "PS_EXE=pwsh.exe"

REM 3. If --cli flag is passed or WinUI script does not exist, run console orchestrator
if /i "%~1"=="--cli" goto :run_cli
if not exist "%~dp0client\ScrcpyDeX-WinUI.ps1" goto :run_cli

REM Launch WinUI Control Center
start "" %PS_EXE% -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "%~dp0client\ScrcpyDeX-WinUI.ps1" %*
exit /b 0

:run_cli
set "CLI_SCRIPT=%~dp0tools\launchers\run-scrcpydex.ps1"
if not exist "%CLI_SCRIPT%" set "CLI_SCRIPT=%~dp0run-scrcpydex.ps1"
%PS_EXE% -ExecutionPolicy Bypass -NoProfile -File "%CLI_SCRIPT%" %*
