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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Core.Collections;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;
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
        private bool _isDisposed;

        #region Observable Properties - Device & Session State

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(SelectedDeviceSummary))]
        [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
        private DeviceInfo? _selectedDevice;

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

        #region Observable Properties - Display & Stream Settings

        [ObservableProperty]
        private string _selectedResolution = "1920x1080";

        public IReadOnlyList<string> AvailableResolutions { get; } = new[]
        {
            "1920x1080",
            "2560x1440",
            "1600x900",
            "3840x2160"
        };

        [ObservableProperty]
        private int _selectedFps = 60;

        public IReadOnlyList<int> AvailableFps { get; } = new[] { 30, 60, 120 };

        [ObservableProperty]
        private int _bitrateMbps = 8;

        [ObservableProperty]
        private string _selectedCodec = "h264";

        public IReadOnlyList<string> AvailableCodecs { get; } = new[] { "h264", "h265", "av1" };

        #endregion

        #region Observable Properties - Input & Window Settings

        [ObservableProperty]
        private bool _mouseUhidEnabled = true;

        [ObservableProperty]
        private bool _stayAwake = false;

        [ObservableProperty]
        private bool _turnScreenOff = false;

        [ObservableProperty]
        private bool _fullscreen = false;

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
            SessionState.StreamingActive => "#4CAF50",
            SessionState.DeviceConnecting or
            SessionState.ServerDeploying or
            SessionState.DisplayNegotiating or
            SessionState.Teardown => "#FF9800",
            SessionState.Error => "#F44336",
            SessionState.Idle => "#9E9E9E",
            _ => "#9E9E9E"
        };

        public string SelectedDeviceSummary => SelectedDevice != null
            ? $"{SelectedDevice.Model} ({SelectedDevice.Serial}) — {SelectedDevice.State}"
            : "No device selected";

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
        }

        public void SetStorageProvider(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        private void InitializeConfigurationAndDiscovery()
        {
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

        private void ApplyLoadedSettings(ScrcpyDeXSettings settings)
        {
            if (AvailableResolutions.Contains(settings.Display.Resolution))
            {
                SelectedResolution = settings.Display.Resolution;
            }

            if (AvailableFps.Contains(settings.Display.Fps))
            {
                SelectedFps = settings.Display.Fps;
            }

            int bitrateInMbps = settings.Display.Bitrate / 1_000_000;
            if (bitrateInMbps >= 4 && bitrateInMbps <= 30)
            {
                BitrateMbps = bitrateInMbps;
            }

            if (AvailableCodecs.Contains(settings.Display.Codec))
            {
                SelectedCodec = settings.Display.Codec;
            }

            MouseUhidEnabled = settings.Input.MouseDriver.Equals("uhid", StringComparison.OrdinalIgnoreCase);
            StayAwake = settings.Device.StayAwake;
            TurnScreenOff = settings.Device.TurnScreenOff;
            Fullscreen = settings.Display.WindowMode.Equals("fullscreen", StringComparison.OrdinalIgnoreCase);

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
                settings.Display.Resolution = SelectedResolution;
                settings.Display.Fps = SelectedFps;
                settings.Display.Bitrate = BitrateMbps * 1_000_000;
                settings.Display.Codec = SelectedCodec;
                settings.Display.WindowMode = Fullscreen ? "fullscreen" : "normal";
                settings.Input.MouseDriver = MouseUhidEnabled ? "uhid" : "aoa";
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
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"scrcpy process terminated with exit code {exitCode}", exitCode != 0));
            });
            _ = _stateMachine.FireAsync(SessionTrigger.ProcessExited, $"Process exited ({exitCode})");
        }

        private void OnExecutionErrorOccurred(object? sender, string error)
        {
            Dispatcher.UIThread.Post(() =>
            {
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
            DeviceList.Clear();

            foreach (var d in devices)
            {
                DeviceList.Add(d);
            }

            if (!string.IsNullOrEmpty(previousSerial))
            {
                SelectedDevice = DeviceList.FirstOrDefault(d => d.Serial == previousSerial);
            }

            if (SelectedDevice == null && DeviceList.Count > 0)
            {
                SelectedDevice = DeviceList[0];
            }
        }

        #endregion

        #region Commands

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

                // Step 2: Build DisplayConfig and start scrcpy
                var displayConfig = new DisplayConfig
                {
                    Resolution = SelectedResolution,
                    Fps = SelectedFps,
                    Bitrate = BitrateMbps * 1_000_000,
                    Codec = SelectedCodec,
                    WindowMode = Fullscreen ? "fullscreen" : "normal",
                    MouseDriver = MouseUhidEnabled ? "uhid" : "aoa",
                    StayAwake = StayAwake,
                    TurnScreenOff = TurnScreenOff
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
            if (!await _sessionLock.WaitAsync(0).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "Stopping DeX session...", false));
                await _stateMachine.FireAsync(SessionTrigger.UserStop, "User requested session stop").ConfigureAwait(false);

                await _executionService.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

                if (SelectedDevice != null)
                {
                    await _adbService.StopDeXSessionAsync(SelectedDevice.Serial).ConfigureAwait(false);
                }

                await _stateMachine.FireAsync(SessionTrigger.Reset, "Session stopped cleanly").ConfigureAwait(false);
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "DeX session stopped and cleaned up.", false));
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Error stopping session: {ex.Message}", true));
                await _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        [RelayCommand]
        public async Task EmergencyKillAsync()
        {
            try
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "EMERGENCY KILL SWITCH TRIGGERED: Halting all processes immediately.", true));
                await _stateMachine.EmergencyHaltAsync("User activated emergency kill switch").ConfigureAwait(false);

                await _executionService.EmergencyKillAsync().ConfigureAwait(false);

                if (SelectedDevice != null)
                {
                    try
                    {
                        await _adbService.StopDeXSessionAsync(SelectedDevice.Serial).ConfigureAwait(false);
                    }
                    catch { }
                }

                LogEntries.Push(new ProcessLogEntry(DateTime.Now, "All processes terminated. System reset to idle.", false));
                await _stateMachine.FireAsync(SessionTrigger.Reset, "Emergency halt complete").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogEntries.Push(new ProcessLogEntry(DateTime.Now, $"Emergency kill exception: {ex.Message}", true));
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
