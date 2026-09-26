# ScrcpyDeX - Standalone Desktop Client for Samsung DeX via USB
# Custom window, H.264 @ 60 FPS streaming, and native Mouse and Keyboard capture

param(
    [int]$WindowWidth = 1280,
    [int]$WindowHeight = 720
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "         ScrcpyDeX - Samsung DeX for PC          " -ForegroundColor Cyan
Write-Host "      Standalone Client via USB (Zero Wi-Fi)     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Load Graphics and Win32 Assemblies
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;

public class Win32 {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public const int GWL_STYLE = -16;
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_CHILD = 0x40000000;
    public const int WS_BORDER = 0x00800000;
    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_CAPTION = 0x00C00000;
}
"@

# 2. Locate required binaries (ADB, FFplay, Server JAR)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$serverJar = Join-Path $rootDir "server\scrcpydex-server.jar"

if (-not (Test-Path $serverJar)) {
    Write-Host "[ERROR] Binary $serverJar not found. Run build-server.ps1 first." -ForegroundColor Red
    exit 1
}

$ffplayPath = "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0.2-full_build\bin\ffplay.exe"
if (-not (Test-Path $ffplayPath)) {
    $ffplayCmd = Get-Command ffplay -ErrorAction SilentlyContinue
    if ($ffplayCmd) { $ffplayPath = $ffplayCmd.Source }
    else {
        Write-Host "[ERROR] The ffplay player was not found." -ForegroundColor Red
        exit 1
    }
}

# 3. Detect connected Samsung device
$device = adb devices | Select-String -Pattern "\bdevice\b" | Where-Object { $_ -notmatch "List of devices" }
if (-not $device) {
    Write-Host "[ERROR] No Samsung device detected via ADB." -ForegroundColor Red
    Write-Host "Connect the Galaxy S23 via USB cable and enable USB Debugging." -ForegroundColor Yellow
    exit 1
}
Write-Host "[1/5] Galaxy device detected via ADB." -ForegroundColor Green

# 4. Configure Port Forwarding and clean up old sessions
Write-Host "[2/5] Cleaning up previous processes and configuring USB tunnels..." -ForegroundColor Green
adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
adb forward tcp:27183 tcp:27183
adb forward tcp:27184 tcp:27184

# 5. Push JAR and start Server
Write-Host "[3/5] Pushing server and activating native Samsung DeX engine..." -ForegroundColor Green
adb push $serverJar /data/local/tmp/scrcpydex-server.jar | Out-Null

$serverProc = Start-Process -FilePath "adb" -ArgumentList "shell", "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server" -PassThru
Start-Sleep -Seconds 4

# 6. Connect to Control Channel (TCP 27184)
Write-Host "[4/5] Connecting to control channel (127.0.0.1:27184)..." -ForegroundColor Cyan
$tcpClient = New-Object System.Net.Sockets.TcpClient
$tcpClient.NoDelay = $true
$tcpClient.Connect("127.0.0.1", 27184)
$stream = $tcpClient.GetStream()

function Read-Int32BE($s) {
    $b = New-Object byte[] 4
    $read = $s.Read($b, 0, 4)
    if ($read -lt 4) { throw "End of stream" }
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
    return [BitConverter]::ToInt32($b, 0)
}

# Read MSG_DEX_READY (0x01)
$msgType = $stream.ReadByte()
$displayId = Read-Int32BE $stream

# Read MSG_DISPLAY_INFO (0x02)
$msgType2 = $stream.ReadByte()
$dexWidth = Read-Int32BE $stream
$dexHeight = Read-Int32BE $stream
$dexDpi = Read-Int32BE $stream
Write-Host ">>> Samsung DeX Connected: ${dexWidth}x${dexHeight} @ ${dexDpi} DPI (Display ID: $displayId) <<<" -ForegroundColor Green

# Input sending helper functions for protocol
function Send-MouseMove($x, $y) {
    if (-not $stream.CanWrite) { return }
    $buf = New-Object byte[] 9
    $buf[0] = 0x10 # MSG_MOUSE_MOVE
    $xb = [BitConverter]::GetBytes([int]$x)
    $yb = [BitConverter]::GetBytes([int]$y)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($xb); [Array]::Reverse($yb) }
    [Array]::Copy($xb, 0, $buf, 1, 4)
    [Array]::Copy($yb, 0, $buf, 5, 4)
    $stream.Write($buf, 0, 9)
    $stream.Flush()
}

function Send-MouseButton($x, $y, $button, $action) {
    if (-not $stream.CanWrite) { return }
    $buf = New-Object byte[] 11
    $buf[0] = 0x11 # MSG_MOUSE_BUTTON
    $xb = [BitConverter]::GetBytes([int]$x)
    $yb = [BitConverter]::GetBytes([int]$y)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($xb); [Array]::Reverse($yb) }
    [Array]::Copy($xb, 0, $buf, 1, 4)
    [Array]::Copy($yb, 0, $buf, 5, 4)
    $buf[9] = [byte]$button
    $buf[10] = [byte]$action
    $stream.Write($buf, 0, 11)
    $stream.Flush()
}

function Send-Scroll($x, $y, $hScroll, $vScroll) {
    if (-not $stream.CanWrite) { return }
    $buf = New-Object byte[] 17
    $buf[0] = 0x13 # MSG_SCROLL
    $xb = [BitConverter]::GetBytes([int]$x)
    $yb = [BitConverter]::GetBytes([int]$y)
    $hb = [BitConverter]::GetBytes([float]$hScroll)
    $vb = [BitConverter]::GetBytes([float]$vScroll)
    if ([BitConverter]::IsLittleEndian) {
        [Array]::Reverse($xb); [Array]::Reverse($yb)
        [Array]::Reverse($hb); [Array]::Reverse($vb)
    }
    [Array]::Copy($xb, 0, $buf, 1, 4)
    [Array]::Copy($yb, 0, $buf, 5, 4)
    [Array]::Copy($hb, 0, $buf, 9, 4)
    [Array]::Copy($vb, 0, $buf, 13, 4)
    $stream.Write($buf, 0, 17)
    $stream.Flush()
}

function Send-Key($keyCode, $action) {
    if (-not $stream.CanWrite) { return }
    $buf = New-Object byte[] 6
    $buf[0] = 0x12 # MSG_KEY_EVENT
    $kb = [BitConverter]::GetBytes([int]$keyCode)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($kb) }
    [Array]::Copy($kb, 0, $buf, 1, 4)
    $buf[5] = [byte]$action
    $stream.Write($buf, 0, 6)
    $stream.Flush()
}

# Windows Key (System.Windows.Forms.Keys) -> Android Keycode Mapper
function Map-WinKeyToAndroid($key) {
    switch ($key) {
        ([System.Windows.Forms.Keys]::LWin) { return 117 } # KEYCODE_META_LEFT (Start Menu)
        ([System.Windows.Forms.Keys]::RWin) { return 118 }
        ([System.Windows.Forms.Keys]::Escape) { return 4 } # KEYCODE_BACK
        ([System.Windows.Forms.Keys]::Return) { return 66 } # KEYCODE_ENTER
        ([System.Windows.Forms.Keys]::Back) { return 67 } # KEYCODE_DEL
        ([System.Windows.Forms.Keys]::Tab) { return 61 } # KEYCODE_TAB
        ([System.Windows.Forms.Keys]::Space) { return 62 } # KEYCODE_SPACE
        ([System.Windows.Forms.Keys]::Delete) { return 112 } # KEYCODE_FORWARD_DEL
        ([System.Windows.Forms.Keys]::Up) { return 19 } # KEYCODE_DPAD_UP
        ([System.Windows.Forms.Keys]::Down) { return 20 } # KEYCODE_DPAD_DOWN
        ([System.Windows.Forms.Keys]::Left) { return 21 } # KEYCODE_DPAD_LEFT
        ([System.Windows.Forms.Keys]::Right) { return 22 } # KEYCODE_DPAD_RIGHT
        ([System.Windows.Forms.Keys]::Home) { return 122 }
        ([System.Windows.Forms.Keys]::End) { return 123 }
        ([System.Windows.Forms.Keys]::PageUp) { return 92 }
        ([System.Windows.Forms.Keys]::PageDown) { return 93 }
        ([System.Windows.Forms.Keys]::VolumeUp) { return 24 }
        ([System.Windows.Forms.Keys]::VolumeDown) { return 25 }
        ([System.Windows.Forms.Keys]::VolumeMute) { return 164 }
        default {
            # Letters A-Z (Android KEYCODE_A = 29)
            $intVal = [int]$key
            if ($intVal -ge 65 -and $intVal -le 90) {
                return (29 + ($intVal - 65))
            }
            # Numbers 0-9 (Android KEYCODE_0 = 7)
            if ($intVal -ge 48 -and $intVal -le 57) {
                return (7 + ($intVal - 48))
            }
            return 0
        }
    }
}

# 7. Create ScrcpyDeX Client Window (Windows Forms)
Write-Host "[5/5] Building GUI and custom window..." -ForegroundColor Green

$form = New-Object System.Windows.Forms.Form
$form.Text = "ScrcpyDeX - Samsung DeX (Galaxy S23 SM-S911B)"
$form.ClientSize = New-Object System.Drawing.Size($WindowWidth, $WindowHeight)
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
$form.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.KeyPreview = $true

# Video Panel
$videoPanel = New-Object System.Windows.Forms.Panel
$videoPanel.Dock = [System.Windows.Forms.DockStyle]::Fill
$videoPanel.BackColor = [System.Drawing.Color]::Black
$form.Controls.Add($videoPanel)

$isFullScreen = $false
$previousBounds = $null
$previousBorderStyle = $null

function Toggle-Fullscreen {
    $script:isFullScreen = -not $script:isFullScreen
    if ($script:isFullScreen) {
        $script:previousBounds = $form.Bounds
        $script:previousBorderStyle = $form.FormBorderStyle
        $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
        $form.WindowState = [System.Windows.Forms.FormWindowState]::Maximized
    } else {
        $form.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $form.FormBorderStyle = $script:previousBorderStyle
        $form.Bounds = $script:previousBounds
    }
}

# Launch ffplay with identifiable window title
$videoTitle = "ScrcpyDeX_VideoSurface_" + [System.Guid]::NewGuid().ToString().Substring(0, 8)
$ffplayArgs = "-f h264 -framerate 60 -fflags nobuffer -flags low_delay -probesize 32768 -analyzeduration 100000 -window_title `"$videoTitle`" -noborder -x $WindowWidth -y $WindowHeight tcp://127.0.0.1:27183"

$ffplayProc = Start-Process -FilePath $ffplayPath -ArgumentList $ffplayArgs -PassThru

# Embed ffplay window into form
$embedTimer = New-Object System.Windows.Forms.Timer
$embedTimer.Interval = 200
$embedRetries = 0

$embedTimer.Add_Tick({
    $script:embedRetries++
    $hwnd = [Win32]::FindWindow($null, $videoTitle)
    if ($hwnd -ne [IntPtr]::Zero) {
        $embedTimer.Stop()
        [Win32]::SetParent($hwnd, $videoPanel.Handle) | Out-Null
        
        # Set as child and disable to pass mouse clicks to parent panel
        $style = [Win32]::GetWindowLong($hwnd, [Win32]::GWL_STYLE)
        $style = ($style -band -bnot [Win32]::WS_CAPTION) -bor [Win32]::WS_CHILD
        [Win32]::SetWindowLong($hwnd, [Win32]::GWL_STYLE, $style) | Out-Null
        
        [Win32]::MoveWindow($hwnd, 0, 0, $videoPanel.Width, $videoPanel.Height, $true) | Out-Null
        [Win32]::EnableWindow($hwnd, $false) | Out-Null
        Write-Host "H.264 video window embedded successfully in ScrcpyDeX!" -ForegroundColor Green
    } elseif ($script:embedRetries -gt 30) {
        $embedTimer.Stop()
        Write-Host "[WARNING] Video window continues in separate process." -ForegroundColor Yellow
    }
})

$videoPanel.Add_Resize({
    $hwnd = [Win32]::FindWindow($null, $videoTitle)
    if ($hwnd -ne [IntPtr]::Zero) {
        [Win32]::MoveWindow($hwnd, 0, 0, $videoPanel.Width, $videoPanel.Height, $true) | Out-Null
    }
})

# Mouse Coordinate Conversion (PC Window -> DeX Display)
function Get-DexCoordinates($mouseX, $mouseY) {
    $w = [Math]::Max(1, $videoPanel.Width)
    $h = [Math]::Max(1, $videoPanel.Height)
    $x = [int][Math]::Round(($mouseX / $w) * $dexWidth)
    $y = [int][Math]::Round(($mouseY / $h) * $dexHeight)
    $x = [Math]::Max(0, [Math]::Min($dexWidth - 1, $x))
    $y = [Math]::Max(0, [Math]::Min($dexHeight - 1, $y))
    return @($x, $y)
}

# Panel Input Events
$videoPanel.Add_MouseMove({
    param($s, $e)
    $coords = Get-DexCoordinates $e.X $e.Y
    Send-MouseMove $coords[0] $coords[1]
})

$videoPanel.Add_MouseDown({
    param($s, $e)
    $coords = Get-DexCoordinates $e.X $e.Y
    $btn = 1
    if ($e.Button -eq [System.Windows.Forms.MouseButtons]::Right) { $btn = 2 }
    elseif ($e.Button -eq [System.Windows.Forms.MouseButtons]::Middle) { $btn = 4 }
    Send-MouseButton $coords[0] $coords[1] $btn 0 # DOWN
})

$videoPanel.Add_MouseUp({
    param($s, $e)
    $coords = Get-DexCoordinates $e.X $e.Y
    $btn = 1
    if ($e.Button -eq [System.Windows.Forms.MouseButtons]::Right) { $btn = 2 }
    elseif ($e.Button -eq [System.Windows.Forms.MouseButtons]::Middle) { $btn = 4 }
    Send-MouseButton $coords[0] $coords[1] $btn 1 # UP
})

$videoPanel.Add_MouseWheel({
    param($s, $e)
    $coords = Get-DexCoordinates $e.X $e.Y
    $delta = if ($e.Delta -gt 0) { 1.0 } else { -1.0 }
    Send-Scroll $coords[0] $coords[1] 0.0 $delta
})

# Keyboard Events
$form.Add_KeyDown({
    param($s, $e)
    if ($e.KeyCode -eq [System.Windows.Forms.Keys]::F11 -or ($e.Alt -and $e.KeyCode -eq [System.Windows.Forms.Keys]::Enter)) {
        Toggle-Fullscreen
        $e.Handled = $true
        return
    }
    $androidKey = Map-WinKeyToAndroid $e.KeyCode
    if ($androidKey -ne 0) {
        Send-Key $androidKey 0 # DOWN
        $e.Handled = $true
    }
})

$form.Add_KeyUp({
    param($s, $e)
    $androidKey = Map-WinKeyToAndroid $e.KeyCode
    if ($androidKey -ne 0) {
        Send-Key $androidKey 1 # UP
        $e.Handled = $true
    }
})

# Cleanup and Safe Shutdown
$form.Add_FormClosing({
    param($s, $e)
    Write-Host "`nClosing ScrcpyDeX..." -ForegroundColor Gray
    try {
        if ($stream.CanWrite) {
            $stream.WriteByte(0xFE) # MSG_DISCONNECT
            $stream.Flush()
        }
    } catch {}
    
    if ($stream) { $stream.Close() }
    if ($tcpClient) { $tcpClient.Close() }
    
    if ($ffplayProc -and -not $ffplayProc.HasExited) {
        Stop-Process -Id $ffplayProc.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Disconnecting DeX engine on device..." -ForegroundColor Gray
    adb shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null
    adb shell "pkill -f com.scrcpydex.server.Server" 2>$null
    if ($serverProc -and -not $serverProc.HasExited) {
        Stop-Process -Id $serverProc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "DeX session terminated successfully." -ForegroundColor Green
})

# Start GUI Loop
$embedTimer.Start()
[System.Windows.Forms.Application]::Run($form)
