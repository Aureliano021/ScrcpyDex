# ScrcpyDeX - Open-Source Client Compatible with Samsung DeX via USB
# Activates device desktop display mode via loopback and launches single window client

param(
    [int]$Bitrate = 8000000,
    [int]$Fps = 60,
    [string]$Resolution = "1920x1080",
    [string]$Codec = "h264",
    [string]$MouseDriver = "uhid",
    [bool]$TurnScreenOff = $true,
    [bool]$AudioEnabled = $true,
    [string]$WindowMode = "normal",
    [string]$ConfigFile = ""
)

$ErrorActionPreference = "Continue"

# Load parameters from configuration file if specified
if ($ConfigFile -and (Test-Path $ConfigFile)) {
    try {
        $json = Get-Content $ConfigFile -Raw | ConvertFrom-Json
        if ($json.display.bitrate) { $Bitrate = [int]$json.display.bitrate }
        if ($json.display.fps) { $Fps = [int]$json.display.fps }
        if ($json.display.codec) { $Codec = [string]$json.display.codec }
        if ($json.display.resolution) { $Resolution = [string]$json.display.resolution }
        if ($json.display.windowMode) { $WindowMode = [string]$json.display.windowMode }
        if ($json.input.mouseDriver) { $MouseDriver = [string]$json.input.mouseDriver }
        if ($null -ne $json.device.turnScreenOff) { $TurnScreenOff = [bool]$json.device.turnScreenOff }
        if ($null -ne $json.audio.enabled) { $AudioEnabled = [bool]$json.audio.enabled }
    } catch {
        Write-Host "Warning: Could not parse config file, using default parameters: $_" -ForegroundColor Yellow
    }
}

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   ScrcpyDeX — Samsung DeX Compatible Client    " -ForegroundColor Cyan
Write-Host "   Single Native Window via USB (Zero Wi-Fi)     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Locate ADB executable and check for Galaxy device
$adbCmd = Get-Command adb -ErrorAction SilentlyContinue
if ($adbCmd) { $adbExe = $adbCmd.Source }
else {
    $adbCandidates = @(
        "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe",
        "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe",
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
        "$env:ProgramFiles\scrcpy\adb.exe",
        "$env:LOCALAPPDATA\Microsoft\WinGet\Links\adb.exe"
    )
    $adbExe = $adbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $adbExe) { $adbExe = "adb" }
}

$device = & "$adbExe" devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "ERROR: No Samsung device detected via ADB!" -ForegroundColor Red
    Write-Host "Connect the Galaxy S23 via USB cable with USB Debugging enabled." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/4] Galaxy device detected via ADB." -ForegroundColor Green

# 2. Locate binaries
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJar = Join-Path $scriptDir "server\scrcpydex-server.jar"

# Look for scrcpy on PATH first, then fallback to common installation locations
$scrcpyCmd = Get-Command scrcpy -ErrorAction SilentlyContinue
if ($scrcpyCmd) {
    $scrcpyExe = $scrcpyCmd.Source
} else {
    $commonPaths = @(
        "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\scrcpy.exe",
        "$env:LOCALAPPDATA\Microsoft\WinGet\Links\scrcpy.exe",
        "$env:ProgramFiles\scrcpy\scrcpy.exe"
    )
    $scrcpyExe = $commonPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $scrcpyExe -or -not (Test-Path $scrcpyExe)) {
    Write-Host "ERROR: scrcpy binary not found on PATH or standard locations." -ForegroundColor Red
    Write-Host "Please install scrcpy (e.g. winget install Genymobile.scrcpy) or add it to PATH." -ForegroundColor Yellow
    exit 1
}

# 3. Clean up previous processes on device
Write-Host "[2/4] Preparing device..." -ForegroundColor Green
& "$adbExe" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
& "$adbExe" shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
& "$adbExe" shell "pkill -f scrcpy" 2>$null | Out-Null
& "$adbExe" push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

# 4. Start DeX activator in background on device with output streaming
Write-Host "[3/4] Activating Samsung DeX display via Miracast loopback..." -ForegroundColor Cyan
$serverLog = Join-Path $env:TEMP "scrcpydex_server.log"
$serverErrLog = Join-Path $env:TEMP "scrcpydex_server_err.log"
if (Test-Path $serverLog) { Remove-Item $serverLog -Force -ErrorAction SilentlyContinue }
if (Test-Path $serverErrLog) { Remove-Item $serverErrLog -Force -ErrorAction SilentlyContinue }

$serverProc = Start-Process -FilePath $adbExe -ArgumentList @("shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server activate") -RedirectStandardOutput $serverLog -RedirectStandardError $serverErrLog -PassThru -WindowStyle Hidden

# Wait for specific 'ScrcpyDeX' display to appear and stream server logs
$dexId = $null
$retries = 0
$lastReadPos = 0
while ($retries -lt 30 -and -not $dexId) {
    Start-Sleep -Milliseconds 350
    if (Test-Path $serverLog) {
        $lines = @(Get-Content $serverLog -ErrorAction SilentlyContinue)
        for ($i = $lastReadPos; $i -lt $lines.Count; $i++) {
            $ln = $lines[$i].Trim()
            if ($ln) {
                Write-Host "[Server] $ln" -ForegroundColor Cyan
                if ($ln -match 'DEX_ACTIVATED_DISPLAY_ID=(\d+)' -or $ln -match 'ready for streaming! ID=(\d+)') {
                    $dexId = $Matches[1]
                }
            }
        }
        $lastReadPos = $lines.Count
    }

    if (-not $dexId) {
        $dumpStr = (& "$adbExe" shell "dumpsys display") -join "`n"
        if ($dumpStr -match 'DisplayInfo\{"ScrcpyDeX", displayId (\d+)') {
            $dexId = $Matches[1]
            break
        } elseif ($dumpStr -match 'Display (\d+):[\s\S]*?mPrimaryDisplayDevice=ScrcpyDeX') {
            $dexId = $Matches[1]
            break
        }
    } else {
        break
    }
    $retries++
}

if (-not $dexId) {
    Write-Host "ERROR: Samsung DeX display was not detected in time." -ForegroundColor Red
    if (Test-Path $serverLog) {
        Write-Host "--- DeX Server Log Diagnostics ---" -ForegroundColor Red
        Get-Content $serverLog -Tail 15 | ForEach-Object { Write-Host "[Server Log] $_" -ForegroundColor Red }
    }
    if (Test-Path $serverErrLog) {
        Write-Host "--- DeX Server Error Diagnostics ---" -ForegroundColor Red
        Get-Content $serverErrLog -Tail 15 | ForEach-Object { Write-Host "[Server Error] $_" -ForegroundColor Red }
    }
    & "$adbExe" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    & "$adbExe" shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    exit 1
}

Write-Host "=================================================" -ForegroundColor Green
Write-Host "  Samsung DeX Activated! Display ID: $dexId      " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "[4/4] Opening native Samsung DeX display window..." -ForegroundColor Cyan
Write-Host ""
Write-Host "---------------- CONTROL CHEAT SHEET ----------------" -ForegroundColor Yellow
Write-Host "  • [Left Alt]    Release mouse cursor back to Windows" -ForegroundColor Yellow
Write-Host "  • [Alt + F]     Toggle Fullscreen borderless mode" -ForegroundColor Yellow
Write-Host "  • [Right-Click] Open native Samsung DeX context menus" -ForegroundColor Yellow
Write-Host "  • [Closing Win] Automatically terminates DeX & saves phone battery" -ForegroundColor Yellow
Write-Host "-----------------------------------------------------" -ForegroundColor Yellow
Write-Host ""

# 5. Open single DeX window with scrcpy in UHID mode (Physical Mouse) with custom settings
try {
    $scrcpyArgs = @(
        "--display-id=$dexId",
        "--mouse=$MouseDriver",
        "--video-bit-rate=$Bitrate",
        "--max-fps=$Fps",
        "--video-codec=$Codec",
        "--stay-awake",
        "--window-title=ScrcpyDeX - Samsung DeX Compatible Client"
    )
    if ($TurnScreenOff) { $scrcpyArgs += "--turn-screen-off" }
    if (-not $AudioEnabled) { $scrcpyArgs += "--no-audio" }
    if ($WindowMode -eq "fullscreen") { $scrcpyArgs += "--fullscreen" }
    elseif ($WindowMode -eq "borderless") { $scrcpyArgs += "--window-borderless" }

    Write-Host "Launching scrcpy with options: $($scrcpyArgs -join ' ')" -ForegroundColor DarkGray
    & $scrcpyExe @scrcpyArgs
}
finally {
    # 6. Automatic session shutdown upon closing the window
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Gray
    Write-Host "Terminating DeX session on Galaxy S23..." -ForegroundColor Gray
    & "$adbExe" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
    & "$adbExe" shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
    & "$adbExe" shell "pkill -f scrcpy" 2>$null | Out-Null
    if ($serverProc -and -not $serverProc.HasExited) {
        Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "DeX session disconnected successfully! Device restored." -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Gray
}
