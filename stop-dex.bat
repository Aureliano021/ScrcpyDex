@echo off
title Disconnect ScrcpyDeX (Samsung DeX Compatible Client)
echo =================================================
echo        Disconnecting ScrcpyDeX Session           
echo =================================================
echo [1/3] Terminating local client processes (scrcpy, ffplay)...
REM Note: Emergency kill switch terminates running scrcpy.exe and ffplay.exe instances on host PC.
taskkill /F /IM scrcpy.exe 2>nul
taskkill /F /IM ffplay.exe 2>nul

REM Resolve adb executable location
set "ADB_EXE=adb"
where adb >nul 2>&1
if %ERRORLEVEL% neq 0 (
    if exist "%LOCALAPPDATA%\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe" (
        set "ADB_EXE=%LOCALAPPDATA%\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe"
    ) else if exist "%ProgramFiles%\scrcpy\adb.exe" (
        set "ADB_EXE=%ProgramFiles%\scrcpy\adb.exe"
    )
)

echo [2/3] Sending disconnect signal to Android device...
"%ADB_EXE%" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>nul
"%ADB_EXE%" shell "pkill -f com.scrcpydex.server.Server" 2>nul
"%ADB_EXE%" shell "pkill -f DexTriggerTest" 2>nul

echo [3/3] Cleaning up USB network forward tunnels...
"%ADB_EXE%" forward --remove tcp:27183 2>nul
"%ADB_EXE%" forward --remove tcp:27184 2>nul

echo.
echo DeX session disconnected. Device display restored.
ping -n 2 127.0.0.1 >nul
