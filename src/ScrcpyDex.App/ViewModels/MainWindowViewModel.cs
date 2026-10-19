// Copyright 2026 Aureliano Peixoto and ScrcpyDeX Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScrcpyDex.App.Models;
using ScrcpyDex.App.Services;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Core.Collections;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;
using ScrcpyDex.Core.Localization;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Models;
using ScrcpyDex.Services;

namespace ScrcpyDex.App.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window.
    /// Manages device state, video/audio configuration, scrcpy process execution, and live diagnostic logs.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly IAdbService _adbService;
        private readonly ISessionStateMachine _stateMachine;
        private readonly IScrcpyExecutionService _executionService;
        private readonly AtomicJsonConfigRepository<ScrcpyDeXSettings> _configRepo;
        private readonly AdbDeviceTracker _deviceTracker;
        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
        private IStorageProvider? _storageProvider;
        private DispatcherTimer? _telemetryTimer;
        private bool _isDisposed;

        #region Observable Properties - Device & Session State

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(SelectedDeviceSummary))]
        [NotifyPropertyChangedFor(nameof(HasSelectedDevice))]
        [NotifyPropertyChangedFor(nameof(DeviceDisplayName))]
        [NotifyPropertyChangedFor(nameof(DeviceModelCode))]
        [NotifyPropertyChangedFor(nameof(DeviceFrameLabel))]
        [NotifyPropertyChangedFor(nameof(DeviceSerialDisplay))]
        [NotifyPropertyChangedFor(nameof(ConnectionStatusText))]
        [NotifyPropertyChangedFor(nameof(ConnectionStatusColor))]
        [NotifyPropertyChangedFor(nameof(ConnectionStatusBackground))]
        [NotifyPropertyChangedFor(nameof(ConnectionDetailsText))]
        [NotifyPropertyChangedFor(nameof(HardwareModelDisplay))]
        [NotifyPropertyChangedFor(nameof(HasDeviceImage))]
        [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
        private DeviceInfo? _selectedDevice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDeviceImage))]
        private Bitmap? _deviceImage;

        [ObservableProperty]
        private ObservableCollection<DeviceInfo> _deviceList = new ObservableCollection<DeviceInfo>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(CanStop))]
        [NotifyPropertyChangedFor(nameof(IsStreaming))]
        [NotifyPropertyChangedFor(nameof(StatusBadgeColor))]
        [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopSessionCommand))]
        private SessionState _connectionState = SessionState.Idle;

        [ObservableProperty]
        private string _sessionStatusText = "Idle — Ready to start session";

        [ObservableProperty]
        private string? _scrcpyPath;

        [ObservableProperty]
        private bool _isScrcpyDetected = true;

        [ObservableProperty]
        private string? _statusWarning;

        #endregion

        #region Observable Properties - Localization

        [ObservableProperty]
        private string _currentLanguage = "en";

        [ObservableProperty]
        private string _currentLanguageCode = "EN";

        #endregion

        #region Observable Properties - Troubleshooting FAQ

        [ObservableProperty]
        private bool _isFaqOpen;

        [ObservableProperty]
        private string _faqSearchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedFaqCategoryId = "All";

        public ObservableCollection<FaqCategoryItem> FaqCategories { get; } = new ObservableCollection<FaqCategoryItem>();

        public ObservableCollection<FaqItem> AllFaqItems { get; } = new ObservableCollection<FaqItem>();

        public ObservableCollection<FaqItem> FilteredFaqItems { get; } = new ObservableCollection<FaqItem>();

        partial void OnFaqSearchQueryChanged(string value) => ApplyFaqFilter();

        partial void OnSelectedFaqCategoryIdChanged(string value) => ApplyFaqFilter();

        #endregion

        #region Observable Properties - Hardware Telemetry

        [ObservableProperty]
        private string _batteryLevel = "--";

        [ObservableProperty]
        private string _batteryTemperature = "--";

        [ObservableProperty]
        private string _usbLinkSpeed = "--";

        [ObservableProperty]
        private string _estimatedLatencyMs = "--";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HardwareModelDisplay))]
        private string _hardwareModel = "--";

        #endregion

        #region Observable Properties - Display & Stream Settings

        [ObservableProperty]
        private string _selectedResolution = "1920x1080 (FHD)";

        public IReadOnlyList<string> AvailableResolutions { get; } = new[]
        {
            "1600x900 (HD+)",
            "1920x1080 (FHD)",
            "2560x1080 (Ultrawide DeX)",
            "2560x1440 (QHD)",
            "3440x1440 (UWQHD 34\")",
            "3840x2160 (4K UHD)",
            "Native Screen Resolution"
        };

        [ObservableProperty]
        private int _selectedFps = 30;

        public IReadOnlyList<int> AvailableFps { get; } = new[] { 30, 60, 90, 120 };

        [ObservableProperty]
        private int _bitrateMbps = 18;

        [ObservableProperty]
        private string _bitrateStatusHint = "18 Mbps • Ideal Performance Balance";

        [ObservableProperty]
        private string _selectedCodec = "h265";

        public IReadOnlyList<string> AvailableCodecs { get; } = new[] { "h264", "h265", "av1" };

        [ObservableProperty]
        private string _selectedAudioCodec = "Opus 48kHz Stereo";

        public IReadOnlyList<string> AvailableAudioCodecs { get; } = new[]
        {
            "Disabled",
            "AAC 44.1kHz",
            "Opus 48kHz Stereo",
            "RAW PCM"
        };

        [ObservableProperty]
        private string _selectedRenderDriver = "Auto (Direct3D 11 / OpenGL)";

        public IReadOnlyList<string> AvailableRenderDrivers { get; } = new[]
        {
            "Auto (Direct3D 11 / OpenGL)",
            "Direct3D 11",
            "OpenGL"
        };

        #endregion

        #region Observable Properties - Input & Session Flags

        [ObservableProperty]
        private bool _mouseUhidEnabled = true;

        [ObservableProperty]
        private bool _forwardShortcuts = true;

        [ObservableProperty]
        private bool _stayAwake = false;

        [ObservableProperty]
        private bool _turnScreenOff = false;

        [ObservableProperty]
        private bool _fullscreen = false;

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private bool _lowLatencyAudio = true;

        #endregion

        #region Observable Properties - Logs & Telemetry

        public BoundedRingBuffer<ProcessLogEntry> LogEntries { get; }

        #endregion

        #region Computed Properties

        public bool CanStart => ConnectionState == SessionState.Idle && SelectedDevice != null && IsScrcpyDetected;

        public bool CanStop => ConnectionState == SessionState.StreamingActive ||
                               ConnectionState == SessionState.DeviceConnecting ||
                               ConnectionState == SessionState.ServerDeploying ||
                               ConnectionState == SessionState.DisplayNegotiating;

        public bool IsStreaming => ConnectionState == SessionState.StreamingActive;

        public string StatusBadgeColor => ConnectionState switch
        {
            SessionState.StreamingActive => "#10B981",
            SessionState.DeviceConnecting or
            SessionState.ServerDeploying or
            SessionState.DisplayNegotiating or
            SessionState.Teardown => "#F59E0B",
            SessionState.Error => "#E11D48",
            SessionState.Idle => "#64748B",
            _ => "#64748B"
        };

        public string SelectedDeviceSummary => SelectedDevice != null
            ? $"{SelectedDevice.MarketingName} ({SelectedDevice.Serial}) — {SelectedDevice.State}"
            : "No device selected";

        public bool HasSelectedDevice => SelectedDevice != null && SelectedDevice.IsConnected;

        public string DeviceDisplayName => SelectedDevice != null ? SelectedDevice.MarketingName : "No Device Connected";

        public string DeviceModelCode => SelectedDevice != null ? SelectedDevice.ModelCode : "--";

        public string DeviceFrameLabel => SelectedDevice != null ? SelectedDevice.FrameLabel : "OFFLINE";

        public string DeviceSerialDisplay => SelectedDevice?.Serial ?? string.Empty;

        public string ConnectionStatusText => HasSelectedDevice ? "Connected" : "Disconnected";

        public string ConnectionStatusColor => HasSelectedDevice ? "#10B981" : "#64748B";

        public string ConnectionStatusBackground => HasSelectedDevice ? "#132E27" : "#1E293B";

        public string ConnectionDetailsText => HasSelectedDevice
            ? "USB Link Active • Direct Link"
            : "Connect phone via USB with USB debugging enabled";

        public string HardwareModelDisplay => HasSelectedDevice && !string.IsNullOrWhiteSpace(HardwareModel) && HardwareModel != "--"
            ? HardwareModel
            : "Hardware detection idle";

        public bool HasDeviceImage => DeviceImage != null;

        #endregion

        public MainWindowViewModel(
            IAdbService? adbService = null,
            ISessionStateMachine? stateMachine = null,
            IScrcpyExecutionService? executionService = null,
            AtomicJsonConfigRepository<ScrcpyDeXSettings>? configRepo = null)
        {
            _adbService = adbService ?? new AdbService();
            _stateMachine = stateMachine ?? new SessionStateMachine();
            _executionService = executionService ?? new ScrcpyExecutionService(PlatformLifetimeManager.Default, (AdbService)_adbService);

            string configDir = DefaultPlatformPaths.Instance.GetConfigDirectory();
            Directory.CreateDirectory(configDir);
            string configFilePath = Path.Combine(configDir, "settings.json");
            _configRepo = configRepo ?? new AtomicJsonConfigRepository<ScrcpyDeXSettings>(configFilePath);

            LogEntries = new BoundedRingBuffer<ProcessLogEntry>(
                maxCapacity: 500,
                dispatcherInvoke: action => Dispatcher.UIThread.Post(action));

            _deviceTracker = new AdbDeviceTracker();
            _deviceTracker.DevicesChanged += OnDeviceTrackerDevicesChanged;

            _stateMachine.StateChanged += OnStateMachineStateChanged;
            _executionService.StreamingStarted += OnExecutionStreamingStarted;
            _executionService.ProcessExited += OnExecutionProcessExited;
            _executionService.ErrorOccurred += OnExecutionErrorOccurred;
            _executionService.LogReceived += OnExecutionLogReceived;

            InitializeConfigurationAndDiscovery();
            StartTelemetryTimer();
        }

        public void SetStorageProvider(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        private void InitializeConfigurationAndDiscovery()
        {
            // Initial FAQ catalog initialization
            foreach (var cat in FaqDataService.CreateCategories())
            {
                FaqCategories.Add(cat);
            }
            foreach (var item in FaqDataService.CreateFaqItems())
            {
                AllFaqItems.Add(item);
            }
            ApplyFaqFilter();

            // Initial language synchronization
            SetLanguageInternal(LocalizationManager.Instance.CurrentCulture.Name);

            // Initial log banner
            LogEntries.Push(new ProcessLogEntry(DateTime.Now, "ScrcpyDeX client initialized. Ready.", false));

            // Load saved settings asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _configRepo.LoadAsync().ConfigureAwait(false);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ApplyLoadedSettings(settings);
                    });
                }
                catch (Exception ex)
                {
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Error loading configuration: {ex.Message}", true));
                }

                // Verify scrcpy executable path
                await VerifyScrcpyExecutableAsync().ConfigureAwait(false);

                // Initial device refresh
                await RefreshDevicesAsync().ConfigureAwait(false);

                // Start background device tracker
                try
                {
                    _deviceTracker.Start();
                }
                catch (Exception ex)
                {
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Notice: ADB tracker background monitoring disabled ({ex.Message})", false));
                }
            });
        }

        private void StartTelemetryTimer()
        {
            _telemetryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _telemetryTimer.Tick += async (s, e) =>
            {
                await UpdateDeviceTelemetryAsync().ConfigureAwait(false);
            };
            _telemetryTimer.Start();
        }

        private void ApplyLoadedSettings(ScrcpyDeXSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.Display.Resolution))
            {
                var matchedRes = AvailableResolutions.FirstOrDefault(r =>
                    r.Equals(settings.Display.Resolution, StringComparison.OrdinalIgnoreCase) ||
                    r.StartsWith(settings.Display.Resolution, StringComparison.OrdinalIgnoreCase));
                if (matchedRes != null)
                {
                    SelectedResolution = matchedRes;
                }
            }

            if (AvailableFps.Contains(settings.Display.Fps))
            {
                SelectedFps = settings.Display.Fps;
            }

            int bitrateInMbps = settings.Display.Bitrate / 1_000_000;
            if (bitrateInMbps >= 4 && bitrateInMbps <= 60)
            {
                BitrateMbps = bitrateInMbps;
            }

            if (AvailableCodecs.Contains(settings.Display.Codec))
            {
                SelectedCodec = settings.Display.Codec;
            }

            MouseUhidEnabled = settings.Input.MouseDriver.Equals("uhid", StringComparison.OrdinalIgnoreCase);
            ForwardShortcuts = settings.Input.ForwardShortcuts;
            StayAwake = settings.Device.StayAwake;
            TurnScreenOff = settings.Device.TurnScreenOff;
            Fullscreen = settings.Display.WindowMode.Equals("fullscreen", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(settings.Display.RenderDriver))
            {
                string driver = settings.Display.RenderDriver.Trim();
                if (driver.Equals("direct3d11", StringComparison.OrdinalIgnoreCase))
                {
                    SelectedRenderDriver = AvailableRenderDrivers.FirstOrDefault(d => d.Contains("Direct3D")) ?? AvailableRenderDrivers[0];
                }
                else if (driver.Equals("opengl", StringComparison.OrdinalIgnoreCase))
                {
                    SelectedRenderDriver = AvailableRenderDrivers.FirstOrDefault(d => d.Contains("OpenGL")) ?? AvailableRenderDrivers[0];
                }
                else
                {
                    SelectedRenderDriver = AvailableRenderDrivers[0];
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.Language))
            {
                SetLanguageInternal(settings.Language);
            }

            if (!string.IsNullOrWhiteSpace(settings.Paths.ScrcpyPath))
            {
                ScrcpyPath = settings.Paths.ScrcpyPath;
            }
        }

        private async Task SaveCurrentSettingsAsync()
        {
            try
            {
                var settings = new ScrcpyDeXSettings();
                settings.Language = CurrentLanguage;
                settings.Display.Resolution = SelectedResolution;
                settings.Display.Fps = SelectedFps;
                settings.Display.Bitrate = BitrateMbps * 1_000_000;
                settings.Display.Codec = SelectedCodec;
                settings.Display.RenderDriver = ResolveEffectiveRenderDriver();
                settings.Display.WindowMode = Fullscreen ? "fullscreen" : "normal";
                settings.Input.MouseDriver = MouseUhidEnabled ? "uhid" : "aoa";
                settings.Input.ForwardShortcuts = ForwardShortcuts;
                settings.Device.StayAwake = StayAwake;
                settings.Device.TurnScreenOff = TurnScreenOff;
                settings.Paths.ScrcpyPath = ScrcpyPath;

                await _configRepo.SaveAsync(settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Warning: Failed to save settings: {ex.Message}", true));
            }
        }

        private async Task VerifyScrcpyExecutableAsync()
        {
            string? resolved = ScrcpyPathResolver.ResolveScrcpyExecutable(ScrcpyPath);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (resolved != null)
                {
                    ScrcpyPath = resolved;
                    IsScrcpyDetected = true;
                    StatusWarning = null;
                }
                else
                {
                    IsScrcpyDetected = false;
                    StatusWarning = "scrcpy executable not detected in PATH or custom directory.";
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, "Warning: scrcpy binary was not found. Please configure its location.", true));
                }
            });
        }

        partial void OnSelectedDeviceChanged(DeviceInfo? value)
        {
            UpdateDeviceImage(value);
            _ = Task.Run(async () =>
            {
                await UpdateDeviceTelemetryAsync().ConfigureAwait(false);
            });
        }

        public void UpdateDeviceImage(DeviceInfo? device)
        {
            DeviceImage = DeviceImageService.GetDeviceImage(device);
        }

        partial void OnBitrateMbpsChanged(int value)
        {
            BitrateStatusHint = value switch
            {
                <= 8 => $"{value} Mbps • Low Latency & High Compression",
                <= 16 => $"{value} Mbps • Balanced for Standard USB / Wi-Fi",
                <= 24 => $"{value} Mbps • Ideal Performance Balance",
                <= 35 => $"{value} Mbps • High Quality Definition",
                _ => $"{value} Mbps • Maximum Fidelity (Direct USB 3.0+)"
            };
        }

        public async Task UpdateDeviceTelemetryAsync()
        {
            if (SelectedDevice == null || SelectedDevice.State != "device")
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BatteryLevel = "--";
                    BatteryTemperature = "--";
                    EstimatedLatencyMs = "--";
                    UsbLinkSpeed = "--";
                    HardwareModel = "--";
                });
                return;
            }

            string serial = SelectedDevice.Serial;
            try
            {
                // 1. Measure latency and verify active connection
                var sw = Stopwatch.StartNew();
                var pingResult = await _adbService.RunCommandAsync($"-s {serial} shell echo 1").ConfigureAwait(false);
                sw.Stop();

                if (!pingResult.Succeeded)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        EstimatedLatencyMs = "--";
                    });
                    return;
                }

                string latency = $"{Math.Max(1, sw.ElapsedMilliseconds)} ms";

                // 2. Query dumpsys battery
                var battResult = await _adbService.RunCommandAsync($"-s {serial} shell dumpsys battery").ConfigureAwait(false);
                string level = "--";
                string temp = "--";

                if (battResult.Succeeded && !string.IsNullOrWhiteSpace(battResult.Output))
                {
                    var levelMatch = Regex.Match(battResult.Output, @"level:\s*(\d+)");
                    if (levelMatch.Success)
                    {
                        level = $"{levelMatch.Groups[1].Value}%";
                    }

                    var tempMatch = Regex.Match(battResult.Output, @"temperature:\s*(\d+)");
                    if (tempMatch.Success && double.TryParse(tempMatch.Groups[1].Value, out double rawTemp))
                    {
                        temp = $"{rawTemp / 10.0:F1}°C";
                    }
                }

                // 3. Hardware Model / SoC
                string hardware = HardwareModel;
                if (SelectedDevice.Model.Contains("S23") || SelectedDevice.Model.Contains("SM-S911") || SelectedDevice.Model.Contains("SM-S918"))
                {
                    hardware = "Adreno 740 • Snapdragon 8 Gen 2";
                }
                else
                {
                    var socResult = await _adbService.RunCommandAsync($"-s {serial} shell getprop ro.soc.model").ConfigureAwait(false);
                    string soc = socResult.Output?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(soc))
                    {
                        var chipResult = await _adbService.RunCommandAsync($"-s {serial} shell getprop ro.board.platform").ConfigureAwait(false);
                        soc = chipResult.Output?.Trim() ?? "";
                    }

                    if (!string.IsNullOrWhiteSpace(soc))
                    {
                        hardware = FormatSocName(soc);
                    }
                    else if (hardware == "--")
                    {
                        hardware = "Hardware Accelerated";
                    }
                }

                string linkSpeed = await DetectUsbLinkSpeedAsync(serial).ConfigureAwait(false);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BatteryLevel = level;
                    BatteryTemperature = temp;
                    EstimatedLatencyMs = latency;
                    HardwareModel = hardware;
                    UsbLinkSpeed = linkSpeed;
                });
            }
            catch
            {
                // Non-blocking telemetry query fallback
            }
        }

        private async Task<string> DetectUsbLinkSpeedAsync(string serial)
        {
            if (serial.Contains(':'))
            {
                return "Wi-Fi";
            }

            try
            {
                // Query Linux USB Device Controller current speed via sysfs
                var udcResult = await _adbService.RunCommandAsync($"-s {serial} shell \"cat /sys/class/udc/*/current_speed 2>/dev/null\"").ConfigureAwait(false);
                string speedRaw = udcResult.Output?.Trim().ToLowerInvariant() ?? "";
                if (speedRaw.Contains("super-speed-plus"))
                {
                    return "10 Gbps";
                }
                if (speedRaw.Contains("super-speed") || speedRaw.Contains("superspeed"))
                {
                    return "5 Gbps";
                }
                if (speedRaw.Contains("high-speed") || speedRaw.Contains("highspeed"))
                {
                    return "480 Mbps";
                }
                if (speedRaw.Contains("full-speed") || speedRaw.Contains("fullspeed"))
                {
                    return "12 Mbps";
                }

                // Fallback: dumpsys usb
                var usbDump = await _adbService.RunCommandAsync($"-s {serial} shell dumpsys usb").ConfigureAwait(false);
                string dumpText = usbDump.Output?.ToLowerInvariant() ?? "";
                if (dumpText.Contains("super-speed-plus")) return "10 Gbps";
                if (dumpText.Contains("super-speed") || dumpText.Contains("superspeed")) return "5 Gbps";
                if (dumpText.Contains("high-speed") || dumpText.Contains("highspeed")) return "480 Mbps";

                return "USB Link";
            }
            catch
            {
                return "USB Link";
            }
        }

        private static string FormatSocName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Adreno 740 • Snapdragon 8 Gen 2";
            string lower = raw.ToLowerInvariant();
            if (lower.Contains("sm8550") || lower.Contains("kalama")) return "Adreno 740 • Snapdragon 8 Gen 2";
            if (lower.Contains("sm8650") || lower.Contains("pineapple")) return "Adreno 750 • Snapdragon 8 Gen 3";
            if (lower.Contains("sm8450") || lower.Contains("taro")) return "Adreno 730 • Snapdragon 8 Gen 1";
            if (lower.Contains("sm8350") || lower.Contains("lahaina")) return "Adreno 660 • Snapdragon 888";
            if (lower.Contains("sm8250") || lower.Contains("kona")) return "Adreno 650 • Snapdragon 865";
            if (lower.Contains("exynos 2400") || lower.Contains("s5e9945")) return "Xclipse 940 • Exynos 2400";
            if (lower.Contains("exynos 2200") || lower.Contains("s5e9925")) return "Xclipse 920 • Exynos 2200";
            if (lower.Contains("exynos")) return $"Mali/Xclipse • Samsung {raw}";
            if (lower.Contains("snapdragon")) return $"Adreno GPU • {raw}";
            return $"Hardware Accelerated • {raw}";
        }

        public string BuildCliCommandPreview()
        {
            string display = SelectedResolution;
            int parenIdx = display.IndexOf('(');
            if (parenIdx > 0) display = display.Substring(0, parenIdx).Trim();
            string resParam = display.Contains('x', StringComparison.OrdinalIgnoreCase)
                ? $"--max-size={display.Split(new[] { 'x', 'X' })[0].Trim()} "
                : "";

            string targetSerial = SelectedDevice != null ? $"-s {SelectedDevice.Serial} " : "";
            string recParam = IsRecording ? $"--record=\"DeX_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4\" " : "";
            string screenOffParam = TurnScreenOff ? "--turn-screen-off " : "";
            string stayAwakeParam = StayAwake ? "--stay-awake " : "";
            string uhidParam = MouseUhidEnabled ? "--mouse=uhid " : "";
            string kbdParam = ForwardShortcuts ? "--keyboard=uhid " : "";
            string fsParam = Fullscreen ? "--fullscreen " : "";

            string effectiveDriver = ResolveEffectiveRenderDriver();
            string driverParam = (effectiveDriver == "auto" && OperatingSystem.IsWindows())
                ? ""
                : $"--render-driver={(effectiveDriver == "auto" ? "opengl" : effectiveDriver)} ";

            return $"scrcpy {targetSerial}--video-codec={SelectedCodec} {resParam}-b {BitrateMbps}M --max-fps={SelectedFps} {driverParam}{uhidParam}{kbdParam}{screenOffParam}{stayAwakeParam}{fsParam}{recParam}--window-title=\"ScrcpyDeX\"".Replace("  ", " ").Trim();
        }

        private string ResolveEffectiveRenderDriver()
        {
            if (SelectedRenderDriver.StartsWith("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return "auto";
            }
            if (SelectedRenderDriver.Contains("Direct3D", StringComparison.OrdinalIgnoreCase))
            {
                return OperatingSystem.IsWindows() ? "direct3d11" : "opengl";
            }
            if (SelectedRenderDriver.Contains("OpenGL", StringComparison.OrdinalIgnoreCase))
            {
                return "opengl";
            }
            return "auto";
        }

        #region Event Handlers

        private void OnStateMachineStateChanged(object? sender, SessionStateTransitionEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ConnectionState = e.CurrentState;
                SessionStatusText = $"{e.CurrentState}: {e.Message ?? GetDefaultStateMessage(e.CurrentState)}";
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"State transition: {e.PreviousState} -> {e.CurrentState} ({e.Trigger})", false));
            });
        }

        private string GetDefaultStateMessage(SessionState state) => state switch
        {
            SessionState.Idle => "Ready to start session",
            SessionState.DeviceConnecting => "Connecting to ADB target...",
            SessionState.ServerDeploying => "Deploying Samsung DeX server component...",
            SessionState.DisplayNegotiating => "Allocating virtual display with system_server...",
            SessionState.StreamingActive => "DeX session active and streaming",
            SessionState.Teardown => "Tearing down DeX session...",
            SessionState.Error => "Session error encountered",
            _ => state.ToString()
        };

        private void OnExecutionStreamingStarted(object? sender, EventArgs e)
        {
            _ = _stateMachine.FireAsync(SessionTrigger.StreamingStarted, "scrcpy rendering active");
        }

        private void OnExecutionProcessExited(object? sender, int exitCode)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"scrcpy window closed (exit code: {exitCode})", exitCode != 0));
            });

            _ = Task.Run(async () =>
            {
                await PerformFullTeardownAsync($"scrcpy window closed ({exitCode})").ConfigureAwait(false);
            });
        }

        private void OnExecutionErrorOccurred(object? sender, string error)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (error.Contains("D3D11CreateDevice", StringComparison.OrdinalIgnoreCase) ||
                    (error.Contains("Could not create renderer", StringComparison.OrdinalIgnoreCase) && error.Contains("direct3d", StringComparison.OrdinalIgnoreCase)))
                {
                    StatusWarning = "D3D11 unsupported: Switch Render Driver to OpenGL";
                }
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Execution error: {error}", true));
            });
            _ = _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, error);
        }

        private void OnExecutionLogReceived(object? sender, ProcessLogEntry entry)
        {
            LogEntries.Push(entry);
        }

        private void OnDeviceTrackerDevicesChanged(object? sender, IReadOnlyList<DeviceInfo> devices)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateDevicesList(devices);
            });
        }

        private void UpdateDevicesList(IReadOnlyList<DeviceInfo> devices)
        {
            var previousSerial = SelectedDevice?.Serial;

            if (devices == null || devices.Count == 0)
            {
                DeviceList.Clear();
                SelectedDevice = null;
                UpdateDeviceImage(null);
                BatteryLevel = "--";
                BatteryTemperature = "--";
                EstimatedLatencyMs = "--";
                UsbLinkSpeed = "--";
                HardwareModel = "--";
                SessionStatusText = "No ADB devices detected. Connect phone via USB with USB debugging enabled.";
                return;
            }

            // Update DeviceList in place to prevent clearing ComboBox selection to null
            for (int i = DeviceList.Count - 1; i >= 0; i--)
            {
                if (!devices.Any(d => d.Serial == DeviceList[i].Serial))
                {
                    DeviceList.RemoveAt(i);
                }
            }

            foreach (var d in devices)
            {
                var existing = DeviceList.FirstOrDefault(x => x.Serial == d.Serial);
                if (existing == null)
                {
                    DeviceList.Add(d);
                }
                else
                {
                    existing.Model = d.Model;
                    existing.Manufacturer = d.Manufacturer;
                    existing.State = d.State;
                    existing.AndroidVersion = d.AndroidVersion;
                    existing.OneUiVersion = d.OneUiVersion;
                    existing.DeXDisplayId = d.DeXDisplayId;
                }
            }

            var matched = !string.IsNullOrEmpty(previousSerial)
                ? DeviceList.FirstOrDefault(d => d.Serial == previousSerial)
                : null;

            SelectedDevice = matched ?? DeviceList.FirstOrDefault();
            UpdateDeviceImage(SelectedDevice);

            _ = Task.Run(async () =>
            {
                await EnrichUnknownDevicesAsync().ConfigureAwait(false);
                await UpdateDeviceTelemetryAsync().ConfigureAwait(false);
            });
        }

        private async Task EnrichUnknownDevicesAsync()
        {
            foreach (var dev in DeviceList.ToList())
            {
                if (dev.State == "device" && (dev.Model == "Unknown" || string.IsNullOrWhiteSpace(dev.AndroidVersion)))
                {
                    try
                    {
                        var enriched = await _adbService.QueryDeviceInfoAsync(dev.Serial).ConfigureAwait(false);
                        if (enriched != null && enriched.Model != "Unknown" && !string.IsNullOrWhiteSpace(enriched.Model))
                        {
                            dev.Model = enriched.Model;
                            dev.AndroidVersion = enriched.AndroidVersion;
                            dev.OneUiVersion = enriched.OneUiVersion;
                            dev.Manufacturer = enriched.Manufacturer;

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                UpdateDeviceImage(SelectedDevice);
                                OnPropertyChanged(nameof(SelectedDevice));
                                OnPropertyChanged(nameof(DeviceDisplayName));
                                OnPropertyChanged(nameof(DeviceModelCode));
                                OnPropertyChanged(nameof(DeviceFrameLabel));
                                OnPropertyChanged(nameof(SelectedDeviceSummary));
                                OnPropertyChanged(nameof(HasDeviceImage));
                            });
                        }
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        public void SetBitratePreset(object? parameter)
        {
            if (parameter is int val)
            {
                BitrateMbps = val;
            }
            else if (parameter is string s && int.TryParse(s, out int parsed))
            {
                BitrateMbps = parsed;
            }
        }

        [RelayCommand]
        public void ToggleRecording()
        {
            IsRecording = !IsRecording;
        }

        [RelayCommand]
        public async Task CopyCliCommandAsync()
        {
            try
            {
                string cmd = BuildCliCommandPreview();
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(cmd).ConfigureAwait(false);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"CLI command copied to clipboard: {cmd}", false));
                        });
                        return;
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"CLI Command: {cmd}", false));
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Failed to copy CLI command: {ex.Message}", true));
                });
            }
        }

        [RelayCommand]
        public async Task RefreshDevicesAsync()
        {
            try
            {
                SessionStatusText = "Scanning for connected ADB devices...";
                var devices = await _adbService.GetConnectedDevicesAsync().ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateDevicesList(devices);
                    SessionStatusText = devices.Count > 0
                        ? $"Found {devices.Count} device(s)"
                        : "No ADB devices detected. Connect phone via USB with USB debugging enabled.";
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SessionStatusText = $"Device scan failed: {ex.Message}";
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Error during device refresh: {ex.Message}", true));
                });
            }
        }

        [RelayCommand]
        public async Task StartSessionAsync()
        {
            if (SelectedDevice == null)
            {
                SessionStatusText = "Cannot start session: No device selected.";
                return;
            }

            if (!await _sessionLock.WaitAsync(0).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                await SaveCurrentSettingsAsync().ConfigureAwait(false);

                bool started = await _stateMachine.FireAsync(SessionTrigger.TriggerStart, "Initiating DeX session").ConfigureAwait(false);
                if (!started)
                {
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, "State machine rejected start trigger.", true));
                    return;
                }

                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Starting DeX session for device {SelectedDevice.Model}...", false));

                // Step 1: Deploy server and activate DeX virtual display
                await _stateMachine.FireAsync(SessionTrigger.DeviceReady, "Deploying DeX server").ConfigureAwait(false);
                int? displayId = await _adbService.DeployAndActivateDeXAsync(SelectedDevice.Serial).ConfigureAwait(false);

                if (!displayId.HasValue || displayId.Value < 0)
                {
                    string errMsg = "Failed to allocate Samsung DeX virtual display ID.";
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, errMsg, true));
                    await _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, errMsg).ConfigureAwait(false);
                    return;
                }

                SelectedDevice.DeXDisplayId = displayId.Value;
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"DeX virtual display allocated successfully (Display ID: {displayId.Value})", false));
                await _stateMachine.FireAsync(SessionTrigger.ServerDeployed, $"Display ID: {displayId.Value} allocated").ConfigureAwait(false);

                // Step 2: Configure recording if enabled
                string? recordFile = null;
                if (IsRecording)
                {
                    try
                    {
                        string recDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ScrcpyDeX");
                        Directory.CreateDirectory(recDir);
                        recordFile = Path.Combine(recDir, $"DeX_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
                        LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Recording enabled: {recordFile}", false));
                    }
                    catch (Exception ex)
                    {
                        LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Warning: Could not prepare recording directory: {ex.Message}", true));
                    }
                }

                string? renderDriver = ResolveEffectiveRenderDriver();

                // Step 3: Build DisplayConfig and start scrcpy
                var displayConfig = new DisplayConfig
                {
                    Resolution = SelectedResolution,
                    Fps = SelectedFps,
                    Bitrate = BitrateMbps * 1_000_000,
                    Codec = SelectedCodec,
                    WindowMode = Fullscreen ? "fullscreen" : "normal",
                    MouseDriver = MouseUhidEnabled ? "uhid" : "aoa",
                    StayAwake = StayAwake,
                    TurnScreenOff = TurnScreenOff,
                    RecordFile = recordFile,
                    ForwardShortcuts = ForwardShortcuts,
                    AudioEnabled = !SelectedAudioCodec.Equals("Disabled", StringComparison.OrdinalIgnoreCase),
                    RenderDriver = renderDriver
                };

                bool execOk = await _executionService.StartAsync(displayConfig, SelectedDevice.Serial, displayId.Value).ConfigureAwait(false);
                if (!execOk)
                {
                    string errMsg = "scrcpy launch failed or exited prematurely.";
                    LogEntries.Push(new ProcessLogEntry(DateTime.Now, errMsg, true));
                    await _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, errMsg).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Exception starting session: {ex.Message}", true));
                await _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        [RelayCommand]
        public async Task StopSessionAsync()
        {
            await PerformFullTeardownAsync("User requested session stop").ConfigureAwait(false);
        }

        private async Task PerformFullTeardownAsync(string reason)
        {
            await _sessionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Teardown sequence initiated: {reason}", false));
                if (_stateMachine.CurrentState != SessionState.Idle && _stateMachine.CurrentState != SessionState.Teardown)
                {
                    await _stateMachine.FireAsync(SessionTrigger.UserStop, reason).ConfigureAwait(false);
                }

                // Step 1: Terminate PC-side scrcpy process cleanly, fallback to kill
                await _executionService.StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                // Step 2: Unregister virtual display and reset server on phone via ADB
                string? serial = SelectedDevice?.Serial;
                if (!string.IsNullOrWhiteSpace(serial))
                {
                    await _adbService.StopDeXSessionAsync(serial).ConfigureAwait(false);
                }

                // Step 3: Reset state machine to Idle
                await _stateMachine.FireAsync(SessionTrigger.Reset, "Teardown completed cleanly").ConfigureAwait(false);
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "DeX session fully stopped. Display restored to mobile.", false));
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Error during session teardown: {ex.Message}", true));
                await _stateMachine.FireAsync(SessionTrigger.Reset, "Teardown error fallback").ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        [RelayCommand]
        public async Task BrowseScrcpyPathAsync()
        {
            var storageProvider = GetStorageProvider();
            if (storageProvider == null)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "File picker unavailable on current window lifetime.", true));
                return;
            }

            try
            {
                var options = new FilePickerOpenOptions
                {
                    Title = "Select scrcpy Executable",
                    AllowMultiple = false
                };

                if (OperatingSystem.IsWindows())
                {
                    options.FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Executable Files (*.exe)") { Patterns = new[] { "*.exe" } },
                        FilePickerFileTypes.All
                    };
                }

                var files = await storageProvider.OpenFilePickerAsync(options).ConfigureAwait(false);
                if (files != null && files.Count > 0)
                {
                    var localPath = files[0].TryGetLocalPath();
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            ScrcpyPath = localPath;
                            await VerifyScrcpyExecutableAsync().ConfigureAwait(false);
                            await SaveCurrentSettingsAsync().ConfigureAwait(false);
                            LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Custom scrcpy path set to: {localPath}", false));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Error selecting scrcpy path: {ex.Message}", true));
            }
        }

        [RelayCommand]
        public void ClearLogs()
        {
            LogEntries.ClearAll();
            LogEntries.Push(new ProcessLogEntry(DateTime.Now, "Terminal logs cleared.", false));
        }

        [RelayCommand]
        public async Task ChangeLanguageAsync(string? cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                return;
            }

            SetLanguageInternal(cultureCode);
            await SaveCurrentSettingsAsync().ConfigureAwait(false);
        }

        private void SetLanguageInternal(string cultureCode)
        {
            LocalizationManager.Instance.SetCulture(cultureCode);
            CurrentLanguage = LocalizationManager.Instance.CurrentCulture.Name;
            CurrentLanguageCode = ResolveLanguageCode(CurrentLanguage);

            foreach (var cat in FaqCategories)
            {
                cat.RefreshLocalizedStrings();
            }
            foreach (var item in AllFaqItems)
            {
                item.RefreshLocalizedStrings();
            }
            ApplyFaqFilter();
        }

        [RelayCommand]
        public void ToggleFaq()
        {
            IsFaqOpen = !IsFaqOpen;
        }

        [RelayCommand]
        public void CloseFaq()
        {
            IsFaqOpen = false;
        }

        [RelayCommand]
        public void SelectFaqCategory(string? categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId)) return;

            SelectedFaqCategoryId = categoryId;
            foreach (var cat in FaqCategories)
            {
                cat.IsSelected = string.Equals(cat.Id, categoryId, StringComparison.OrdinalIgnoreCase);
            }
            ApplyFaqFilter();
        }

        [RelayCommand]
        public void ToggleFaqItemExpanded(FaqItem? item)
        {
            if (item != null)
            {
                item.IsExpanded = !item.IsExpanded;
            }
        }

        [RelayCommand]
        public async Task CopyFaqCommandTextAsync(string? commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) return;

            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(commandText).ConfigureAwait(false);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Command copied to clipboard: {commandText}", false));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Failed to copy command: {ex.Message}", true));
            }
        }

        private void ApplyFaqFilter()
        {
            FilteredFaqItems.Clear();
            string query = (FaqSearchQuery ?? string.Empty).Trim();
            string category = SelectedFaqCategoryId ?? "All";

            var culture = LocalizationManager.Instance.CurrentCulture;

            foreach (var item in AllFaqItems)
            {
                if (category != "All" && !string.Equals(item.CategoryId, category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(query))
                {
                    bool matchTitle = culture.CompareInfo.IndexOf(item.Title, query, CompareOptions.IgnoreCase) >= 0;
                    bool matchSymptom = culture.CompareInfo.IndexOf(item.Symptom, query, CompareOptions.IgnoreCase) >= 0;
                    bool matchCause = culture.CompareInfo.IndexOf(item.Cause, query, CompareOptions.IgnoreCase) >= 0;
                    bool matchSolution = culture.CompareInfo.IndexOf(item.Solution, query, CompareOptions.IgnoreCase) >= 0;
                    bool matchCommand = item.CommandCode != null && item.CommandCode.Contains(query, StringComparison.OrdinalIgnoreCase);

                    if (!matchTitle && !matchSymptom && !matchCause && !matchSolution && !matchCommand)
                    {
                        continue;
                    }
                }

                FilteredFaqItems.Add(item);
            }

            OnPropertyChanged(nameof(HasNoFaqResults));
        }

        public bool HasNoFaqResults => FilteredFaqItems.Count == 0;

        private static string ResolveLanguageCode(string cultureCode)
        {
            if (cultureCode.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                return "PT";
            }
            if (cultureCode.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            {
                return "ES";
            }
            if (cultureCode.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            {
                return "DE";
            }
            if (cultureCode.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
            {
                return "FR";
            }
            if (cultureCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "ZH";
            }
            return "EN";
        }

        private IStorageProvider? GetStorageProvider()
        {
            if (_storageProvider != null) return _storageProvider;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                return TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
            }

            return null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _telemetryTimer?.Stop();
            _stateMachine.StateChanged -= OnStateMachineStateChanged;
            _executionService.StreamingStarted -= OnExecutionStreamingStarted;
            _executionService.ProcessExited -= OnExecutionProcessExited;
            _executionService.ErrorOccurred -= OnExecutionErrorOccurred;
            _executionService.LogReceived -= OnExecutionLogReceived;
            _deviceTracker.DevicesChanged -= OnDeviceTrackerDevicesChanged;

            _ = _deviceTracker.DisposeAsync();
            _executionService.Dispose();
            _sessionLock.Dispose();
        }

        #endregion
    }
}
