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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Core.Collections;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.Lifecycle;
using ScrcpyDex.Models;
using ScrcpyDex.Services;

namespace ScrcpyDex.ViewModels
{
    /// <summary>
    /// Master MVVM ViewModel for the ScrcpyDeX modern client.
    /// Orchestrates the session finite state machine, raw ADB daemon event stream,
    /// atomic NTFS configuration repository, and native scrcpy streaming engine.
    /// </summary>
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ISessionStateMachine _stateMachine;
        private readonly AtomicJsonConfigRepository<ScrcpyDeXSettings> _configRepo;
        private readonly AdbDeviceTracker _deviceTracker;
        private readonly IScrcpyExecutionService? _executionService;
        private readonly IAdbService? _adbService;
        private bool _isDisposed;

        #region Observable Properties

        [ObservableProperty]
        private ScrcpyDeXSettings _settings;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusPillText))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusColor))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusDetails))]
        [NotifyCanExecuteChangedFor(nameof(StartDeXCommand))]
        private DeviceInfo? _currentDevice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsIdle))]
        [NotifyPropertyChangedFor(nameof(IsStreaming))]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(CanStop))]
        [NotifyPropertyChangedFor(nameof(ShowForceCloseButton))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusPillText))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusColor))]
        [NotifyPropertyChangedFor(nameof(DeviceStatusDetails))]
        [NotifyCanExecuteChangedFor(nameof(StartDeXCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopDeXCommand))]
        private SessionState _currentState = SessionState.Idle;

        [ObservableProperty]
        private string _statusMessage = "Ready for Samsung DeX";

        [ObservableProperty]
        private bool _isLogPanelVisible = true;

        [ObservableProperty]
        private bool _isAutoScrollEnabled = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowForceCloseButton))]
        private bool _isForceCloseRequested;

        #endregion

        #region Computed State Properties

        public bool IsIdle => CurrentState == SessionState.Idle;

        public bool IsStreaming => CurrentState == SessionState.StreamingActive;

        public bool CanStart => CurrentState == SessionState.Idle && CurrentDevice != null && CurrentDevice.IsConnected;

        public bool CanStop => CurrentState != SessionState.Idle && CurrentState != SessionState.Error;

        public bool ShowForceCloseButton => IsForceCloseRequested || CurrentState == SessionState.Teardown || CurrentState == SessionState.Error;

        public string DeviceStatusPillText
        {
            get
            {
                if (CurrentState == SessionState.DeviceConnecting ||
                    CurrentState == SessionState.ServerDeploying ||
                    CurrentState == SessionState.DisplayNegotiating)
                {
                    return "Connecting...";
                }

                if (CurrentDevice != null && CurrentDevice.IsConnected)
                {
                    return $"{CurrentDevice.Model} Connected";
                }

                return "Disconnected";
            }
        }

        public string DeviceStatusColor
        {
            get
            {
                if (CurrentState == SessionState.DeviceConnecting ||
                    CurrentState == SessionState.ServerDeploying ||
                    CurrentState == SessionState.DisplayNegotiating)
                {
                    return "Orange";
                }

                if (CurrentDevice != null && CurrentDevice.IsConnected)
                {
                    return "Green";
                }

                return "Red";
            }
        }

        public string DeviceStatusDetails
        {
            get
            {
                if (CurrentDevice == null || !CurrentDevice.IsConnected)
                {
                    return "No device detected. Connect a Samsung Galaxy device via USB.";
                }

                string oneUi = string.IsNullOrEmpty(CurrentDevice.OneUiVersion) ? "One UI" : $"One UI {CurrentDevice.OneUiVersion}";
                string android = string.IsNullOrEmpty(CurrentDevice.AndroidVersion) ? "Android" : $"Android {CurrentDevice.AndroidVersion}";
                return $"{CurrentDevice.Manufacturer} {CurrentDevice.Model} ({CurrentDevice.Serial}) • {oneUi} • {android}";
            }
        }

        #endregion

        #region UI Selection Options

        public IReadOnlyList<string> Resolutions { get; } = new[]
        {
            "1920x1080",
            "2560x1440",
            "3840x2160",
            "1600x900",
            "1280x720"
        };

        public IReadOnlyList<int> FpsOptions { get; } = new[] { 60, 90, 120, 30 };

        public IReadOnlyList<int> BitrateOptions { get; } = new[]
        {
            4000000,
            8000000,
            12000000,
            16000000,
            24000000
        };

        public IReadOnlyList<string> CodecOptions { get; } = new[] { "h264", "h265", "av1" };

        public IReadOnlyList<string> WindowModeOptions { get; } = new[] { "normal", "fullscreen", "borderless" };

        public IReadOnlyList<string> MouseDriverOptions { get; } = new[] { "uhid", "sdk" };

        public IReadOnlyList<string> ReleaseKeyOptions { get; } = new[] { "LeftAlt", "RightAlt", "Control", "Escape", "F11" };

        public IReadOnlyList<int> AudioBufferOptions { get; } = new[] { 20, 50, 80, 120, 200 };

        #endregion

        #region Diagnostics Collection

        public BoundedRingBuffer<string> DiagnosticLogs { get; }

        #endregion

        #region Constructors & Initialization

        public MainViewModel()
            : this(null, null, null, null, null)
        {
        }

        public MainViewModel(
            ISessionStateMachine? stateMachine = null,
            AtomicJsonConfigRepository<ScrcpyDeXSettings>? configRepo = null,
            AdbDeviceTracker? deviceTracker = null,
            IScrcpyExecutionService? executionService = null,
            IAdbService? adbService = null)
        {
            _stateMachine = stateMachine ?? new SessionStateMachine();

            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScrcpyDeX");
            string configPath = Path.Combine(configDir, "settings.json");
            _configRepo = configRepo ?? new AtomicJsonConfigRepository<ScrcpyDeXSettings>(configPath);

            _deviceTracker = deviceTracker ?? new AdbDeviceTracker();
            _executionService = executionService ?? new ScrcpyExecutionService();
            _adbService = adbService ?? AdbService.Instance;

            _settings = new ScrcpyDeXSettings();
            DiagnosticLogs = new BoundedRingBuffer<string>(1000, Application.Current?.Dispatcher);

            // Subscribe to State Machine transitions
            _stateMachine.StateChanged += OnStateMachineStateChanged;

            // Subscribe to raw ADB daemon track-devices stream
            _deviceTracker.DevicesChanged += OnDeviceTrackerDevicesChanged;

            // Subscribe to execution engine events
            if (_executionService != null)
            {
                _executionService.LogReceived += (s, e) => Log(e.Text, e.IsError ? "ERR" : "SCRCPY");
                _executionService.StreamingStarted += (s, e) =>
                {
                    RunOnUI(async () =>
                    {
                        await _stateMachine.FireAsync(SessionTrigger.StreamingStarted, "Streaming active");
                        StatusMessage = $"Streaming Samsung DeX ({Settings.Display.Resolution} @ {Settings.Display.Fps} FPS)";
                    });
                };
                _executionService.ProcessExited += (s, exitCode) =>
                {
                    RunOnUI(async () =>
                    {
                        Log($"scrcpy process exited with code {exitCode}");
                        await _stateMachine.FireAsync(SessionTrigger.ProcessExited, $"Process exited (code {exitCode})");
                        await _stateMachine.FireAsync(SessionTrigger.Reset);
                        StatusMessage = "Session disconnected. Ready.";
                    });
                };
                _executionService.ErrorOccurred += (s, err) =>
                {
                    RunOnUI(async () =>
                    {
                        Log($"Execution engine error: {err}", "ERR");
                        await _stateMachine.FireAsync(SessionTrigger.ErrorOccurred, err);
                        StatusMessage = $"Error: {err}";
                    });
                };
            }

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            Log("ScrcpyDeX Presentation Layer initialized.");

            try
            {
                Settings = await _configRepo.LoadAsync().ConfigureAwait(false);
                Log("Configuration successfully loaded from storage.");
            }
            catch (Exception ex)
            {
                Log($"Failed to load configuration, fallback to defaults: {ex.Message}", "WARN");
            }

            try
            {
                _deviceTracker.Start();
                Log("ADB Device Tracker active on TCP 127.0.0.1:5037 (Zero-Polling Push Protocol).");
            }
            catch (Exception ex)
            {
                Log($"Could not start ADB wire protocol tracker: {ex.Message}", "WARN");
            }

            // Initial device discovery probe
            if (_adbService != null)
            {
                try
                {
                    var initial = await _adbService.DetectDeviceAsync().ConfigureAwait(false);
                    if (initial.IsConnected)
                    {
                        RunOnUI(() => UpdateDevice(initial));
                    }
                }
                catch { }
            }
        }

        #endregion

        #region Event Handlers

        private void OnStateMachineStateChanged(object? sender, SessionStateTransitionEventArgs e)
        {
            RunOnUI(() =>
            {
                CurrentState = e.CurrentState;
                if (!string.IsNullOrWhiteSpace(e.Message))
                {
                    StatusMessage = e.Message;
                }
                Log($"[State Machine] {e.PreviousState} -> {e.CurrentState} (Trigger: {e.Trigger}){(string.IsNullOrEmpty(e.Message) ? "" : ": " + e.Message)}");
            });
        }

        private void OnDeviceTrackerDevicesChanged(object? sender, IReadOnlyList<DeviceInfo> devices)
        {
            RunOnUI(async () =>
            {
                var connected = devices.FirstOrDefault(d => d.IsConnected);
                if (connected != null)
                {
                    if (string.IsNullOrEmpty(connected.AndroidVersion) && _adbService != null)
                    {
                        try
                        {
                            var enriched = await _adbService.QueryDeviceInfoAsync(connected.Serial);
                            connected = enriched;
                        }
                        catch { }
                    }
                    UpdateDevice(connected);
                }
                else
                {
                    UpdateDevice(null);
                }
            });
        }

        private void UpdateDevice(DeviceInfo? device)
        {
            CurrentDevice = device;

            if (device != null && device.IsConnected)
            {
                Log($"Device detected: {device.Manufacturer} {device.Model} ({device.Serial}) - {device.State}");
                if (CurrentState == SessionState.Idle)
                {
                    StatusMessage = $"{device.Model} Connected ({device.Serial})";
                }

                if (Settings.Device.AutoConnect && CanStart)
                {
                    Log("Auto-connect triggered by settings policy.", "AUTO");
                    _ = StartDeX();
                }
            }
            else
            {
                Log("Device disconnected or unavailable.", "WARN");
                if (CurrentState == SessionState.Idle)
                {
                    StatusMessage = "No device detected. Connect a Samsung Galaxy device via USB.";
                }
                else if (CurrentState == SessionState.StreamingActive)
                {
                    _ = _stateMachine.FireAsync(SessionTrigger.DeviceLost, "USB connection lost");
                }
            }
        }

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task StartDeX()
        {
            if (!CanStart || CurrentDevice == null) return;

            try
            {
                Log($"Initiating Samsung DeX session on {CurrentDevice.Model} ({CurrentDevice.Serial})...");
                StatusMessage = "Connecting to device...";
                await _stateMachine.FireAsync(SessionTrigger.TriggerStart, "Starting Samsung DeX...");

                int? dexDisplayId = null;
                if (_adbService != null)
                {
                    _adbService.ResetResilienceState();
                    StatusMessage = "Deploying scrcpydex-server.jar and activating DeX...";
                    await _stateMachine.FireAsync(SessionTrigger.DeviceReady, "Device authorized");

                    dexDisplayId = await _adbService.DeployAndActivateDeXAsync(CurrentDevice.Serial);
                    if (!dexDisplayId.HasValue)
                    {
                        Log("Display ID not returned by activator stdout, probing dumpsys display...", "WARN");
                        dexDisplayId = await _adbService.FindDeXDisplayIdAsync(CurrentDevice.Serial);
                    }

                    if (dexDisplayId.HasValue)
                    {
                        CurrentDevice.DeXDisplayId = dexDisplayId.Value;
                        Log($"Samsung DeX Virtual Display acquired: ID #{dexDisplayId.Value}");
                    }
                    else
                    {
                        Log("Could not detect Samsung DeX virtual display, falling back to scrcpy autodetection.", "WARN");
                    }

                    await _stateMachine.FireAsync(SessionTrigger.ServerDeployed, "Server deployed");
                }

                StatusMessage = "Launching hardware-accelerated Direct3D11 streaming engine...";
                if (_executionService != null)
                {
                    bool started = await _executionService.StartAsync(Settings, CurrentDevice.Serial, dexDisplayId);
                    if (started)
                    {
                        await _stateMachine.FireAsync(SessionTrigger.StreamingStarted, "DeX streaming active");
                        StatusMessage = $"Streaming Samsung DeX ({Settings.Display.Resolution} @ {Settings.Display.Fps} FPS)";
                        Log($"DeX streaming established successfully on {CurrentDevice.Model}.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to launch scrcpy streaming process.");
                    }
                }
                else
                {
                    await _stateMachine.FireAsync(SessionTrigger.StreamingStarted, "Streaming active (standalone)");
                    StatusMessage = "Streaming active (standalone mode)";
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting DeX session: {ex.Message}", "ERR");
                StatusMessage = $"Connection failed: {ex.Message}";
                await _stateMachine.EmergencyHaltAsync(ex.Message);
                await _stateMachine.FireAsync(SessionTrigger.Reset);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private async Task StopDeX()
        {
            try
            {
                IsForceCloseRequested = true;
                Log("Terminating Samsung DeX session...");
                StatusMessage = "Stopping session...";
                await _stateMachine.FireAsync(SessionTrigger.UserStop, "Disconnect requested");

                // Immediately yield to UI thread so "Forçar Fechamento" renders instantly
                await Task.Yield();

                if (_executionService != null)
                {
                    await _executionService.StopAsync(TimeSpan.FromMilliseconds(200));
                }

                if (_adbService != null && CurrentDevice != null)
                {
                    await _adbService.StopDeXSessionAsync(CurrentDevice.Serial);
                }

                await _stateMachine.FireAsync(SessionTrigger.Reset, "Session stopped");
                StatusMessage = "Session disconnected. Ready.";
                Log("DeX session cleanly terminated.");
            }
            catch (Exception ex)
            {
                Log($"Error stopping DeX session: {ex.Message}", "ERR");
                await _stateMachine.FireAsync(SessionTrigger.Reset);
            }
            finally
            {
                IsForceCloseRequested = false;
            }
        }

        [RelayCommand]
        private async Task EmergencyKill()
        {
            try
            {
                Log("FORÇAR FECHAMENTO / KILL SWITCH ATIVADO! Encerrando todos os processos...", "CRIT");
                StatusMessage = "Forçar fechamento acionado!";
                await _stateMachine.EmergencyHaltAsync("Emergency kill switch activated");

                if (_executionService != null)
                {
                    await _executionService.EmergencyKillAsync();
                }

                if (_adbService != null && CurrentDevice != null)
                {
                    await _adbService.StopDeXSessionAsync(CurrentDevice.Serial);
                }

                await _stateMachine.FireAsync(SessionTrigger.Reset, "Emergency kill reset");
                StatusMessage = "Todos os processos finalizados. Pronto.";
                Log("Forçar fechamento concluído.");
            }
            catch (Exception ex)
            {
                Log($"Erro durante o fechamento forçado: {ex.Message}", "ERR");
                StatusMessage = "Fechamento forçado concluído com avisos.";
            }
            finally
            {
                IsForceCloseRequested = false;
            }
        }

        [RelayCommand]
        private async Task SaveSettings()
        {
            try
            {
                await _configRepo.SaveAsync(Settings);
                StatusMessage = "Configuration saved successfully.";
                Log("Settings successfully committed to settings.json via two-phase atomic write.");
            }
            catch (Exception ex)
            {
                Log($"Failed to save settings: {ex.Message}", "ERR");
                StatusMessage = "Failed to save settings.";
            }
        }

        [RelayCommand]
        private async Task ResetDefaults()
        {
            try
            {
                Settings = new ScrcpyDeXSettings();
                await _configRepo.SaveAsync(Settings);
                StatusMessage = "Configuration reset to defaults.";
                Log("Settings reset to factory defaults and saved to settings.json.");
            }
            catch (Exception ex)
            {
                Log($"Failed to reset settings: {ex.Message}", "ERR");
            }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            DiagnosticLogs.ClearAll();
            Log("Diagnostic logs buffer cleared.");
        }

        [RelayCommand]
        private void CopyLogs()
        {
            try
            {
                string allLogs = string.Join(Environment.NewLine, DiagnosticLogs);
                Clipboard.SetText(allLogs);
                StatusMessage = "Diagnostic logs copied to clipboard.";
                Log("All diagnostic logs copied to clipboard.");
            }
            catch (Exception ex)
            {
                Log($"Failed to copy logs to clipboard: {ex.Message}", "ERR");
            }
        }

        #endregion

        #region Helpers & Diagnostics

        public void Log(string message, string level = "INFO")
        {
            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            DiagnosticLogs.Push(entry);
        }

        private static void RunOnUI(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        public async void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try { await _deviceTracker.DisposeAsync(); } catch { }
            if (_executionService != null)
            {
                try { await _executionService.DisposeAsync(); } catch { }
            }
        }

        #endregion
    }
}

namespace ScrcpyDex.WinUI.ViewModels
{
    /// <summary>
    /// Backward-compatibility alias for ScrcpyDex.WinUI namespace consumers.
    /// </summary>
    public class MainViewModel : ScrcpyDex.ViewModels.MainViewModel
    {
        public MainViewModel() : base() { }
        public MainViewModel(
            ScrcpyDex.Core.Lifecycle.ISessionStateMachine? stateMachine = null,
            ScrcpyDex.Core.Configuration.AtomicJsonConfigRepository<ScrcpyDex.Core.Configuration.ScrcpyDeXSettings>? configRepo = null,
            ScrcpyDex.Core.Adb.AdbDeviceTracker? deviceTracker = null,
            ScrcpyDex.Services.IScrcpyExecutionService? executionService = null,
            ScrcpyDex.Services.IAdbService? adbService = null)
            : base(stateMachine, configRepo, deviceTracker, executionService, adbService)
        {
        }
    }
}
