# Script de Validacao do Hard Gate 2: Control Channel & Input Injection
# Valida conexao TCP 27184, leitura de handshake e injecao de mouse/teclado no DeX

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "      Teste de Validacao: HARD GATE 2            " -ForegroundColor Cyan
Write-Host "  ScrcpyDeX Control Channel & Input Injection   " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Verificar aparelho Samsung conectado via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "[ERRO] Nenhum aparelho Samsung detectado via ADB!" -ForegroundColor Red
    Write-Host "Conecte o Galaxy S23 via cabo USB com Depuracao USB ativa." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/5] Galaxy detectado via ADB." -ForegroundColor Green

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"

if (-not (Test-Path $serverJar)) {
    Write-Host "[ERRO] Binario $serverJar nao encontrado. Execute build-server.ps1 primeiro." -ForegroundColor Red
    exit 1
}

# 2. Limpar processos anteriores e configurar Port Forwarding
Write-Host "[2/5] Limpando processos anteriores e configurando portas..." -ForegroundColor Green
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
adb forward tcp:27183 tcp:27183
adb forward tcp:27184 tcp:27184

# 3. Enviar JAR atualizado para o celular
Write-Host "[3/5] Enviando scrcpydex-server.jar para /data/local/tmp/..." -ForegroundColor Green
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 4. Iniciar o ScrcpyDeX Server no aparelho
Write-Host "[4/5] Disparando o ScrcpyDeX Server no dispositivo..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server" -PassThru

Write-Host "Servidor disparado (PID: $($serverProc.Id)). Aguardando DeX iniciar (4s)..." -ForegroundColor Gray
Start-Sleep -Seconds 4

# 5. Conectar ao Control Channel (TCP 27184)
Write-Host "[5/5] Conectando ao Control Channel (127.0.0.1:27184)..." -ForegroundColor Cyan
$tcpClient = $null
$stream = $null

try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.NoDelay = $true
    $tcpClient.Connect("127.0.0.1", 27184)
    $stream = $tcpClient.GetStream()
    Write-Host "Conexao de controle estabelecida!" -ForegroundColor Green

    # Funcao auxiliar para ler inteiros Big-Endian
    function Read-Int32BE($s) {
        $b = New-Object byte[] 4
        $read = $s.Read($b, 0, 4)
        if ($read -lt 4) { throw "Fim inesperado do stream de controle" }
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
        return [BitConverter]::ToInt32($b, 0)
    }

    # Ler MSG_DEX_READY (0x01)
    $msgType = $stream.ReadByte()
    if ($msgType -eq 0x01) {
        $displayId = Read-Int32BE $stream
        Write-Host ">>> RECEBIDO MSG_DEX_READY: DisplayId = $displayId <<<" -ForegroundColor Green
    } else {
        Write-Host "[AVISO] Esperava 0x01, recebido 0x$([Convert]::ToString($msgType, 16))" -ForegroundColor Yellow
    }

    # Ler MSG_DISPLAY_INFO (0x02)
    $msgType2 = $stream.ReadByte()
    if ($msgType2 -eq 0x02) {
        $width = Read-Int32BE $stream
        $height = Read-Int32BE $stream
        $dpi = Read-Int32BE $stream
        Write-Host ">>> RECEBIDO MSG_DISPLAY_INFO: ${width}x${height} @ ${dpi} DPI <<<" -ForegroundColor Green
    }

    # Iniciar ffplay em processo separado com janela 1280x720 para visualizacao
    $ffplayCmd = Get-Command ffplay -ErrorAction SilentlyContinue
    $ffplayProc = $null
    if ($ffplayCmd) {
        Write-Host "`nIniciando janela de video (1280x720, 60fps, nobuffer)..." -ForegroundColor Cyan
        $ffplayProc = Start-Process -FilePath "ffplay" -ArgumentList "-f h264 -framerate 60 -fflags nobuffer -flags low_delay -probesize 32768 -analyzeduration 100000 -window_title `"ScrcpyDeX - Hard Gate 2 (Video + Input)`" -x 1280 -y 720 tcp://127.0.0.1:27183" -PassThru
        Start-Sleep -Seconds 2
    }

    Write-Host "`n-------------------------------------------------" -ForegroundColor Gray
    Write-Host "Controle Interativo de Teste (Porta 27184):" -ForegroundColor White
    Write-Host " [W] Tecla Windows / Menu DeX (Key 117 - KEYCODE_META_LEFT)" -ForegroundColor Green
    Write-Host " [S] Clicar no Botao Apps/Iniciar do DeX (x=35, y=1050)" -ForegroundColor Green
    Write-Host " [R] Alternador de Aplicativos (Key 187 - KEYCODE_APP_SWITCH)" -ForegroundColor Yellow
    Write-Host " [N] Clicar no Painel de Notificacoes (x=1850, y=1050)" -ForegroundColor Yellow
    Write-Host " [H] Pressionar Home (Key 3 - KEYCODE_HOME)" -ForegroundColor Yellow
    Write-Host " [B] Pressionar Voltar (Key 4 - KEYCODE_BACK)" -ForegroundColor Yellow
    Write-Host " [A] Teste Automatico Completo (Menu + Apps + Voltar)" -ForegroundColor Cyan
    Write-Host " [Q] Sair e Encerrar DeX" -ForegroundColor Red
    Write-Host "-------------------------------------------------" -ForegroundColor Gray

    function Send-MouseMove($s, $x, $y) {
        $buf = New-Object byte[] 9
        $buf[0] = 0x10 # MSG_MOUSE_MOVE
        $xb = [BitConverter]::GetBytes([int]$x)
        $yb = [BitConverter]::GetBytes([int]$y)
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($xb); [Array]::Reverse($yb) }
        [Array]::Copy($xb, 0, $buf, 1, 4)
        [Array]::Copy($yb, 0, $buf, 5, 4)
        $s.Write($buf, 0, 9)
        $s.Flush()
    }

    function Send-MouseButton($s, $x, $y, $button, $action) {
        $buf = New-Object byte[] 11
        $buf[0] = 0x11 # MSG_MOUSE_BUTTON
        $xb = [BitConverter]::GetBytes([int]$x)
        $yb = [BitConverter]::GetBytes([int]$y)
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($xb); [Array]::Reverse($yb) }
        [Array]::Copy($xb, 0, $buf, 1, 4)
        [Array]::Copy($yb, 0, $buf, 5, 4)
        $buf[9] = [byte]$button
        $buf[10] = [byte]$action
        $s.Write($buf, 0, 11)
        $s.Flush()
    }

    function Send-Click($s, $x, $y) {
        Send-MouseButton $s $x $y 1 0 # DOWN
        Start-Sleep -Milliseconds 60
        Send-MouseButton $s $x $y 1 1 # UP
    }

    function Send-Key($s, $keyCode) {
        # DOWN
        $bufDown = New-Object byte[] 6
        $bufDown[0] = 0x12 # MSG_KEY_EVENT
        $kb = [BitConverter]::GetBytes([int]$keyCode)
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($kb) }
        [Array]::Copy($kb, 0, $bufDown, 1, 4)
        $bufDown[5] = 0 # ACTION_DOWN
        $s.Write($bufDown, 0, 6)
        $s.Flush()
        Start-Sleep -Milliseconds 50

        # UP
        $bufUp = New-Object byte[] 6
        $bufUp[0] = 0x12
        [Array]::Copy($kb, 0, $bufUp, 1, 4)
        $bufUp[5] = 1 # ACTION_UP
        $s.Write($bufUp, 0, 6)
        $s.Flush()
    }

    $keepRunning = $true
    while ($keepRunning) {
        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true).Key
            switch ($key) {
                'W' {
                    Write-Host "[INPUT] Enviando KEYCODE_META_LEFT / Windows Key (117)..." -ForegroundColor Green
                    Send-Key $stream 117
                }
                'S' {
                    Write-Host "[INPUT] Clicando no Menu Iniciar do DeX em (35, 1050)..." -ForegroundColor Green
                    Send-Click $stream 35 1050
                }
                'R' {
                    Write-Host "[INPUT] Enviando KEYCODE_APP_SWITCH / Recentes (187)..." -ForegroundColor Yellow
                    Send-Key $stream 187
                }
                'N' {
                    Write-Host "[INPUT] Clicando no Painel de Notificacoes em (1850, 1050)..." -ForegroundColor Yellow
                    Send-Click $stream 1850 1050
                }
                'H' {
                    Write-Host "[INPUT] Enviando KEYCODE_HOME (3)..." -ForegroundColor Yellow
                    Send-Key $stream 3
                }
                'B' {
                    Write-Host "[INPUT] Enviando KEYCODE_BACK (4)..." -ForegroundColor Yellow
                    Send-Key $stream 4
                }
                'A' {
                    Write-Host "[TESTE AUTO] Executando rotina completa de input..." -ForegroundColor Green
                    Write-Host " -> 1. Abrindo Menu Iniciar do DeX via Clique em (35, 1050)..."
                    Send-Click $stream 35 1050
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 2. Fechando com Voltar (Key 4)..."
                    Send-Key $stream 4
                    Start-Sleep -Milliseconds 600
                    Write-Host " -> 3. Abrindo Menu Iniciar via Tecla Windows (Key 117)..."
                    Send-Key $stream 117
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 4. Abrindo Recentes (Key 187)..."
                    Send-Key $stream 187
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 5. Voltando para Home (Key 3)..."
                    Send-Key $stream 3
                    Write-Host "[TESTE AUTO] Teste concluido!" -ForegroundColor Green
                }
                'Q' {
                    Write-Host "`nEncerrando teste..." -ForegroundColor Gray
                    $keepRunning = $false
                }
            }
        }
        Start-Sleep -Milliseconds 50
    }

} catch {
    Write-Host "[ERRO] Excecao durante execucao do canal de controle: $_" -ForegroundColor Red
} finally {
    if ($stream) { $stream.Close() }
    if ($tcpClient) { $tcpClient.Close() }
    if ($ffplayProc -and -not $ffplayProc.HasExited) {
        Stop-Process -Id $ffplayProc.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Finalizando sessao DeX no Galaxy..." -ForegroundColor Gray
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
if ($serverProc -and -not $serverProc.HasExited) {
    Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
}
Write-Host "Sessao DeX encerrada com sucesso." -ForegroundColor Green
