@echo off
title Desconectar ScrcpyDeX
echo =================================================
echo        Desconectando Samsung DeX e Scrcpy        
echo =================================================
echo [1/3] Finalizando processos no Windows (scrcpy, ffplay)...
taskkill /F /IM scrcpy.exe 2>nul
taskkill /F /IM ffplay.exe 2>nul

echo [2/3] Enviando comando de desconexao para o Galaxy S23...
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>nul
adb shell "pkill -f com.scrcpydex.server.Server" 2>nul
adb shell "pkill -f DexTriggerTest" 2>nul

echo [3/3] Limpando tneis de rede USB...
adb forward --remove tcp:27183 2>nul
adb forward --remove tcp:27184 2>nul

echo.
echo DeX desconectado com sucesso! Celular restaurado ao normal.
ping -n 3 127.0.0.1 >nul
