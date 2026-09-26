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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;
using ScrcpyDex.Models;

namespace ScrcpyDex.Services
{
    public interface IScrcpyExecutionService : IAsyncDisposable, IDisposable
    {
        bool IsRunning { get; }
        int? CurrentProcessId { get; }
        string? ResolvedScrcpyPath { get; }
        string? ResolvedServerJarPath { get; }

        event EventHandler? StreamingStarted;
        event EventHandler<int>? ProcessExited;
        event EventHandler<string>? ErrorOccurred;
        event EventHandler<ProcessLogEntry>? LogReceived;

        Task<bool> StartAsync(ScrcpyDeXSettings settings, string? serial = null, int? displayId = null, CancellationToken ct = default);
        Task<bool> StartAsync(DisplayConfig config, string? serial = null, int? displayId = null, CancellationToken ct = default);
        Task StopAsync(TimeSpan? timeout = null);
        Task EmergencyKillAsync();
    }

    /// <summary>
    /// Production-grade execution engine for scrcpy.exe.
    /// Directly spawns the native scrcpy process without any PowerShell or batch script indirection.
    /// Assigns all processes to Windows Job Objects for zero-orphan containment and streams I/O asynchronously.
    /// </summary>
    public sealed class ScrcpyExecutionService : IScrcpyExecutionService
    {
        private readonly ProcessJobTracker _jobTracker;
        private readonly AdbService? _adbService;
        private readonly string? _customScrcpyPath;
        private readonly string? _customServerJarPath;

        private Process? _currentProcess;
        private NonBlockingProcessStream? _processStream;
        private TaskCompletionSource<int>? _exitTcs;
        private readonly object _stateLock = new object();
        private bool _isDisposed;
        private bool _streamingStartedFired;
        private bool _stopping;

        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentProcess != null && !_currentProcess.HasExited;
                }
            }
        }

        public int? CurrentProcessId
        {
            get
            {
                lock (_stateLock)
                {
                    try
                    {
                        return (_currentProcess != null && !_currentProcess.HasExited) ? _currentProcess.Id : null;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        public string? ResolvedScrcpyPath { get; private set; }
        public string? ResolvedServerJarPath { get; private set; }

        public event EventHandler? StreamingStarted;
        public event EventHandler<int>? ProcessExited;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<ProcessLogEntry>? LogReceived;

        public ScrcpyExecutionService(
            ProcessJobTracker? jobTracker = null,
            AdbService? adbService = null,
            string? customScrcpyPath = null,
            string? customServerJarPath = null)
        {
            _jobTracker = jobTracker ?? ProcessJobTracker.Default;
            _adbService = adbService;
            _customScrcpyPath = customScrcpyPath;
            _customServerJarPath = customServerJarPath;

            ResolvedScrcpyPath = ScrcpyPathResolver.ResolveScrcpyExecutable(_customScrcpyPath);
            ResolvedServerJarPath = ScrcpyPathResolver.ResolveServerJar(_customServerJarPath);
        }

        /// <summary>
        /// Starts scrcpy directly with parameters mapped from ScrcpyDeXSettings.
        /// </summary>
        public async Task<bool> StartAsync(
            ScrcpyDeXSettings settings,
            string? serial = null,
            int? displayId = null,
            CancellationToken ct = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            lock (_stateLock)
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("A scrcpy session is already running. Call StopAsync() first.");
                }
                _stopping = false;
                _streamingStartedFired = false;
                _exitTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            // Resolve scrcpy binary
            string scrcpyPath = ScrcpyPathResolver.ResolveScrcpyExecutable(_customScrcpyPath);
            ResolvedScrcpyPath = scrcpyPath;

            if (!File.Exists(scrcpyPath) && scrcpyPath != "scrcpy.exe" && scrcpyPath != "scrcpy")
            {
                throw new FileNotFoundException($"scrcpy executable not found at '{scrcpyPath}'. Please verify installation.", scrcpyPath);
            }

            // Resolve or query DeX display ID if not provided
            int? effectiveDisplayId = displayId;
            if (!effectiveDisplayId.HasValue && !string.IsNullOrWhiteSpace(serial) && _adbService != null)
            {
                try
                {
                    effectiveDisplayId = await _adbService.FindDeXDisplayIdAsync(serial, ct).ConfigureAwait(false);
                }
                catch
                {
                    // Fallback to scrcpy autodetection
                }
            }

            // Build argument list
            var argsList = BuildArgumentsList(settings, serial, effectiveDisplayId);

            var psi = new ProcessStartInfo
            {
                FileName = scrcpyPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Set working directory if scrcpy executable path is known
            if (File.Exists(scrcpyPath))
            {
                psi.WorkingDirectory = Path.GetDirectoryName(scrcpyPath) ?? AppContext.BaseDirectory;
            }

            foreach (string arg in argsList)
            {
                psi.ArgumentList.Add(arg);
            }

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            process.Exited += OnProcessExitedInternal;

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Process.Start failed to launch scrcpy.exe.");
                }

                // Register with Windows Job Object immediately for kernel-level orphan termination
                _jobTracker.AssignProcess(process);

                // Initialize non-blocking asynchronous stream pumping
                var processStream = new NonBlockingProcessStream(4096);
                processStream.EntryReceived += OnProcessLogEntryReceived;
                processStream.Bind(process);

                lock (_stateLock)
                {
                    _currentProcess = process;
                    _processStream = processStream;
                }

                // Monitor startup: if the process exits within 500ms, it failed to start
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(600, ct).ConfigureAwait(false);
                        lock (_stateLock)
                        {
                            if (!_streamingStartedFired && _currentProcess != null && !_currentProcess.HasExited)
                            {
                                _streamingStartedFired = true;
                                StreamingStarted?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                }, ct);

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch { }

                process.Dispose();
                ErrorOccurred?.Invoke(this, $"Failed to launch scrcpy: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Overload to start scrcpy from DisplayConfig model.
        /// </summary>
        public Task<bool> StartAsync(
            DisplayConfig config,
            string? serial = null,
            int? displayId = null,
            CancellationToken ct = default)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var settings = MapConfigToSettings(config);
            return StartAsync(settings, serial, displayId, ct);
        }

        /// <summary>
        /// Stops the running scrcpy session gracefully, then forces termination if unresponsive.
        /// </summary>
        public async Task StopAsync(TimeSpan? timeout = null)
        {
            Process? proc;
            NonBlockingProcessStream? stream;

            lock (_stateLock)
            {
                if (_stopping || _currentProcess == null) return;
                _stopping = true;
                proc = _currentProcess;
                stream = _processStream;
            }

            TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMilliseconds(350);

            if (proc != null && !proc.HasExited)
            {
                try
                {
                    // 1. Attempt clean UI window close
                    proc.CloseMainWindow();
                }
                catch { }

                // 2. Wait up to 150ms for process to exit cleanly
                using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
                try
                {
                    await proc.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                }
                catch { }

                // 3. Immediately terminate process tree if not exited
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                }
                catch { }
            }

            // Also terminate any stray scrcpy.exe processes on the host PC
            try
            {
                foreach (var p in Process.GetProcessesByName("scrcpy"))
                {
                    try
                    {
                        if (!p.HasExited) p.Kill(entireProcessTree: true);
                    }
                    catch { }
                }
            }
            catch { }

            if (stream != null)
            {
                try
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
            }

            lock (_stateLock)
            {
                _currentProcess = null;
                _processStream = null;
                _stopping = false;
                _streamingStartedFired = false;
            }
        }

        /// <summary>
        /// Immediately terminates the scrcpy process tree with zero grace period.
        /// </summary>
        public async Task EmergencyKillAsync()
        {
            Process? proc;
            NonBlockingProcessStream? stream;

            lock (_stateLock)
            {
                proc = _currentProcess;
                stream = _processStream;
                _currentProcess = null;
                _processStream = null;
                _stopping = false;
                _streamingStartedFired = false;
            }

            if (proc != null)
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                }
                catch { }
                finally
                {
                    proc.Dispose();
                }
            }

            // Terminate any remaining scrcpy.exe instances on Windows
            try
            {
                foreach (var p in Process.GetProcessesByName("scrcpy"))
                {
                    try
                    {
                        if (!p.HasExited) p.Kill(entireProcessTree: true);
                    }
                    catch { }
                }
            }
            catch { }

            if (stream != null)
            {
                try
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
            }
        }

        /// <summary>
        /// Builds the list of scrcpy command line arguments based on ScrcpyDeXSettings.
        /// </summary>
        public static List<string> BuildArgumentsList(ScrcpyDeXSettings settings, string? serial = null, int? displayId = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var args = new List<string>();

            // Target specific device if serial is provided
            if (!string.IsNullOrWhiteSpace(serial))
            {
                args.Add("-s");
                args.Add(serial);
            }

            // Direct3D 11 rendering for zero-latency D3D11 swapchain flip model
            args.Add("--render-driver=direct3d11");

            // Hardware UHID mouse injection
            string mouseDriver = !string.IsNullOrWhiteSpace(settings.Input?.MouseDriver)
                ? settings.Input.MouseDriver
                : "uhid";
            args.Add($"--mouse={mouseDriver}");

            // Video Bitrate
            int bitrate = settings.Display?.Bitrate ?? 8000000;
            if (bitrate > 0)
            {
                args.Add($"--video-bit-rate={bitrate}");
            }

            // Max FPS
            int fps = settings.Display?.Fps ?? 60;
            if (fps > 0)
            {
                args.Add($"--max-fps={fps}");
            }

            // Max size / resolution dimension
            int maxSize = ResolveMaxSize(settings.Display);
            if (maxSize > 0)
            {
                args.Add($"--max-size={maxSize}");
            }

            // Video Codec
            string codec = !string.IsNullOrWhiteSpace(settings.Display?.Codec)
                ? settings.Display.Codec
                : "h264";
            args.Add($"--video-codec={codec}");

            // Audio configuration
            if (settings.Audio != null && !settings.Audio.Enabled)
            {
                args.Add("--no-audio");
            }
            else if (settings.Audio != null && settings.Audio.BufferMs > 0)
            {
                args.Add($"--audio-buffer={settings.Audio.BufferMs}");
            }

            // Device flags
            if (settings.Device?.TurnScreenOff == true)
            {
                args.Add("--turn-screen-off");
            }

            if (settings.Device?.StayAwake == true)
            {
                args.Add("--stay-awake");
            }

            // DeX Display selection
            if (displayId.HasValue && displayId.Value >= 0)
            {
                args.Add($"--display-id={displayId.Value}");
            }

            // Window mode
            string windowMode = settings.Display?.WindowMode?.ToLowerInvariant() ?? "normal";
            if (windowMode == "fullscreen")
            {
                args.Add("--fullscreen");
            }
            else if (windowMode == "borderless")
            {
                args.Add("--window-borderless");
            }

            // Window title
            args.Add("--window-title=ScrcpyDeX - Samsung DeX Compatible Client");

            return args;
        }

        private static int ResolveMaxSize(DisplaySection? display)
        {
            if (display == null) return 1920;

            if (display.Width.HasValue && display.Height.HasValue && display.Width.Value > 0 && display.Height.Value > 0)
            {
                return Math.Max(display.Width.Value, display.Height.Value);
            }

            if (!string.IsNullOrWhiteSpace(display.Resolution))
            {
                string res = display.Resolution.Trim();
                if (res.Contains('x', StringComparison.OrdinalIgnoreCase))
                {
                    var parts = res.Split(new[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0].Trim(), out int w) &&
                        int.TryParse(parts[1].Trim(), out int h))
                    {
                        return Math.Max(w, h);
                    }
                }
                else if (int.TryParse(res, out int singleVal) && singleVal > 0)
                {
                    return singleVal;
                }
            }

            return 1920;
        }

        public static ScrcpyDeXSettings MapConfigToSettings(DisplayConfig config)
        {
            return new ScrcpyDeXSettings
            {
                Display = new DisplaySection
                {
                    Resolution = config.Resolution,
                    Width = config.Width,
                    Height = config.Height,
                    Dpi = config.Dpi,
                    Fps = config.Fps,
                    Bitrate = config.Bitrate,
                    Codec = config.Codec,
                    WindowMode = config.WindowMode
                },
                Audio = new AudioSection
                {
                    Enabled = config.AudioEnabled,
                    BufferMs = 50
                },
                Input = new InputSection
                {
                    MouseDriver = config.MouseDriver,
                    ReleaseKey = config.ReleaseKey
                },
                Device = new DeviceSection
                {
                    TurnScreenOff = config.TurnScreenOff,
                    StayAwake = config.StayAwake
                }
            };
        }

        private void OnProcessLogEntryReceived(ProcessLogEntry entry)
        {
            LogReceived?.Invoke(this, entry);

            // Detect successful streaming activation
            if (!_streamingStartedFired && IsStreamingStartedIndicator(entry.Text))
            {
                _streamingStartedFired = true;
                StreamingStarted?.Invoke(this, EventArgs.Empty);
            }

            // Detect errors in process log stream
            if (entry.IsError || IsErrorIndicator(entry.Text))
            {
                ErrorOccurred?.Invoke(this, entry.Text);
            }
        }

        private static bool IsStreamingStartedIndicator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            return text.Contains("Renderer: direct3d11", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Renderer:", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Initial texture", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Texture:", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Display: ", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsErrorIndicator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            return text.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Exception in thread", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Could not open video stream", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("Failed to start", StringComparison.OrdinalIgnoreCase);
        }

        private void OnProcessExitedInternal(object? sender, EventArgs e)
        {
            int exitCode = -1;
            try
            {
                if (sender is Process proc && proc.HasExited)
                {
                    exitCode = proc.ExitCode;
                }
            }
            catch { }

            _exitTcs?.TrySetResult(exitCode);
            ProcessExited?.Invoke(this, exitCode);
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            await StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                StopAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
            }
            catch { }

            GC.SuppressFinalize(this);
        }
    }
}
