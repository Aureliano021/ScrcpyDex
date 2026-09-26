# Script de Teste do Hard Gate 1 (ScrcpyDeX Server)
# Valida a ativacao do Samsung DeX e streaming H.264 via USB

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "      Teste de Validação: HARD GATE 1            " -ForegroundColor Cyan
Write-Host "     ScrcpyDeX Server (H.264 Stream via USB)     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Verificar celular conectado via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "[ERRO] Nenhum aparelho Samsung detectado via ADB!" -ForegroundColor Red
    Write-Host "Conecte o Galaxy S23 via cabo USB e certifique-se de que a Depuracao USB esta ativa." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/4] Galaxy detectado via ADB." -ForegroundColor Green

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"

if (-not (Test-Path $serverJar)) {
    Write-Host "[ERRO] Binario $serverJar nao encontrado. Execute build-server.ps1 primeiro." -ForegroundColor Red
    exit 1
}

# 2. Verificar presenca do ffplay
$ffplayCmd = Get-Command ffplay -ErrorAction SilentlyContinue
if (-not $ffplayCmd) {
    Write-Host "[AVISO] O player 'ffplay' (FFmpeg) nao foi encontrado no PATH." -ForegroundColor Yellow
    Write-Host "Voce pode instala-lo rapidamente executando no terminal:" -ForegroundColor Yellow
    Write-Host "  winget install Gyan.FFmpeg" -ForegroundColor Cyan
    Write-Host "Apos instalar, feche e abra o terminal novamente." -ForegroundColor Yellow
    Write-Host "`nTentando continuar caso queira apenas iniciar o servidor..." -ForegroundColor Gray
}

# 3. Limpeza de processos anteriores
Write-Host "[2/4] Limpando processos anteriores no celular..." -ForegroundColor Green
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
adb forward tcp:27183 tcp:27183

# 4. Enviar JAR para o dispositivo
Write-Host "[3/4] Enviando scrcpydex-server.jar para /data/local/tmp/..." -ForegroundColor Green
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 5. Iniciar o servidor no celular em background e abrir ffplay
Write-Host "[4/4] Disparando o ScrcpyDeX Server no dispositivo..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server" -PassThru

Write-Host "Servidor disparado (PID: $($serverProc.Id)). Aguardando inicialização do DeX (4s)..." -ForegroundColor Gray
Start-Sleep -Seconds 4

if ($ffplayCmd) {
    Write-Host "Iniciando ffplay com baixa latencia (60fps, nobuffer)..." -ForegroundColor Green
    & ffplay -f h264 -framerate 60 -fflags nobuffer -flags low_delay -probesize 32768 -analyzeduration 100000 tcp://127.0.0.1:27183
} else {
    Write-Host "`nServidor rodando e aguardando conexao na porta 27183!" -ForegroundColor Green
    Write-Host "Para assistir o stream, conecte qualquer player H.264 em tcp://127.0.0.1:27183" -ForegroundColor Cyan
}

Write-Host "`nPressione qualquer tecla para encerrar a sessão DeX..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Finalizando sessão..." -ForegroundColor Gray
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
if ($serverProc -and -not $serverProc.HasExited) {
    Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
}
Write-Host "Sessão DeX encerrada com sucesso." -ForegroundColor Green
