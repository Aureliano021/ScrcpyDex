# ScrcpyDeX - Samsung DeX for PC via USB
# Activates native Samsung DeX engine on device and launches in a single native window

$ErrorActionPreference = "Continue"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "         ScrcpyDeX - Samsung DeX for PC          " -ForegroundColor Cyan
Write-Host "   Single Native Window via USB (Zero Wi-Fi)     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Check for Galaxy device connected via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "ERROR: No Samsung device detected via ADB!" -ForegroundColor Red
    Write-Host "Connect the Galaxy S23 via USB cable with USB Debugging enabled." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/4] Galaxy device detected via ADB." -ForegroundColor Green

# 2. Locate binaries
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"
$scrcpyExe = "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\scrcpy.exe"

if (-not (Test-Path $scrcpyExe)) {
    $cmd = Get-Command scrcpy -ErrorAction SilentlyContinue
    if ($cmd) { $scrcpyExe = $cmd.Source }
    else {
        Write-Host "ERROR: scrcpy binary not found." -ForegroundColor Red
        exit 1
    }
}

# 3. Clean up previous processes on device
Write-Host "[2/4] Preparing device..." -ForegroundColor Green
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
adb shell "pkill -f scrcpy" 2>$null | Out-Null
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 4. Start DeX activator in background on device
Write-Host "[3/4] Activating native Samsung DeX engine via Miracast loopback..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server activate" -PassThru

# Wait for specific 'ScrcpyDeX' display to appear
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
    Write-Host "ERROR: Samsung DeX display was not detected in time." -ForegroundColor Red
    adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    exit 1
}

Write-Host "=================================================" -ForegroundColor Green
Write-Host "  Samsung DeX Activated! Display ID: $dexId      " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "[4/4] Opening single Samsung DeX window on PC..." -ForegroundColor Cyan
Write-Host ""
Write-Host ">>> PHYSICAL MOUSE CONTROLS (UHID) <<<" -ForegroundColor Yellow
Write-Host "- To release mouse back to Windows: Press [LEFT ALT]" -ForegroundColor Yellow
Write-Host "- To toggle Fullscreen:             Press [ALT + F]" -ForegroundColor Yellow
Write-Host "- To open context menus:            Right Click" -ForegroundColor Yellow
Write-Host ""

# 5. Open single native DeX window with scrcpy in UHID mode (Real Physical Mouse)
try {
    & $scrcpyExe --display-id=$dexId --mouse=uhid --stay-awake --window-title="Samsung DeX (ScrcpyDeX)"
}
finally {
    # 6. Guaranteed 100% automatic shutdown upon closing the window
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Gray
    Write-Host "Terminating DeX session on Galaxy S23..." -ForegroundColor Gray
    adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    adb shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    adb shell "pkill -f scrcpy" 2>$null | Out-Null
    if ($serverProc -and -not $serverProc.HasExited) {
        Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "DeX session disconnected successfully! Device restored." -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Gray
}
