# ScrcpyDeX - Samsung DeX for PC via USB
# Ativa o motor nativo Samsung DeX no celular e abre em 1 unica janela nativa

$ErrorActionPreference = "Continue"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "         ScrcpyDeX - Samsung DeX for PC          " -ForegroundColor Cyan
Write-Host "   Janela Unica Nativa via USB (Zero Wi-Fi)      " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Verificar Galaxy conectado via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "ERRO: Nenhum aparelho Samsung detectado via ADB!" -ForegroundColor Red
    Write-Host "Conecte o Galaxy S23 via cabo USB com Depuracao USB ativa." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/4] Galaxy detectado via ADB." -ForegroundColor Green

# 2. Localizar binarios
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"
$scrcpyExe = "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\scrcpy.exe"

if (-not (Test-Path $scrcpyExe)) {
    $cmd = Get-Command scrcpy -ErrorAction SilentlyContinue
    if ($cmd) { $scrcpyExe = $cmd.Source }
    else {
        Write-Host "ERRO: Binario do scrcpy nao encontrado." -ForegroundColor Red
        exit 1
    }
}

# 3. Limpar processos anteriores no celular
Write-Host "[2/4] Preparando celular..." -ForegroundColor Green
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
adb shell "pkill -f scrcpy" 2>$null | Out-Null
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 4. Iniciar o ativador DeX em background no celular
Write-Host "[3/4] Ativando motor nativo Samsung DeX via loopback Miracast..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server activate" -PassThru

# Aguardar o surgimento especifico do display 'ScrcpyDeX'
$dexId = $null
$retries = 0
while ($retries -lt 30 -and -not $dexId) {
    Start-Sleep -Milliseconds 400
    $dumpStr = (adb shell "dumpsys display") -join "`n"
    if ($dumpStr -match 'DisplayInfo\{"ScrcpyDeX", displayId (\d+)') {
        $dexId = $Matches[1]
        break
    } elseif ($dumpStr -match 'Display (\d+):[\s\S]*?mPrimaryDisplayDevice=ScrcpyDeX') {
        $dexId = $Matches[1]
        break
    }
    $retries++
}

if (-not $dexId) {
    Write-Host "ERRO: Display do Samsung DeX nao foi detectado a tempo." -ForegroundColor Red
    adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    exit 1
}

Write-Host "=================================================" -ForegroundColor Green
Write-Host "  Samsung DeX Ativado! Display ID: $dexId        " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "[4/4] Abrindo janela unica do Samsung DeX no PC..." -ForegroundColor Cyan
Write-Host ""
Write-Host ">>> CONTROLES DO MOUSE FISICO (UHID) <<<" -ForegroundColor Yellow
Write-Host "- Para soltar o mouse de volta para o Windows: Pressione [ALT ESQUERDO]" -ForegroundColor Yellow
Write-Host "- Para alternar Tela Cheia (Fullscreen):       Pressione [ALT + F]" -ForegroundColor Yellow
Write-Host "- Para abrir menus de contexto:               Clique com Botao Direito" -ForegroundColor Yellow
Write-Host ""

# 5. Abrir a janela unica e nativa do DeX com scrcpy em modo UHID (Mouse Fisico Real)
try {
    & $scrcpyExe --display-id=$dexId --mouse=uhid --stay-awake --window-title="Samsung DeX (ScrcpyDeX)"
}
finally {
    # 6. Desligamento 100% automatico e garantido ao fechar a janela
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Gray
    Write-Host "Encerrando sessao DeX no Galaxy S23..." -ForegroundColor Gray
    adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    adb shell "pkill -f scrcpy" 2>$null | Out-Null
    if ($serverProc -and -not $serverProc.HasExited) {
        Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Sessao DeX desligada com sucesso! Celular restaurado." -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Gray
}
