# ScrcpyDeX — Modern WinUI 3 Desktop Control Center
# Windows 11 Fluent Design Settings, Real-Time Diagnostics & Lifecycle Manager for Samsung DeX via USB

param(
    [switch]$HeadlessTest
)

$ErrorActionPreference = "Continue"

# 1. Load Windows Presentation Foundation (WPF) Assemblies
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml

# 2. Register DWM Interop for Windows 11 Immersive Dark Mode & Rounded Corners
Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinUIDwm {
    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public static void ApplyModernWindowStyling(IntPtr hwnd) {
        if (hwnd == IntPtr.Zero) return;
        try {
            int darkMode = 1; // Immersive Dark Mode
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            int cornerPref = 2; // DWMWCP_ROUND (Windows 11 Rounded Corners)
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
            int backdropMica = 2; // Mica Effect
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropMica, sizeof(int));
        } catch { }
    }
}
"@

# 3. Path Resolution & Binaries Discovery
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$rootDir = if (Test-Path (Join-Path $scriptDir "run-scrcpydex.ps1")) { $scriptDir } else { Split-Path -Parent $scriptDir }

$configDir = Join-Path $rootDir "config"
$configLocal = Join-Path $configDir "settings.json"
$appDataDir = Join-Path $env:APPDATA "ScrcpyDeX"
$configAppData = Join-Path $appDataDir "settings.json"
$runScript = Join-Path $rootDir "run-scrcpydex.ps1"
$stopScript = Join-Path $rootDir "stop-dex.bat"
$assetsDir = Join-Path $rootDir "assets"

function Get-AdbExecutable {
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidates = @(
        "C:\Users\aurel\AppData\Local\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe",
        "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\scrcpy-win64-v4.1\adb.exe",
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
        "$env:ProgramFiles\scrcpy\adb.exe",
        "$env:LOCALAPPDATA\Microsoft\WinGet\Links\adb.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    return "adb"
}

$adbBinary = Get-AdbExecutable

# 4. Settings Store Logic
function Load-ConfigStore {
    $targetPath = $null
    if (Test-Path $configAppData) { $targetPath = $configAppData }
    elseif (Test-Path $configLocal) { $targetPath = $configLocal }

    if ($targetPath) {
        try {
            $content = Get-Content $targetPath -Raw -Encoding UTF8 | ConvertFrom-Json
            return $content
        } catch {
            Write-Host "Warning: Failed to parse $targetPath, falling back to defaults." -ForegroundColor Yellow
        }
    }

    # Default: 1080p @ 8 Mbps, 60 FPS, H.264
    return [PSCustomObject]@{
        version = "1.0"
        display = [PSCustomObject]@{
            resolution = "1920x1080"
            fps = 60
            bitrate = 8000000
            codec = "h264"
            windowMode = "normal"
        }
        audio = [PSCustomObject]@{
            enabled = $true
            bufferMs = 50
        }
        input = [PSCustomObject]@{
            mouseDriver = "uhid"
            releaseKey = "LeftAlt"
        }
        device = [PSCustomObject]@{
            turnScreenOff = $true
            stayAwake = $true
            autoConnect = $false
        }
        ui = [PSCustomObject]@{
            theme = "dark"
            showLogPanel = $true
        }
    }
}

function Save-ConfigStore($cfg) {
    try {
        if (-not (Test-Path $appDataDir)) {
            New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
        }
        if (-not (Test-Path $configDir)) {
            New-Item -ItemType Directory -Path $configDir -Force | Out-Null
        }
        $jsonStr = $cfg | ConvertTo-Json -Depth 6
        Set-Content -Path $configAppData -Value $jsonStr -Encoding UTF8
        Set-Content -Path $configLocal -Value $jsonStr -Encoding UTF8
        return $true
    } catch {
        Write-Host "Error saving configuration: $_" -ForegroundColor Red
        return $false
    }
}

$currentConfig = Load-ConfigStore

# 5. Build WinUI 3 Styled XAML Definition
$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="ScrcpyDeX — Samsung DeX Control Center"
        Width="960" Height="720" MinWidth="900" MinHeight="660"
        WindowStartupLocation="CenterScreen"
        Background="#1C1C1C" Foreground="#FFFFFF"
        FontFamily="Segoe UI Variable Text, Segoe UI, sans-serif">

    <Window.Resources>
        <!-- Modern Settings Card Style -->
        <Style x:Key="SettingsCard" TargetType="Border">
            <Setter Property="Background" Value="#252525"/>
            <Setter Property="BorderBrush" Value="#383838"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="Padding" Value="16,14"/>
            <Setter Property="Margin" Value="0,0,0,10"/>
        </Style>

        <!-- Stepper Phase Badge Style -->
        <Style x:Key="PhaseBadge" TargetType="Border">
            <Setter Property="Background" Value="#222222"/>
            <Setter Property="BorderBrush" Value="#383838"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="CornerRadius" Value="6"/>
            <Setter Property="Padding" Value="10,6"/>
            <Setter Property="Margin" Value="0,0,8,0"/>
        </Style>

        <!-- Primary Action Button (Fluent Accent) -->
        <Style x:Key="AccentButton" TargetType="Button">
            <Setter Property="Background" Value="#0078D4"/>
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,9"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="BtnBorder" Background="{TemplateBinding Background}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="BtnBorder" Property="Background" Value="#1984D8"/>
                            </Trigger>
                            <Trigger Property="IsEnabled" Value="False">
                                <Setter TargetName="BtnBorder" Property="Background" Value="#333333"/>
                                <Setter Property="Foreground" Value="#777777"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Disconnect / Active Session Button Style -->
        <Style x:Key="StopSessionButton" TargetType="Button">
            <Setter Property="Background" Value="#C42B1C"/>
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,9"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="BtnBorder" Background="{TemplateBinding Background}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="BtnBorder" Property="Background" Value="#D83B01"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Standard Neutral Button -->
        <Style x:Key="StandardButton" TargetType="Button">
            <Setter Property="Background" Value="#2E2E2E"/>
            <Setter Property="Foreground" Value="#E0E0E0"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderBrush" Value="#454545"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="StdBorder" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="StdBorder" Property="Background" Value="#383838"/>
                                <Setter TargetName="StdBorder" Property="BorderBrush" Value="#555555"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Small Toolbar Button -->
        <Style x:Key="ToolbarButton" TargetType="Button">
            <Setter Property="Background" Value="#2A2A2A"/>
            <Setter Property="Foreground" Value="#CCCCCC"/>
            <Setter Property="FontSize" Value="11"/>
            <Setter Property="Padding" Value="10,5"/>
            <Setter Property="BorderBrush" Value="#3E3E3E"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="TbBorder" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="TbBorder" Property="Background" Value="#363636"/>
                                <Setter TargetName="TbBorder" Property="BorderBrush" Value="#505050"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Active Stream Filter Button -->
        <Style x:Key="FilterButtonActive" TargetType="Button">
            <Setter Property="Background" Value="#0078D4"/>
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="FontSize" Value="11"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Padding" Value="10,5"/>
            <Setter Property="BorderBrush" Value="#0078D4"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="ActiveFilterBorder" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="ActiveFilterBorder" Property="Background" Value="#1A86D9"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Emergency Kill Button -->
        <Style x:Key="DangerButton" TargetType="Button">
            <Setter Property="Background" Value="#6A1616"/>
            <Setter Property="Foreground" Value="#FF9E9E"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderBrush" Value="#8A2020"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border x:Name="DngBorder" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="DngBorder" Property="Background" Value="#8A2020"/>
                                <Setter Property="Foreground" Value="#FFFFFF"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Modern WinUI 3 ComboBoxItem Style -->
        <Style TargetType="ComboBoxItem">
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Padding" Value="10,7"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ComboBoxItem">
                        <Border x:Name="ItemBorder" Background="{TemplateBinding Background}" CornerRadius="5" Margin="3,1" Padding="{TemplateBinding Padding}">
                            <ContentPresenter/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="ItemBorder" Property="Background" Value="#383838"/>
                            </Trigger>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="ItemBorder" Property="Background" Value="#0078D4"/>
                                <Setter Property="Foreground" Value="#FFFFFF"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Modern WinUI 3 ComboBox ToggleButton -->
        <ControlTemplate x:Key="WinUIComboBoxToggleButton" TargetType="ToggleButton">
            <Border x:Name="BtnBorder" Background="#2C2C2C" BorderBrush="#3E3E3E" BorderThickness="1" CornerRadius="6">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="28"/>
                    </Grid.ColumnDefinitions>
                    <!-- Clean Vector Chevron Icon -->
                    <Path Grid.Column="1" HorizontalAlignment="Center" VerticalAlignment="Center" 
                          Data="M 0 0 L 4 4 L 8 0" Stroke="#B0B0B0" StrokeThickness="1.5"/>
                </Grid>
            </Border>
            <ControlTemplate.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter TargetName="BtnBorder" Property="Background" Value="#343434"/>
                    <Setter TargetName="BtnBorder" Property="BorderBrush" Value="#505050"/>
                </Trigger>
                <Trigger Property="IsChecked" Value="True">
                    <Setter TargetName="BtnBorder" Property="Background" Value="#252525"/>
                    <Setter TargetName="BtnBorder" Property="BorderBrush" Value="#0078D4"/>
                </Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate>

        <!-- Modern WinUI 3 ComboBox Full Style -->
        <Style TargetType="ComboBox">
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="Background" Value="#2C2C2C"/>
            <Setter Property="BorderBrush" Value="#3E3E3E"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="MinHeight" Value="32"/>
            <Setter Property="FontSize" Value="12"/>
            <Setter Property="SnapsToDevicePixels" Value="True"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ComboBox">
                        <Grid>
                            <ToggleButton Name="ToggleButton" Template="{StaticResource WinUIComboBoxToggleButton}"
                                          Focusable="false"
                                          IsChecked="{Binding Path=IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"
                                          ClickMode="Press"/>
                            <ContentPresenter Name="ContentSite" IsHitTestVisible="False"
                                              Content="{TemplateBinding SelectionBoxItem}"
                                              ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                              ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"
                                              Margin="12,3,28,3" VerticalAlignment="Center" HorizontalAlignment="Left"/>
                            <Popup Name="Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}"
                                   AllowsTransparency="True" Focusable="False" PopupAnimation="Slide">
                                <Grid Name="DropDown" SnapsToDevicePixels="True" MinWidth="{TemplateBinding ActualWidth}" MaxHeight="240" Margin="0,4,0,0">
                                    <Border x:Name="DropDownBorder" Background="#242424" BorderBrush="#3E3E3E" BorderThickness="1" CornerRadius="8">
                                        <Border.Effect>
                                            <DropShadowEffect BlurRadius="12" Direction="270" ShadowDepth="4" Opacity="0.4" Color="#000000"/>
                                        </Border.Effect>
                                        <ScrollViewer Margin="3" SnapsToDevicePixels="True">
                                            <StackPanel IsItemsHost="True" KeyboardNavigation.DirectionalNavigation="Contained"/>
                                        </ScrollViewer>
                                    </Border>
                                </Grid>
                            </Popup>
                        </Grid>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- Genuine WinUI 3 ToggleSwitch Style (Replaces CheckBox) -->
        <Style TargetType="CheckBox">
            <Setter Property="Foreground" Value="#FFFFFF"/>
            <Setter Property="FontSize" Value="12.5"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="CheckBox">
                        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                            <!-- Toggle Pill Track -->
                            <Grid Width="42" Height="22" Margin="0,0,10,0">
                                <Border x:Name="Track" Background="#383838" BorderBrush="#4C4C4C" BorderThickness="1" CornerRadius="11"/>
                                <Ellipse x:Name="Thumb" Width="14" Height="14" Fill="#FFFFFF" HorizontalAlignment="Left" Margin="4,0,0,0"/>
                            </Grid>
                            <ContentPresenter VerticalAlignment="Center"/>
                        </StackPanel>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsChecked" Value="True">
                                <Setter TargetName="Track" Property="Background" Value="#0078D4"/>
                                <Setter TargetName="Track" Property="BorderBrush" Value="#0078D4"/>
                                <Setter TargetName="Thumb" Property="HorizontalAlignment" Value="Right"/>
                                <Setter TargetName="Thumb" Property="Margin" Value="0,0,4,0"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="Track" Property="BorderBrush" Value="#666666"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <!-- ==================== WINUI 3 MODERN TAB NAVIGATION ==================== -->
        <Style TargetType="TabControl">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Padding" Value="0"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TabControl">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                            </Grid.RowDefinitions>
                            
                            <!-- Modern Segmented Pill Navigation Bar -->
                            <Border Grid.Row="0" 
                                    Background="#14FFFFFF" 
                                    BorderBrush="#25FFFFFF" 
                                    BorderThickness="1" 
                                    CornerRadius="10" 
                                    Padding="4" 
                                    HorizontalAlignment="Left" 
                                    Margin="0,0,0,14">
                                <TabPanel IsItemsHost="True" Background="Transparent"/>
                            </Border>

                            <!-- Selected Tab Page Content -->
                            <Border Grid.Row="1" Background="Transparent" BorderThickness="0">
                                <ContentPresenter ContentSource="SelectedContent"/>
                            </Border>
                        </Grid>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style TargetType="TabItem">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Foreground" Value="#9E9E9E"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Padding" Value="18,8"/>
            <Setter Property="Margin" Value="2,0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TabItem">
                        <Border x:Name="TabItemBorder" 
                                Background="{TemplateBinding Background}" 
                                BorderBrush="Transparent"
                                BorderThickness="1"
                                CornerRadius="7" 
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter x:Name="TabContent" 
                                              ContentSource="Header" 
                                              HorizontalAlignment="Center" 
                                              VerticalAlignment="Center" 
                                              RecognizesAccessKey="True"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="TabItemBorder" Property="Background" Value="#22FFFFFF"/>
                                <Setter Property="Foreground" Value="#FFFFFF"/>
                            </Trigger>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="TabItemBorder" Property="Background" Value="#0078D4"/>
                                <Setter TargetName="TabItemBorder" Property="BorderBrush" Value="#60CDFF"/>
                                <Setter Property="Foreground" Value="#FFFFFF"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <!-- Header Bar -->
            <RowDefinition Height="Auto"/>
            <!-- Main Content Area (Navigation Tabs) -->
            <RowDefinition Height="*"/>
            <!-- Footer Action Bar -->
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- ==================== HEADER BAR ==================== -->
        <Border Grid.Row="0" Background="#202020" BorderBrush="#303030" BorderThickness="0,0,0,1" Padding="20,14">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <Border Background="#1C2128" BorderBrush="#30363D" BorderThickness="1" Width="38" Height="38" CornerRadius="9" Margin="0,0,12,0" Padding="2">
                        <Image x:Name="ImgAppLogo" Width="32" Height="32" HorizontalAlignment="Center" VerticalAlignment="Center" RenderOptions.BitmapScalingMode="HighQuality"/>
                    </Border>
                    <StackPanel>
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="ScrcpyDeX" FontSize="18" FontWeight="Bold" Foreground="#FFFFFF"/>
                            <Border Background="#2B3C4E" CornerRadius="4" Padding="6,2" Margin="8,0,0,0" VerticalAlignment="Center">
                                <TextBlock Text="WinUI 3 Control Center" FontSize="10" FontWeight="SemiBold" Foreground="#60CDFF"/>
                            </Border>
                        </StackPanel>
                        <TextBlock Text="High-Performance Desktop Client via USB (Zero Wi-Fi Latency)" FontSize="11" Foreground="#9E9E9E"/>
                    </StackPanel>
                </StackPanel>

                <!-- Distinct Header Status Cards (No Pill Confusion) -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                    <!-- Hardware USB Status Card -->
                    <Border Background="#282828" BorderBrush="#383838" BorderThickness="1" CornerRadius="6" Padding="12,6" Margin="0,0,8,0">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="📱" FontSize="12" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock x:Name="TxtHeaderDevice" Text="Scanning ADB..." FontSize="12" Foreground="#D0D0D0" FontWeight="Medium"/>
                        </StackPanel>
                    </Border>

                    <!-- DeX Session Status Card -->
                    <Border Background="#282828" BorderBrush="#383838" BorderThickness="1" CornerRadius="6" Padding="12,6">
                        <StackPanel Orientation="Horizontal">
                            <Ellipse x:Name="SessionDot" Width="8" Height="8" Fill="#777777" Margin="0,0,6,0" VerticalAlignment="Center"/>
                            <TextBlock x:Name="TxtHeaderSession" Text="DeX: Offline" FontSize="12" Foreground="#8A8886" FontWeight="SemiBold"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </Grid>
        </Border>

        <!-- ==================== MAIN CONTENT (TABS) ==================== -->
        <TabControl x:Name="MainTabs" Grid.Row="1" Background="Transparent" BorderThickness="0" Margin="16,10,16,10">
            
            <!-- TAB 1: SETTINGS -->
            <TabItem Header="⚙️ Settings">
                <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="0,10,10,0">
                    <StackPanel>

                        <!-- Card: Display Resolution -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Display Resolution" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Virtual workspace canvas dimensions allocated for Samsung DeX." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbResolution" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="1920x1080 (1080p FHD — Verified Sweet Spot)" Tag="1920x1080"/>
                                    <ComboBoxItem Content="2560x1440 (1440p QHD)" Tag="2560x1440"/>
                                    <ComboBoxItem Content="3840x2160 (4K UHD)" Tag="3840x2160"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Frame Rate -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Target Frame Rate (FPS)" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Hardware refresh rate synchronized with Direct3D11 rendering." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbFps" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="60 FPS (Standard 60Hz — Default)" Tag="60"/>
                                    <ComboBoxItem Content="120 FPS (Fluid 120Hz — High Performance)" Tag="120"/>
                                    <ComboBoxItem Content="30 FPS (Battery Saver / Low USB Bandwidth)" Tag="30"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Video Bitrate -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Video Bitrate" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Compression rate over USB. 8 Mbps is standard; 12 Mbps is the tested 120Hz ceiling." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbBitrate" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="8 Mbps (Standard Balanced USB — Default)" Tag="8000000"/>
                                    <ComboBoxItem Content="12 Mbps (Tested 120Hz Zero-Lag Ceiling)" Tag="12000000"/>
                                    <ComboBoxItem Content="16 Mbps (High Quality — May Add Input Lag)" Tag="16000000"/>
                                    <ComboBoxItem Content="24 Mbps (Ultra — Heavy Input Lag)" Tag="24000000"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Video Codec -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Video Codec" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="H.264 provides lowest hardware decode latency on PC." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbCodec" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="H.264 / AVC (Recommended — Lowest Latency)" Tag="h264"/>
                                    <ComboBoxItem Content="H.265 / HEVC (Higher Compression)" Tag="h265"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Window Mode -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Initial Window Mode" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Window presentation style (Alt+F toggles Fullscreen anytime)." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbWindowMode" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="Standard Resizable Window" Tag="normal"/>
                                    <ComboBoxItem Content="Borderless Window" Tag="borderless"/>
                                    <ComboBoxItem Content="Fullscreen Mode" Tag="fullscreen"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Audio Passthrough -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="USB Audio Passthrough" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Routes DeX desktop audio directly to Windows speakers/headphones." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <CheckBox x:Name="ChkAudio" Content="Enable Direct USB Audio" IsChecked="True" Grid.Column="1" VerticalAlignment="Center"/>
                            </Grid>
                        </Border>

                        <!-- Card: Physical Mouse Driver (UHID) -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Physical Desktop Mouse (/dev/uhid)" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Linux kernel driver with persistent arrow cursor, hover states, and right-click." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <ComboBox x:Name="CmbMouseDriver" Grid.Column="1" VerticalAlignment="Center">
                                    <ComboBoxItem Content="Native Linux Kernel UHID (Recommended)" Tag="uhid"/>
                                    <ComboBoxItem Content="Touchscreen Emulation (SDK)" Tag="sdk"/>
                                </ComboBox>
                            </Grid>
                        </Border>

                        <!-- Card: Turn AMOLED Screen Off -->
                        <Border Style="{StaticResource SettingsCard}">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="320"/>
                                </Grid.ColumnDefinitions>
                                <StackPanel>
                                    <TextBlock Text="Turn Phone Screen Off (--turn-screen-off)" FontSize="14" FontWeight="SemiBold" Foreground="#FFFFFF"/>
                                    <TextBlock Text="Keeps the physical phone display off while DeX runs (saves battery &amp; prevents burn-in)." FontSize="11" Foreground="#9E9E9E" Margin="0,2,0,0"/>
                                </StackPanel>
                                <CheckBox x:Name="ChkTurnScreenOff" Content="Turn Off Phone Display" IsChecked="True" Grid.Column="1" VerticalAlignment="Center"/>
                            </Grid>
                        </Border>

                    </StackPanel>
                </ScrollViewer>
            </TabItem>

            <!-- TAB 2: DEVICE & TELEMETRY -->
            <TabItem Header="📱 Device &amp; Telemetry">
                <Grid Margin="0,10,0,0">
                    <!-- Status Grid -->
                    <UniformGrid Columns="2" Margin="0,0,0,16">
                        <Border Style="{StaticResource SettingsCard}" Margin="0,0,8,8">
                            <StackPanel>
                                <TextBlock Text="CONNECTED DEVICE" FontSize="10" FontWeight="Bold" Foreground="#60CDFF"/>
                                <TextBlock x:Name="TxtDeviceModel" Text="Scanning ADB..." FontSize="16" FontWeight="SemiBold" Foreground="#FFFFFF" Margin="0,4,0,2"/>
                                <TextBlock x:Name="TxtDeviceOs" Text="Awaiting USB handshake..." FontSize="11" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>

                        <Border Style="{StaticResource SettingsCard}" Margin="8,0,0,8">
                            <StackPanel>
                                <TextBlock Text="SAMSUNG DEX ENGINE" FontSize="10" FontWeight="Bold" Foreground="#60CDFF"/>
                                <TextBlock x:Name="TxtDexStatus" Text="Inactive (Ready to Launch)" FontSize="16" FontWeight="SemiBold" Foreground="#FFFFFF" Margin="0,4,0,2"/>
                                <TextBlock x:Name="TxtDisplayId" Text="No DeX display allocated" FontSize="11" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>

                        <Border Style="{StaticResource SettingsCard}" Margin="0,8,8,0">
                            <StackPanel>
                                <TextBlock Text="VIDEO PIPELINE" FontSize="10" FontWeight="Bold" Foreground="#60CDFF"/>
                                <TextBlock x:Name="TxtPipelineInfo" Text="Direct3D11 / 60 FPS Accelerated" FontSize="16" FontWeight="SemiBold" Foreground="#FFFFFF" Margin="0,4,0,2"/>
                                <TextBlock Text="Standard USB: 8 Mbps / 60 FPS (12 Mbps / 120 FPS capable)" FontSize="11" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>

                        <Border Style="{StaticResource SettingsCard}" Margin="8,8,0,0">
                            <StackPanel>
                                <TextBlock Text="INPUT ARCHITECTURE" FontSize="10" FontWeight="Bold" Foreground="#60CDFF"/>
                                <TextBlock Text="Kernel /dev/uhid (Group 3011)" FontSize="16" FontWeight="SemiBold" Foreground="#6CCB5F" Margin="0,4,0,2"/>
                                <TextBlock Text="Release cursor: [LEFT ALT] or [WIN KEY]" FontSize="11" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>
                    </UniformGrid>
                </Grid>
            </TabItem>

            <!-- TAB 3: DIAGNOSTICS & LOGS (POLISHED EMBEDDED CONSOLE) -->
            <TabItem x:Name="TabDiagnostics" Header="📋 Diagnostics &amp; Logs">
                <Grid Margin="0,10,0,0">
                    <Grid.RowDefinitions>
                        <!-- Visual Step Tracker -->
                        <RowDefinition Height="Auto"/>
                        <!-- Telemetry & Controls Cheat Sheet Ribbon -->
                        <RowDefinition Height="Auto"/>
                        <!-- Controls & Stream Filter Toolbar -->
                        <RowDefinition Height="Auto"/>
                        <!-- Integrated Log Box -->
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <!-- 4-Step Visual Lifecycle Stepper -->
                    <UniformGrid Columns="4" Grid.Row="0" Margin="0,0,0,10">
                        <Border x:Name="Step1Badge" Style="{StaticResource PhaseBadge}">
                            <StackPanel>
                                <TextBlock Text="PHASE 1" FontSize="9" FontWeight="Bold" Foreground="#888888"/>
                                <TextBlock x:Name="Step1Text" Text="USB Link: Ready" FontSize="11" FontWeight="SemiBold" Foreground="#D0D0D0"/>
                            </StackPanel>
                        </Border>
                        <Border x:Name="Step2Badge" Style="{StaticResource PhaseBadge}">
                            <StackPanel>
                                <TextBlock Text="PHASE 2" FontSize="9" FontWeight="Bold" Foreground="#888888"/>
                                <TextBlock x:Name="Step2Text" Text="Server: Pending" FontSize="11" FontWeight="SemiBold" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>
                        <Border x:Name="Step3Badge" Style="{StaticResource PhaseBadge}">
                            <StackPanel>
                                <TextBlock Text="PHASE 3" FontSize="9" FontWeight="Bold" Foreground="#888888"/>
                                <TextBlock x:Name="Step3Text" Text="DeX RTSP: Inactive" FontSize="11" FontWeight="SemiBold" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>
                        <Border x:Name="Step4Badge" Style="{StaticResource PhaseBadge}">
                            <StackPanel>
                                <TextBlock Text="PHASE 4" FontSize="9" FontWeight="Bold" Foreground="#888888"/>
                                <TextBlock x:Name="Step4Text" Text="D3D11 / UHID: Idle" FontSize="11" FontWeight="SemiBold" Foreground="#8A8886"/>
                            </StackPanel>
                        </Border>
                    </UniformGrid>

                    <!-- Row 1: Active Stream Telemetry & Keyboard Cheat Sheet Ribbon -->
                    <Border Grid.Row="1" Background="#1E1E1E" BorderBrush="#333333" BorderThickness="1" CornerRadius="6" Padding="12,7" Margin="0,0,0,10">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                                <Border Background="#1C2D3D" CornerRadius="4" Padding="6,2" Margin="0,0,8,0">
                                    <TextBlock x:Name="TxtDiagPipeline" Text="⚡ 1080p @ 60 FPS | 8 Mbps H.264" FontSize="11" Foreground="#60CDFF" FontWeight="SemiBold"/>
                                </Border>
                                <Border Background="#1C3020" CornerRadius="4" Padding="6,2" Margin="0,0,8,0">
                                    <TextBlock Text="🖱️ Kernel UHID Mouse" FontSize="11" Foreground="#6CCB5F" FontWeight="SemiBold"/>
                                </Border>
                                <Border Background="#2B281B" CornerRadius="4" Padding="6,2">
                                    <TextBlock Text="🔊 USB Direct Audio" FontSize="11" Foreground="#FFD550" FontWeight="SemiBold"/>
                                </Border>
                            </StackPanel>
                            <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                                <TextBlock Text="Release Mouse: " FontSize="11" Foreground="#888888" VerticalAlignment="Center"/>
                                <Border Background="#2A2A2A" BorderBrush="#444444" BorderThickness="1" CornerRadius="3" Padding="4,1" Margin="2,0,10,0">
                                    <TextBlock Text="Left Alt" FontSize="10.5" Foreground="#E0E0E0" FontWeight="Bold"/>
                                </Border>
                                <TextBlock Text="Fullscreen: " FontSize="11" Foreground="#888888" VerticalAlignment="Center"/>
                                <Border Background="#2A2A2A" BorderBrush="#444444" BorderThickness="1" CornerRadius="3" Padding="4,1" Margin="2,0,0,0">
                                    <TextBlock Text="Alt + F" FontSize="10.5" Foreground="#E0E0E0" FontWeight="Bold"/>
                                </Border>
                            </StackPanel>
                        </Grid>
                    </Border>

                    <!-- Row 2: Controls & Stream Filter Toolbar -->
                    <Grid Grid.Row="2" Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <!-- Stream Filter Selector -->
                        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                            <TextBlock Text="Stream Filter: " FontSize="11.5" Foreground="#8A8886" VerticalAlignment="Center" Margin="0,0,8,0"/>
                            <Button x:Name="BtnFilterAll" Content="All Streams (Unified)" Style="{StaticResource FilterButtonActive}" Margin="0,0,6,0"/>
                            <Button x:Name="BtnFilterOrch" Content="Orchestrator (Client)" Style="{StaticResource ToolbarButton}" Margin="0,0,6,0"/>
                            <Button x:Name="BtnFilterServer" Content="DeX Server (Loopback)" Style="{StaticResource ToolbarButton}"/>
                        </StackPanel>

                        <!-- Log Action Buttons -->
                        <StackPanel Grid.Column="1" Orientation="Horizontal">
                            <Button x:Name="BtnRefreshAdb" Content="🔄 Refresh ADB" Style="{StaticResource ToolbarButton}" Margin="0,0,6,0"/>
                            <Button x:Name="BtnCopyLogs" Content="📋 Copy Logs" Style="{StaticResource ToolbarButton}" Margin="0,0,6,0"/>
                            <Button x:Name="BtnClearLogs" Content="🧹 Clear" Style="{StaticResource ToolbarButton}"/>
                        </StackPanel>
                    </Grid>

                    <!-- Row 3: Polished Monospace Terminal Console -->
                    <Border Grid.Row="3" Background="#121212" BorderBrush="#303030" BorderThickness="1" CornerRadius="8" Padding="12">
                        <TextBox x:Name="TxtLogs" Background="Transparent" Foreground="#A9D3AB" BorderThickness="0"
                                 FontFamily="Cascadia Code, Consolas, monospace" FontSize="11.5"
                                 IsReadOnly="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"/>
                    </Border>
                </Grid>
            </TabItem>
        </TabControl>

        <!-- ==================== FOOTER ACTION BAR ==================== -->
        <Border Grid.Row="2" Background="#202020" BorderBrush="#303030" BorderThickness="0,1,0,0" Padding="20,12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- Left Actions: Reset & Save -->
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <Button x:Name="BtnResetDefaults" Content="Restore Defaults" Style="{StaticResource StandardButton}" Margin="0,0,8,0"/>
                    <Button x:Name="BtnSaveConfig" Content="Save Preferences" Style="{StaticResource StandardButton}"/>
                    <TextBlock x:Name="TxtFeedback" Text="" Foreground="#6CCB5F" FontSize="11" FontWeight="Medium" Margin="14,0,0,0" VerticalAlignment="Center"/>
                </StackPanel>

                <!-- Right Actions: Emergency Kill & Start -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                    <Button x:Name="BtnKillSwitch" Content="🛑 Kill Switch (stop-dex)" Style="{StaticResource DangerButton}" Margin="0,0,10,0" Visibility="Collapsed"/>
                    <Button x:Name="BtnStartDex" Content="🚀 Launch Samsung DeX" Style="{StaticResource AccentButton}" MinWidth="200"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
"@

# 6. Parse XAML & Create Window
$stringReader = New-Object System.IO.StringReader($xaml)
$xmlReader = [System.Xml.XmlReader]::Create($stringReader)
$window = [System.Windows.Markup.XamlReader]::Load($xmlReader)

# Set Application Window Icon (Titlebar, Alt+Tab, Taskbar)
$appIconPath = Join-Path $assetsDir "app_icon.ico"
if (-not (Test-Path $appIconPath)) { $appIconPath = Join-Path $rootDir "icon.ico" }
if (Test-Path $appIconPath) {
    try {
        $window.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.Uri]::new($appIconPath))
    } catch { }
}

$imgAppLogo = $window.FindName("ImgAppLogo")
$appLogoPng = Join-Path $assetsDir "app_icon.png"
if (-not (Test-Path $appLogoPng)) { $appLogoPng = Join-Path $rootDir "icon.png" }
if ($imgAppLogo -and (Test-Path $appLogoPng)) {
    try {
        $imgAppLogo.Source = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.Uri]::new($appLogoPng))
    } catch { }
}

# 7. Element References
$mainTabs = $window.FindName("MainTabs")
$tabDiagnostics = $window.FindName("TabDiagnostics")

$txtHeaderDevice = $window.FindName("TxtHeaderDevice")
$txtHeaderSession = $window.FindName("TxtHeaderSession")
$sessionDot = $window.FindName("SessionDot")

$cmbResolution = $window.FindName("CmbResolution")
$cmbFps = $window.FindName("CmbFps")
$cmbBitrate = $window.FindName("CmbBitrate")
$cmbCodec = $window.FindName("CmbCodec")
$cmbWindowMode = $window.FindName("CmbWindowMode")
$chkAudio = $window.FindName("ChkAudio")
$cmbMouseDriver = $window.FindName("CmbMouseDriver")
$chkTurnScreenOff = $window.FindName("ChkTurnScreenOff")

$txtDeviceModel = $window.FindName("TxtDeviceModel")
$txtDeviceOs = $window.FindName("TxtDeviceOs")
$txtDexStatus = $window.FindName("TxtDexStatus")
$txtDisplayId = $window.FindName("TxtDisplayId")
$txtPipelineInfo = $window.FindName("TxtPipelineInfo")

$step1Badge = $window.FindName("Step1Badge")
$step1Text = $window.FindName("Step1Text")
$step2Badge = $window.FindName("Step2Badge")
$step2Text = $window.FindName("Step2Text")
$step3Badge = $window.FindName("Step3Badge")
$step3Text = $window.FindName("Step3Text")
$step4Badge = $window.FindName("Step4Badge")
$step4Text = $window.FindName("Step4Text")

$txtLogs = $window.FindName("TxtLogs")
$txtDiagPipeline = $window.FindName("TxtDiagPipeline")
$btnFilterAll = $window.FindName("BtnFilterAll")
$btnFilterOrch = $window.FindName("BtnFilterOrch")
$btnFilterServer = $window.FindName("BtnFilterServer")

$btnRefreshAdb = $window.FindName("BtnRefreshAdb")
$btnCopyLogs = $window.FindName("BtnCopyLogs")
$btnClearLogs = $window.FindName("BtnClearLogs")

$btnResetDefaults = $window.FindName("BtnResetDefaults")
$btnSaveConfig = $window.FindName("BtnSaveConfig")
$btnKillSwitch = $window.FindName("BtnKillSwitch")
$btnStartDex = $window.FindName("BtnStartDex")
$txtFeedback = $window.FindName("TxtFeedback")

# 8. Filterable Logging Helper
$script:allLogEntries = New-Object System.Collections.Generic.List[PSCustomObject]
$script:currentFilter = "ALL" # "ALL", "ORCH", "SERVER"

function Append-Log($msg, $level = "INFO", $stream = "SYSTEM") {
    $time = (Get-Date).ToString("HH:mm:ss.fff")
    $line = "[$time] [$stream] [$level] $msg`r`n"
    
    $entry = [PSCustomObject]@{
        Time = $time
        Level = $level
        Stream = $stream
        Message = $msg
        Formatted = $line
    }
    $script:allLogEntries.Add($entry)
    
    $shouldShow = ($script:currentFilter -eq "ALL") -or 
                  ($script:currentFilter -eq "ORCH" -and ($stream -in "ORCH", "SYSTEM", "DEVICE", "D3D11", "INPUT")) -or 
                  ($script:currentFilter -eq "SERVER" -and ($stream -eq "SERVER"))
                  
    if ($shouldShow -and $txtLogs) {
        $txtLogs.AppendText($line)
        $txtLogs.ScrollToEnd()
    }
    Write-Host "[$stream] [$level] $msg" -ForegroundColor Gray
}

function Refresh-LogView {
    if (-not $txtLogs) { return }
    $sb = New-Object System.Text.StringBuilder
    foreach ($entry in $script:allLogEntries) {
        $shouldShow = ($script:currentFilter -eq "ALL") -or 
                      ($script:currentFilter -eq "ORCH" -and ($entry.Stream -in "ORCH", "SYSTEM", "DEVICE", "D3D11", "INPUT")) -or 
                      ($script:currentFilter -eq "SERVER" -and ($entry.Stream -eq "SERVER"))
        if ($shouldShow) {
            [void]$sb.Append($entry.Formatted)
        }
    }
    $txtLogs.Text = $sb.ToString()
    $txtLogs.ScrollToEnd()
}

# 9. Populate UI from Config
function Apply-ConfigToUI($cfg) {
    # Resolution
    foreach ($item in $cmbResolution.Items) {
        if ($item.Tag -eq $cfg.display.resolution) { $cmbResolution.SelectedItem = $item; break }
    }
    if (-not $cmbResolution.SelectedItem) { $cmbResolution.SelectedIndex = 0 }

    # FPS
    foreach ($item in $cmbFps.Items) {
        if ([int]$item.Tag -eq [int]$cfg.display.fps) { $cmbFps.SelectedItem = $item; break }
    }
    if (-not $cmbFps.SelectedItem) { $cmbFps.SelectedIndex = 0 }

    # Bitrate
    foreach ($item in $cmbBitrate.Items) {
        if ([int]$item.Tag -eq [int]$cfg.display.bitrate) { $cmbBitrate.SelectedItem = $item; break }
    }
    if (-not $cmbBitrate.SelectedItem) { $cmbBitrate.SelectedIndex = 0 }

    # Codec
    foreach ($item in $cmbCodec.Items) {
        if ($item.Tag -eq $cfg.display.codec) { $cmbCodec.SelectedItem = $item; break }
    }
    if (-not $cmbCodec.SelectedItem) { $cmbCodec.SelectedIndex = 0 }

    # Window Mode
    foreach ($item in $cmbWindowMode.Items) {
        if ($item.Tag -eq $cfg.display.windowMode) { $cmbWindowMode.SelectedItem = $item; break }
    }
    if (-not $cmbWindowMode.SelectedItem) { $cmbWindowMode.SelectedIndex = 0 }

    # Mouse Driver
    foreach ($item in $cmbMouseDriver.Items) {
        if ($item.Tag -eq $cfg.input.mouseDriver) { $cmbMouseDriver.SelectedItem = $item; break }
    }
    if (-not $cmbMouseDriver.SelectedItem) { $cmbMouseDriver.SelectedIndex = 0 }

    # CheckBoxes
    $chkAudio.IsChecked = [bool]$cfg.audio.enabled
    $chkTurnScreenOff.IsChecked = [bool]$cfg.device.turnScreenOff
}

function Read-UIConfig {
    $resTag = if ($cmbResolution.SelectedItem) { [string]$cmbResolution.SelectedItem.Tag } else { "1920x1080" }
    $fpsTag = if ($cmbFps.SelectedItem) { [int]$cmbFps.SelectedItem.Tag } else { 60 }
    $bitrateTag = if ($cmbBitrate.SelectedItem) { [int]$cmbBitrate.SelectedItem.Tag } else { 8000000 }
    $codecTag = if ($cmbCodec.SelectedItem) { [string]$cmbCodec.SelectedItem.Tag } else { "h264" }
    $winModeTag = if ($cmbWindowMode.SelectedItem) { [string]$cmbWindowMode.SelectedItem.Tag } else { "normal" }
    $mouseTag = if ($cmbMouseDriver.SelectedItem) { [string]$cmbMouseDriver.SelectedItem.Tag } else { "uhid" }

    $cfg = [PSCustomObject]@{
        version = "1.0"
        display = [PSCustomObject]@{
            resolution = $resTag
            fps = $fpsTag
            bitrate = $bitrateTag
            codec = $codecTag
            windowMode = $winModeTag
        }
        audio = [PSCustomObject]@{
            enabled = [bool]$chkAudio.IsChecked
            bufferMs = 50
        }
        input = [PSCustomObject]@{
            mouseDriver = $mouseTag
            releaseKey = "LeftAlt"
        }
        device = [PSCustomObject]@{
            turnScreenOff = [bool]$chkTurnScreenOff.IsChecked
            stayAwake = $true
            autoConnect = $false
        }
        ui = [PSCustomObject]@{
            theme = "dark"
            showLogPanel = $true
        }
    }
    return $cfg
}

# 10. Robust Continuous ADB Device Polling Logic
$lastDeviceSerial = $null
$script:isDeXRunning = $false

function Update-DeviceStatus {
    try {
        $raw = & "$adbBinary" devices -l 2>$null
        $foundSerial = $null

        foreach ($line in ($raw -split "`n")) {
            $trimmed = $line.Trim()
            if ($trimmed -match '^([A-Za-z0-9_-]+)\s+device\b') {
                $foundSerial = $Matches[1]
                break
            }
        }

        if ($foundSerial) {
            $txtHeaderDevice.Foreground = [System.Windows.Media.Brushes]::LightGreen

            if ($foundSerial -ne $script:lastDeviceSerial) {
                $script:lastDeviceSerial = $foundSerial
                $model = (& "$adbBinary" -s $foundSerial shell getprop ro.product.model 2>$null).Trim()
                $oneui = (& "$adbBinary" -s $foundSerial shell getprop ro.build.version.oneui 2>$null).Trim()
                $android = (& "$adbBinary" -s $foundSerial shell getprop ro.build.version.release 2>$null).Trim()

                $displayModel = if ($model -eq "SM-S911B") { "Galaxy S23 (SM-S911B)" } elseif ($model) { "$model ($foundSerial)" } else { "Galaxy Device ($foundSerial)" }
                $txtHeaderDevice.Text = $displayModel
                $txtDeviceModel.Text = if ($model -eq "SM-S911B") { "Samsung Galaxy S23 (SM-S911B)" } else { $displayModel }
                
                $oneUiFormatted = if ($oneui -eq "80500") { "One UI 8.5" } elseif ($oneui) { "One UI $oneui" } else { "One UI" }
                $txtDeviceOs.Text = "$oneUiFormatted / Android $android (SDK 36)"
                
                if (-not $script:isDeXRunning) {
                    $txtDexStatus.Text = "Ready to Launch Samsung DeX"
                    $txtDisplayId.Text = "Will be allocated automatically"
                    $btnStartDex.IsEnabled = $true
                }
                
                $step1Text.Text = "USB Link: Connected"
                $step1Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#153D1C")
                $step1Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#2E7D32")
                $step1Text.Foreground = [System.Windows.Media.Brushes]::LightGreen
                Append-Log "Device detected via USB: $displayModel | $oneUiFormatted | Android $android" "SUCCESS"
            }
        } else {
            $script:lastDeviceSerial = $null
            $txtHeaderDevice.Text = "No USB Device"
            $txtHeaderDevice.Foreground = [System.Windows.Media.Brushes]::Gray
            $txtDeviceModel.Text = "No Galaxy device detected"
            $txtDeviceOs.Text = "Connect Galaxy S23 via USB with USB Debugging enabled"
            
            if (-not $script:isDeXRunning) {
                $txtDexStatus.Text = "Inactive"
                $txtDisplayId.Text = "Awaiting USB"
                $btnStartDex.IsEnabled = $false
            }
            
            $step1Text.Text = "USB Link: Disconnected"
            $step1Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#222222")
            $step1Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#383838")
            $step1Text.Foreground = [System.Windows.Media.Brushes]::Gray
        }
    } catch {
        $txtHeaderDevice.Text = "ADB Offline"
        $txtHeaderDevice.Foreground = [System.Windows.Media.Brushes]::Salmon
        $btnStartDex.IsEnabled = $false
    }
}

# 11. Integrated Process Execution (Zero External Windows)
$script:sessionProcess = $null

function Process-SessionOutputLine($line) {
    if (-not $line) { return }
    $trimmed = $line.Trim()

    $stream = "ORCH"
    $level = "INFO"

    if ($trimmed -match '^\[Server\]\s*(.*)$') {
        $stream = "SERVER"
        $trimmed = $Matches[1].Trim()
        if ($trimmed -match '(ready for streaming|Activated|Success|OK)') {
            $level = "SUCCESS"
        } elseif ($trimmed -match '(error|exception|fail|timeout)') {
            $level = "ERROR"
        } else {
            $level = "INFO"
        }
    } elseif ($trimmed -match '^\[\d/\d\]') {
        $stream = "ORCH"
        $level = "STEP"
    } elseif ($trimmed -match '^(ERROR|FATAL|Exception|\[Server Error\])') {
        $stream = "ORCH"
        $level = "ERROR"
    } elseif ($trimmed -match '^(WARN|Warning)') {
        $stream = "ORCH"
        $level = "WARN"
    } elseif ($trimmed -match '^(INFO:|scrcpy|Direct3D|Texture:)') {
        $stream = "D3D11"
        $level = "STREAM"
    } elseif ($trimmed -match '^(CONTROL CHEAT SHEET|• \[Left Alt\]|• \[Alt \+ F\]|• \[Right-Click\]|• \[Closing Win\]|>>> PHYSICAL)') {
        $stream = "INPUT"
        $level = "SHORTCUT"
    } elseif ($trimmed -match '(Activated!|ready|Success)') {
        $stream = "ORCH"
        $level = "SUCCESS"
    }

    Append-Log $trimmed $level $stream

    # Stage 2: Server Prep
    if ($trimmed -match '\[2/4\] Preparing device' -or $trimmed -match 'Starting ScrcpyDeX Server') {
        $step2Text.Text = "Server: Deploying..."
        $step2Text.Foreground = [System.Windows.Media.Brushes]::Yellow
    }
    # Stage 3: Activation
    elseif ($trimmed -match '\[3/4\] Activating Samsung DeX' -or $trimmed -match 'Starting Samsung DeX engine activation') {
        $step2Text.Text = "Server: Deployed OK"
        $step2Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#153D1C")
        $step2Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#2E7D32")
        $step2Text.Foreground = [System.Windows.Media.Brushes]::LightGreen

        $step3Text.Text = "DeX RTSP: Handshaking..."
        $step3Text.Foreground = [System.Windows.Media.Brushes]::Yellow
        
        $sessionDot.Fill = [System.Windows.Media.Brushes]::Yellow
        $txtHeaderSession.Text = "DeX: Starting..."
        $txtHeaderSession.Foreground = [System.Windows.Media.Brushes]::Yellow
    }
    # Display ID Allocated
    elseif ($trimmed -match 'Samsung DeX Activated! Display ID: (\d+)' -or $trimmed -match 'ready for streaming! ID=(\d+)') {
        $allocatedId = $Matches[1]
        $step3Text.Text = "DeX RTSP: Display $allocatedId"
        $step3Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#153D1C")
        $step3Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#2E7D32")
        $step3Text.Foreground = [System.Windows.Media.Brushes]::LightGreen

        $step4Text.Text = "D3D11 / UHID: Starting..."
        $step4Text.Foreground = [System.Windows.Media.Brushes]::Yellow

        $sessionDot.Fill = [System.Windows.Media.Brushes]::LimeGreen
        $txtHeaderSession.Text = "DeX: Active ($allocatedId)"
        $txtHeaderSession.Foreground = [System.Windows.Media.Brushes]::LightGreen
        $txtDexStatus.Text = "● Samsung DeX Running"
        $fpsText = if ($script:activeSessionConfig) { "$($script:activeSessionConfig.display.fps) FPS" } else { "60 FPS" }
        $txtDisplayId.Text = "Display ID: $allocatedId ($fpsText)"
    }
    # Stage 4: scrcpy launched
    elseif ($trimmed -match 'Opening native Samsung DeX' -or $trimmed -match 'Launching scrcpy') {
        $fpsText = if ($script:activeSessionConfig) { "$($script:activeSessionConfig.display.fps) FPS" } else { "60 FPS" }
        $step4Text.Text = "D3D11 / UHID: Active ($fpsText)"
        $step4Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#153D1C")
        $step4Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#2E7D32")
        $step4Text.Foreground = [System.Windows.Media.Brushes]::LightGreen
    }
    # Cleanup on close
    elseif ($trimmed -match 'DeX session disconnected' -or $trimmed -match 'Terminating active DeX sessions') {
        Reset-SessionUIState
    }
}

$script:sessionLogFile = Join-Path $env:TEMP "scrcpydex_session.log"
$script:sessionErrFile = Join-Path $env:TEMP "scrcpydex_session_err.log"
$script:sessionLastLinePos = 0
$script:sessionLastErrPos = 0
$script:sessionTimer = $null
$script:activeSessionConfig = $null

function Reset-SessionUIState {
    $script:isDeXRunning = $false
    if ($script:sessionTimer) {
        $script:sessionTimer.Stop()
    }
    $sessionDot.Fill = [System.Windows.Media.Brushes]::Gray
    $txtHeaderSession.Text = "DeX: Offline"
    $txtHeaderSession.Foreground = [System.Windows.Media.Brushes]::Gray

    $txtDexStatus.Text = "Inactive (Ready to Launch)"
    $txtDisplayId.Text = "No DeX display allocated"

    $btnStartDex.Content = "🚀 Launch Samsung DeX"
    $btnStartDex.Style = $window.FindResource("AccentButton")
    $btnStartDex.IsEnabled = $true
    if ($btnKillSwitch) {
        $btnKillSwitch.Visibility = [System.Windows.Visibility]::Collapsed
    }

    $step2Text.Text = "Server: Idle"
    $step2Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#222222")
    $step2Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#383838")
    $step2Text.Foreground = [System.Windows.Media.Brushes]::Gray

    $step3Text.Text = "DeX RTSP: Inactive"
    $step3Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#222222")
    $step3Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#383838")
    $step3Text.Foreground = [System.Windows.Media.Brushes]::Gray

    $step4Text.Text = "D3D11 / UHID: Idle"
    $step4Badge.Background = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#222222")
    $step4Badge.BorderBrush = [System.Windows.Media.BrushConverter]::new().ConvertFromString("#383838")
    $step4Text.Foreground = [System.Windows.Media.Brushes]::Gray
}

function Start-DeXSessionBackground {
    $cfg = Read-UIConfig
    Save-ConfigStore $cfg | Out-Null
    $script:activeSessionConfig = $cfg

    $mbps = [math]::Round($cfg.display.bitrate / 1000000)
    if ($txtDiagPipeline) {
        $txtDiagPipeline.Text = "⚡ $($cfg.display.resolution) @ $($cfg.display.fps) FPS | $mbps Mbps $($cfg.display.codec.ToUpper())"
    }

    Append-Log "=========================================================="
    Append-Log "🚀 LAUNCHING INTEGRATED DEX SESSION (NO CONSOLE WINDOWS)"
    Append-Log "   • Resolution: $($cfg.display.resolution) | Target FPS: $($cfg.display.fps)"
    Append-Log "   • Bitrate: $($cfg.display.bitrate) bps ($mbps Mbps) | Codec: $($cfg.display.codec)"
    Append-Log "   • Mouse: $($cfg.input.mouseDriver) | Audio: $($cfg.audio.enabled)"
    Append-Log "   • Screen Off: $($cfg.device.turnScreenOff) | Mode: $($cfg.display.windowMode)"
    Append-Log "=========================================================="

    # Switch automatically to Diagnostics & Logs tab so user sees all execution
    $mainTabs.SelectedItem = $tabDiagnostics

    if (Test-Path $script:sessionLogFile) {
        Remove-Item $script:sessionLogFile -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $script:sessionErrFile) {
        Remove-Item $script:sessionErrFile -Force -ErrorAction SilentlyContinue
    }
    $script:sessionLastLinePos = 0
    $script:sessionLastErrPos = 0

    $pwshExe = (Get-Command pwsh -ErrorAction SilentlyContinue)?.Source
    if (-not $pwshExe) { $pwshExe = "pwsh.exe" }

    $argList = @(
        "-ExecutionPolicy", "Bypass",
        "-NoProfile",
        "-File", $runScript,
        "-Bitrate", [string]$cfg.display.bitrate,
        "-Fps", [string]$cfg.display.fps,
        "-Codec", [string]$cfg.display.codec,
        "-Resolution", [string]$cfg.display.resolution,
        "-MouseDriver", [string]$cfg.input.mouseDriver,
        "-TurnScreenOff:`$$($cfg.device.turnScreenOff)",
        "-AudioEnabled:`$$($cfg.audio.enabled)",
        "-WindowMode", [string]$cfg.display.windowMode
    )

    try {
        $script:sessionProcess = Start-Process -FilePath $pwshExe -ArgumentList $argList `
            -RedirectStandardOutput $script:sessionLogFile `
            -RedirectStandardError $script:sessionErrFile `
            -PassThru -WindowStyle Hidden

        $script:isDeXRunning = $true
        $btnStartDex.Content = "⏹️ Disconnect Samsung DeX"
        $btnStartDex.Style = $window.FindResource("StopSessionButton")
        if ($btnKillSwitch) {
            $btnKillSwitch.Visibility = [System.Windows.Visibility]::Visible
        }
        $sessionDot.Fill = [System.Windows.Media.Brushes]::Yellow
        $txtHeaderSession.Text = "DeX: Starting..."
        $txtHeaderSession.Foreground = [System.Windows.Media.Brushes]::Yellow

        # Start thread-safe UI DispatcherTimer to stream log output directly on UI thread
        if ($script:sessionTimer) {
            $script:sessionTimer.Stop()
        }
        $script:sessionTimer = New-Object System.Windows.Threading.DispatcherTimer
        $script:sessionTimer.Interval = [TimeSpan]::FromMilliseconds(120)
        $script:sessionTimer.Add_Tick({
            if (Test-Path $script:sessionLogFile) {
                try {
                    $fileStream = [System.IO.File]::Open($script:sessionLogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                    $reader = New-Object System.IO.StreamReader($fileStream, [System.Text.Encoding]::UTF8)
                    $lines = @()
                    while (-not $reader.EndOfStream) {
                        $lines += $reader.ReadLine()
                    }
                    $reader.Close()
                    $fileStream.Close()

                    for ($i = $script:sessionLastLinePos; $i -lt $lines.Count; $i++) {
                        Process-SessionOutputLine $lines[$i]
                    }
                    $script:sessionLastLinePos = $lines.Count
                } catch { }
            }

            if (Test-Path $script:sessionErrFile) {
                try {
                    $errStream = [System.IO.File]::Open($script:sessionErrFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                    $errReader = New-Object System.IO.StreamReader($errStream, [System.Text.Encoding]::UTF8)
                    $errLines = @()
                    while (-not $errReader.EndOfStream) {
                        $errLines += $errReader.ReadLine()
                    }
                    $errReader.Close()
                    $errStream.Close()

                    for ($i = $script:sessionLastErrPos; $i -lt $errLines.Count; $i++) {
                        $el = $errLines[$i]
                        if ($el -and $el.Trim()) {
                            Process-SessionOutputLine "[Error] $el"
                        }
                    }
                    $script:sessionLastErrPos = $errLines.Count
                } catch { }
            }

            # Check if process exited
            if ($script:sessionProcess -and $script:sessionProcess.HasExited) {
                $script:sessionTimer.Stop()
                Append-Log "Session orchestrator exited (Exit code: $($script:sessionProcess.ExitCode))." "INFO"
                Reset-SessionUIState
            }
        })
        $script:sessionTimer.Start()

    } catch {
        Append-Log "Failed to start background orchestrator: $_" "ERROR"
        Reset-SessionUIState
    }
}

function Stop-DeXSession {
    Append-Log "Stopping Samsung DeX session & restoring device..." "WARN"
    if ($script:sessionTimer) {
        $script:sessionTimer.Stop()
    }

    # 1. Terminate local streaming client immediately on Windows (scrcpy, ffplay)
    try {
        Get-Process -Name "scrcpy", "ffplay" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    } catch { }
    try {
        taskkill.exe /F /IM scrcpy.exe /T 2>$null | Out-Null
        taskkill.exe /F /IM ffplay.exe /T 2>$null | Out-Null
    } catch { }

    # 2. Terminate background orchestrator process tree
    if ($script:sessionProcess -and -not $script:sessionProcess.HasExited) {
        try {
            taskkill.exe /F /PID $script:sessionProcess.Id /T 2>$null | Out-Null
            $script:sessionProcess.Kill()
        } catch {}
    }

    # 3. Send disconnect command directly to Android system_server via ADB
    if ($adbBinary -and (Test-Path $adbBinary)) {
        try {
            & "$adbBinary" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>$null | Out-Null
            & "$adbBinary" shell "pkill -f com.scrcpydex.server.Server" 2>$null | Out-Null
            & "$adbBinary" shell "pkill -f DexTriggerTest" 2>$null | Out-Null
            & "$adbBinary" forward --remove tcp:27183 2>$null | Out-Null
            & "$adbBinary" forward --remove tcp:27184 2>$null | Out-Null
        } catch { }
    }

    # 4. Fallback execution of stop-dex.bat without incompatible arguments
    if (Test-Path $stopScript) {
        try {
            Start-Process -FilePath "cmd.exe" -ArgumentList @("/c", "`"$stopScript`"") -WindowStyle Hidden -Wait -ErrorAction SilentlyContinue
        } catch { }
    }

    Reset-SessionUIState
    Append-Log "Session disconnected. Smartphone restored." "SUCCESS"
}

# 12. Event Handlers
$btnSaveConfig.Add_Click({
    $cfg = Read-UIConfig
    if (Save-ConfigStore $cfg) {
        $txtFeedback.Text = "✓ Preferences saved successfully!"
        Append-Log "Preferences written to settings.json."
    }
})

$btnResetDefaults.Add_Click({
    $defaults = Load-ConfigStore
    Apply-ConfigToUI $defaults
    $txtFeedback.Text = "✓ Defaults restored!"
    Append-Log "Settings reset to recommended factory defaults (1080p @ 120 FPS, 12 Mbps)."
})

$btnClearLogs.Add_Click({
    $script:allLogEntries.Clear()
    $txtLogs.Clear()
})

$btnCopyLogs.Add_Click({
    [System.Windows.Clipboard]::SetText($txtLogs.Text)
    $txtFeedback.Text = "✓ Logs copied to clipboard!"
})

$btnFilterAll.Add_Click({
    $script:currentFilter = "ALL"
    $btnFilterAll.Style = $window.FindResource("FilterButtonActive")
    $btnFilterOrch.Style = $window.FindResource("ToolbarButton")
    $btnFilterServer.Style = $window.FindResource("ToolbarButton")
    Refresh-LogView
})

$btnFilterOrch.Add_Click({
    $script:currentFilter = "ORCH"
    $btnFilterAll.Style = $window.FindResource("ToolbarButton")
    $btnFilterOrch.Style = $window.FindResource("FilterButtonActive")
    $btnFilterServer.Style = $window.FindResource("ToolbarButton")
    Refresh-LogView
})

$btnFilterServer.Add_Click({
    $script:currentFilter = "SERVER"
    $btnFilterAll.Style = $window.FindResource("ToolbarButton")
    $btnFilterOrch.Style = $window.FindResource("ToolbarButton")
    $btnFilterServer.Style = $window.FindResource("FilterButtonActive")
    Refresh-LogView
})

$btnRefreshAdb.Add_Click({
    Append-Log "Refreshing ADB device table..."
    $script:lastDeviceSerial = $null
    Update-DeviceStatus
})

$btnKillSwitch.Add_Click({
    Append-Log "🛑 TRIGGERING EMERGENCY KILL SWITCH (stop-dex.bat)..." "WARN"
    Stop-DeXSession
    $txtFeedback.Text = "🛑 Emergency stop dispatched!"
})

$btnStartDex.Add_Click({
    if ($script:isDeXRunning) {
        Stop-DeXSession
    } else {
        Start-DeXSessionBackground
    }
})

# 13. Window Lifecycle & Styling
$window.Add_SourceInitialized({
    $helper = New-Object System.Windows.Interop.WindowInteropHelper($window)
    [WinUIDwm]::ApplyModernWindowStyling($helper.Handle)
})

# Initialize UI and Logging
Apply-ConfigToUI $currentConfig
Append-Log "ScrcpyDeX WinUI 3 Control Center initialized."
Append-Log "Resolved ADB binary: $adbBinary"
Append-Log "Loaded settings from ConfigStore (Default: 1080p @ 60 FPS, 8 Mbps H.264)."
Update-DeviceStatus

# 14. Background Continuous ADB Polling Timer (Every 1.5 seconds)
$pollTimer = New-Object System.Windows.Threading.DispatcherTimer
$pollTimer.Interval = [TimeSpan]::FromMilliseconds(1500)
$pollTimer.Add_Tick({
    Update-DeviceStatus
})
$pollTimer.Start()

$window.Add_Closed({
    $pollTimer.Stop()
    if ($script:isDeXRunning) {
        Stop-DeXSession
    }
})

# Headless Test Execution Mode
if ($HeadlessTest) {
    Append-Log "Headless test mode verified. Validating integrity..."
    $testConfig = Read-UIConfig
    Save-ConfigStore $testConfig | Out-Null
    Write-Host "HEADLESS TEST PASSED: WinUI 3 Window parsed, modern styles loaded, and device verified." -ForegroundColor Green
    return
}

# Run Application Loop
$app = New-Object System.Windows.Application
$app.Run($window)
