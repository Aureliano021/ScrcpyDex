@echo off
title Disconnect ScrcpyDeX
echo =================================================
echo        Disconnecting Samsung DeX and Scrcpy        
echo =================================================
echo [1/3] Terminating Windows processes (scrcpy, ffplay)...
taskkill /F /IM scrcpy.exe 2>nul
taskkill /F /IM ffplay.exe 2>nul

echo [2/3] Sending disconnect command to Galaxy S23...
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>nul
adb shell "pkill -f com.scrcpydex.server.Server" 2>nul
adb shell "pkill -f DexTriggerTest" 2>nul

echo [3/3] Cleaning up USB network forward tunnels...
adb forward --remove tcp:27183 2>nul
adb forward --remove tcp:27184 2>nul

echo.
echo DeX disconnected successfully! Device restored to normal.
ping -n 3 127.0.0.1 >nul
