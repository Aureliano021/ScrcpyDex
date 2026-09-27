# Hard Gate 2 Validation Script: Control Channel & Input Injection
# Validates TCP 27184 connection, handshake reception, and mouse/keyboard injection into DeX

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "     Validation Test: HARD GATE 2                " -ForegroundColor Cyan
Write-Host "  ScrcpyDeX Control Channel & Input Injection    " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Check for Samsung device connected via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "[ERROR] No Samsung device detected via ADB!" -ForegroundColor Red
    Write-Host "Connect the Galaxy S23 via USB cable with USB Debugging enabled." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/5] Galaxy device detected via ADB." -ForegroundColor Green

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$serverJar = Join-Path $rootDir "server\scrcpydex-server.jar"
if (-not (Test-Path $serverJar)) {
    $serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"
}

if (-not (Test-Path $serverJar)) {
    Write-Host "[ERROR] Binary $serverJar not found. Run build-server.ps1 first." -ForegroundColor Red
    exit 1
}

# 2. Clean up previous processes and configure Port Forwarding
Write-Host "[2/5] Cleaning up previous processes and configuring ports..." -ForegroundColor Green
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
adb forward tcp:27183 tcp:27183
adb forward tcp:27184 tcp:27184

# 3. Push updated JAR to device
Write-Host "[3/5] Pushing scrcpydex-server.jar to /data/local/tmp/..." -ForegroundColor Green
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 4. Launch ScrcpyDeX Server on device
Write-Host "[4/5] Launching ScrcpyDeX Server on device..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server" -PassThru

Write-Host "Server launched (PID: $($serverProc.Id)). Waiting for DeX initialization (4s)..." -ForegroundColor Gray
Start-Sleep -Seconds 4

# 5. Connect to Control Channel (TCP 27184)
Write-Host "[5/5] Connecting to Control Channel (127.0.0.1:27184)..." -ForegroundColor Cyan
$tcpClient = $null
$stream = $null

try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.NoDelay = $true
    $tcpClient.Connect("127.0.0.1", 27184)
    $stream = $tcpClient.GetStream()
    Write-Host "Control connection established!" -ForegroundColor Green

    # Helper function to read Big-Endian 32-bit integers
    function Read-Int32BE($s) {
        $b = New-Object byte[] 4
        $read = $s.Read($b, 0, 4)
        if ($read -lt 4) { throw "Unexpected end of control stream" }
        if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
        return [BitConverter]::ToInt32($b, 0)
    }

    # Read MSG_DEX_READY (0x01)
    $msgType = $stream.ReadByte()
    if ($msgType -eq 0x01) {
        $displayId = Read-Int32BE $stream
        Write-Host ">>> RECEIVED MSG_DEX_READY: DisplayId = $displayId <<<" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Expected 0x01, received 0x$([Convert]::ToString($msgType, 16))" -ForegroundColor Yellow
    }

    # Read MSG_DISPLAY_INFO (0x02)
    $msgType2 = $stream.ReadByte()
    if ($msgType2 -eq 0x02) {
        $width = Read-Int32BE $stream
        $height = Read-Int32BE $stream
        $dpi = Read-Int32BE $stream
        Write-Host ">>> RECEIVED MSG_DISPLAY_INFO: ${width}x${height} @ ${dpi} DPI <<<" -ForegroundColor Green
    }

    # Launch ffplay in separate process with 1280x720 window for visual output
    $ffplayCmd = Get-Command ffplay -ErrorAction SilentlyContinue
    $ffplayProc = $null
    if ($ffplayCmd) {
        Write-Host "`nLaunching video window (1280x720, 60fps, nobuffer)..." -ForegroundColor Cyan
        $ffplayProc = Start-Process -FilePath "ffplay" -ArgumentList "-f h264 -framerate 60 -fflags nobuffer -flags low_delay -probesize 32768 -analyzeduration 100000 -window_title `"ScrcpyDeX - Hard Gate 2 (Video + Input)`" -x 1280 -y 720 tcp://127.0.0.1:27183" -PassThru
        Start-Sleep -Seconds 2
    }

    Write-Host "`n-------------------------------------------------" -ForegroundColor Gray
    Write-Host "Interactive Test Controls (Port 27184):" -ForegroundColor White
    Write-Host " [W] Windows Key / DeX Menu (Key 117 - KEYCODE_META_LEFT)" -ForegroundColor Green
    Write-Host " [S] Click DeX Apps/Start Button (x=35, y=1050)" -ForegroundColor Green
    Write-Host " [R] App Switcher / Recents (Key 187 - KEYCODE_APP_SWITCH)" -ForegroundColor Yellow
    Write-Host " [N] Click Notifications Panel (x=1850, y=1050)" -ForegroundColor Yellow
    Write-Host " [H] Press Home (Key 3 - KEYCODE_HOME)" -ForegroundColor Yellow
    Write-Host " [B] Press Back (Key 4 - KEYCODE_BACK)" -ForegroundColor Yellow
    Write-Host " [A] Complete Automated Test (Menu + Apps + Back)" -ForegroundColor Cyan
    Write-Host " [Q] Quit and Terminate DeX" -ForegroundColor Red
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
                    Write-Host "[INPUT] Sending KEYCODE_META_LEFT / Windows Key (117)..." -ForegroundColor Green
                    Send-Key $stream 117
                }
                'S' {
                    Write-Host "[INPUT] Clicking DeX Start Menu at (35, 1050)..." -ForegroundColor Green
                    Send-Click $stream 35 1050
                }
                'R' {
                    Write-Host "[INPUT] Sending KEYCODE_APP_SWITCH / Recents (187)..." -ForegroundColor Yellow
                    Send-Key $stream 187
                }
                'N' {
                    Write-Host "[INPUT] Clicking Notifications Panel at (1850, 1050)..." -ForegroundColor Yellow
                    Send-Click $stream 1850 1050
                }
                'H' {
                    Write-Host "[INPUT] Sending KEYCODE_HOME (3)..." -ForegroundColor Yellow
                    Send-Key $stream 3
                }
                'B' {
                    Write-Host "[INPUT] Sending KEYCODE_BACK (4)..." -ForegroundColor Yellow
                    Send-Key $stream 4
                }
                'A' {
                    Write-Host "[AUTO TEST] Running full input sequence..." -ForegroundColor Green
                    Write-Host " -> 1. Opening DeX Start Menu via click at (35, 1050)..."
                    Send-Click $stream 35 1050
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 2. Closing with Back (Key 4)..."
                    Send-Key $stream 4
                    Start-Sleep -Milliseconds 600
                    Write-Host " -> 3. Opening Start Menu via Windows Key (Key 117)..."
                    Send-Key $stream 117
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 4. Opening Recents (Key 187)..."
                    Send-Key $stream 187
                    Start-Sleep -Milliseconds 1200
                    Write-Host " -> 5. Returning to Home (Key 3)..."
                    Send-Key $stream 3
                    Write-Host "[AUTO TEST] Test completed!" -ForegroundColor Green
                }
                'Q' {
                    Write-Host "`nTerminating test..." -ForegroundColor Gray
                    $keepRunning = $false
                }
            }
        }
        Start-Sleep -Milliseconds 50
    }

} catch {
    Write-Host "[ERROR] Exception during control channel execution: $_" -ForegroundColor Red
} finally {
    if ($stream) { $stream.Close() }
    if ($tcpClient) { $tcpClient.Close() }
    if ($ffplayProc -and -not $ffplayProc.HasExited) {
        Stop-Process -Id $ffplayProc.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Terminating DeX session on Galaxy..." -ForegroundColor Gray
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
if ($serverProc -and -not $serverProc.HasExited) {
    Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
}
Write-Host "DeX session terminated." -ForegroundColor Green
