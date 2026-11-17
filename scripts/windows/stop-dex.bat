@echo off
setlocal EnableExtensions

if "%~1"=="-h" goto :usage
if "%~1"=="--help" goto :usage
if "%~1"=="/?" goto :usage

set "TARGET_SERIAL=%~1"
if /i "%~1"=="-s" set "TARGET_SERIAL=%~2"

title Disconnect ScrcpyDeX (Samsung DeX Compatible Client)
echo =================================================
echo        Disconnecting ScrcpyDeX Session           
echo =================================================
echo [1/2] Terminating ScrcpyDeX client processes (scrcpy)...
REM Scope process termination strictly to scrcpy.exe instances launched by ScrcpyDeX.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-CimInstance Win32_Process -Filter \"Name='scrcpy.exe'\" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -match '--window-title=ScrcpyDeX' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }" >nul 2>&1

REM Resolve adb executable location dynamically
set "ADB_EXE="
for /f "delims=" %%I in ('where adb.exe 2^>nul') do (
    if not defined ADB_EXE set "ADB_EXE=%%I"
)

if not defined ADB_EXE (
    for /f "delims=" %%I in ('where scrcpy.exe 2^>nul') do (
        if not defined ADB_EXE if exist "%%~dpIadb.exe" set "ADB_EXE=%%~dpIadb.exe"
    )
)

if not defined ADB_EXE if exist "%LOCALAPPDATA%\Microsoft\WinGet\Links\adb.exe" (
    set "ADB_EXE=%LOCALAPPDATA%\Microsoft\WinGet\Links\adb.exe"
)

if not defined ADB_EXE if exist "%LOCALAPPDATA%\Microsoft\WinGet\Packages" (
    for /d %%P in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\Genymobile.scrcpy*") do (
        if not defined ADB_EXE if exist "%%~fP\adb.exe" (
            set "ADB_EXE=%%~fP\adb.exe"
        )
        if not defined ADB_EXE (
            for /d %%S in ("%%~fP\scrcpy-*") do (
                if not defined ADB_EXE if exist "%%~fS\adb.exe" (
                    set "ADB_EXE=%%~fS\adb.exe"
                )
            )
        )
    )
)

if not defined ADB_EXE if exist "%USERPROFILE%\scoop\apps\scrcpy\current\adb.exe" (
    set "ADB_EXE=%USERPROFILE%\scoop\apps\scrcpy\current\adb.exe"
)
if not defined ADB_EXE if exist "%USERPROFILE%\scoop\apps\adb\current\adb.exe" (
    set "ADB_EXE=%USERPROFILE%\scoop\apps\adb\current\adb.exe"
)
if not defined ADB_EXE if exist "%ProgramData%\chocolatey\bin\adb.exe" (
    set "ADB_EXE=%ProgramData%\chocolatey\bin\adb.exe"
)
if not defined ADB_EXE if exist "%ProgramFiles%\scrcpy\adb.exe" (
    set "ADB_EXE=%ProgramFiles%\scrcpy\adb.exe"
)
if not defined ADB_EXE if exist "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" (
    set "ADB_EXE=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
)

if not defined ADB_EXE (
    echo [WARN] adb.exe not found in PATH or standard installation locations.
    echo        Skipping device-side DeX disconnect.
    goto :done
)

set "ADB_TARGET="
if defined TARGET_SERIAL set ADB_TARGET=-s "%TARGET_SERIAL%"

echo [2/2] Sending disconnect signal to Android device...
"%ADB_EXE%" %ADB_TARGET% shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" >nul 2>&1
"%ADB_EXE%" %ADB_TARGET% shell "am broadcast -a com.sec.android.app.desktoplauncher.ACTION_STOP_DESKTOP_MODE ; am broadcast -a com.samsung.android.desktopmode.action.DESKTOP_MODE_UPDATE --ez enabled false ; pkill -f com.scrcpydex.server.Server" >nul 2>&1

:done
echo.
echo DeX session disconnected. Device display restored.
endlocal
exit /b 0

:usage
echo Usage: stop-dex.bat [serial ^| -s ^<serial^>]
echo Terminates ScrcpyDeX-managed scrcpy.exe instances and sends DeX disconnect signals via ADB.
endlocal
exit /b 0
