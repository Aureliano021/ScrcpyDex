# Hard Gate 1 Test Script (ScrcpyDeX Server)
# Validates Samsung DeX activation and H.264 streaming via USB

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "     Validation Test: HARD GATE 1                " -ForegroundColor Cyan
Write-Host "     ScrcpyDeX Server (H.264 Stream via USB)     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Check for Galaxy device connected via ADB
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "[ERROR] No Samsung device detected via ADB!" -ForegroundColor Red
    Write-Host "Connect the Galaxy S23 via USB cable and ensure USB Debugging is enabled." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/4] Galaxy device detected via ADB." -ForegroundColor Green

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

# 2. Check for ffplay presence
$ffplayCmd = Get-Command ffplay -ErrorAction SilentlyContinue
if (-not $ffplayCmd) {
    Write-Host "[WARNING] The 'ffplay' player (FFmpeg) was not found in PATH." -ForegroundColor Yellow
    Write-Host "You can install it quickly by running in terminal:" -ForegroundColor Yellow
    Write-Host "  winget install Gyan.FFmpeg" -ForegroundColor Cyan
    Write-Host "After installing, restart your terminal." -ForegroundColor Yellow
    Write-Host "`nAttempting to continue if you only want to start the server..." -ForegroundColor Gray
}

# 3. Clean up previous processes
Write-Host "[2/4] Cleaning up previous processes on device..." -ForegroundColor Green
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
adb forward tcp:27183 tcp:27183

# 4. Push JAR to device
Write-Host "[3/4] Pushing scrcpydex-server.jar to /data/local/tmp/..." -ForegroundColor Green
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 5. Launch server on device in background and open ffplay
Write-Host "[4/4] Launching ScrcpyDeX Server on device..." -ForegroundColor Cyan
$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server" -PassThru

Write-Host "Server launched (PID: $($serverProc.Id)). Waiting for DeX initialization (4s)..." -ForegroundColor Gray
Start-Sleep -Seconds 4

if ($ffplayCmd) {
    Write-Host "Launching ffplay with low latency (60fps, nobuffer)..." -ForegroundColor Green
    & ffplay -f h264 -framerate 60 -fflags nobuffer -flags low_delay -probesize 32768 -analyzeduration 100000 tcp://127.0.0.1:27183
} else {
    Write-Host "`nServer running and awaiting connection on port 27183!" -ForegroundColor Green
    Write-Host "To watch the stream, connect any H.264 player to tcp://127.0.0.1:27183" -ForegroundColor Cyan
}

Write-Host "`nPress any key to terminate DeX session..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Terminating session..." -ForegroundColor Gray
adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
if ($serverProc -and -not $serverProc.HasExited) {
    Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
}
Write-Host "DeX session terminated." -ForegroundColor Green
