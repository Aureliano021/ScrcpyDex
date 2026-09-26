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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;
using ScrcpyDex.Models;

namespace ScrcpyDex.Services
{
    public interface IAdbService
    {
        Task<DeviceInfo> DetectDeviceAsync(CancellationToken ct = default);
        Task<IReadOnlyList<DeviceInfo>> GetConnectedDevicesAsync(CancellationToken ct = default);
        Task<DeviceInfo> QueryDeviceInfoAsync(string serial, CancellationToken ct = default);
        Task<int?> FindDeXDisplayIdAsync(string serial, CancellationToken ct = default);
        Task<int?> WaitForDeXDisplayIdAsync(string serial, TimeSpan timeout, CancellationToken ct = default);
        Task<int?> DeployAndActivateDeXAsync(string serial, string? serverJarPath = null, CancellationToken ct = default);
        Task StopDeXSessionAsync(string? serial = null, CancellationToken ct = default);
        Task<AdbExecutionResult> RunCommandAsync(string arguments, CancellationToken ct = default);
        void ResetResilienceState();
    }

    /// <summary>
    /// Service for managing ADB communication, device discovery, DeX display detection, and process management.
    /// </summary>
    public class AdbService : IAdbService
    {
        private readonly string _adbPath;
        private readonly ProcessJobTracker _jobTracker;
        private readonly AdbResilienceService _resilience;
        private Process? _activatorProcess;
        private NonBlockingProcessStream? _activatorStream;

        private static readonly Lazy<AdbService> _defaultInstance = new Lazy<AdbService>(() => new AdbService());
        public static AdbService Instance => _defaultInstance.Value;

        public string AdbPath => _adbPath;

        public void ResetResilienceState() => _resilience.ResetCircuitBreaker();

        public AdbService(string? adbPath = null, ProcessJobTracker? jobTracker = null)
        {
            _jobTracker = jobTracker ?? ProcessJobTracker.Default;
            _adbPath = !string.IsNullOrWhiteSpace(adbPath)
                ? adbPath
                : ScrcpyPathResolver.ResolveAdbExecutable();

            _resilience = new AdbResilienceService(_adbPath, _jobTracker);
        }

        #region Device Detection & Properties

        /// <summary>
        /// Asynchronously detects the primary connected Android device without blocking the caller.
        /// </summary>
        public async Task<DeviceInfo> DetectDeviceAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await _resilience.ExecuteWithRetryAsync("devices -l", 2, TimeSpan.FromSeconds(5), ct)
                    .ConfigureAwait(false);

                if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Output))
                {
                    return new DeviceInfo { State = "disconnected" };
                }

                var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase)) continue;

                    var parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[1].Equals("device", StringComparison.OrdinalIgnoreCase))
                    {
                        string serial = parts[0];
                        return await QueryDeviceInfoAsync(serial, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Fallback on unexpected failure
            }

            return new DeviceInfo { State = "disconnected" };
        }

        /// <summary>
        /// Retrieves all connected devices with their full properties queried concurrently.
        /// </summary>
        public async Task<IReadOnlyList<DeviceInfo>> GetConnectedDevicesAsync(CancellationToken ct = default)
        {
            var list = new List<DeviceInfo>();

            try
            {
                var result = await _resilience.ExecuteWithRetryAsync("devices -l", 2, TimeSpan.FromSeconds(5), ct)
                    .ConfigureAwait(false);

                if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Output))
                {
                    return list;
                }

                var queryTasks = new List<Task<DeviceInfo>>();
                var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase)) continue;

                    var parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string serial = parts[0];
                        string state = parts[1];

                        if (state.Equals("device", StringComparison.OrdinalIgnoreCase))
                        {
                            queryTasks.Add(QueryDeviceInfoAsync(serial, ct));
                        }
                        else
                        {
                            list.Add(new DeviceInfo(serial, "Unknown", state));
                        }
                    }
                }

                if (queryTasks.Count > 0)
                {
                    var results = await Task.WhenAll(queryTasks).ConfigureAwait(false);
                    list.AddRange(results);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch { }

            return list;
        }

        /// <summary>
        /// Asynchronously queries phone properties (model, manufacturer, release, OneUI) in parallel.
        /// </summary>
        public async Task<DeviceInfo> QueryDeviceInfoAsync(string serial, CancellationToken ct = default)
        {
            var info = new DeviceInfo { Serial = serial, State = "device" };

            // Query ro properties and DeX display ID concurrently without blocking
            var modelTask = RunCommandAsync($"-s {serial} shell getprop ro.product.model", ct);
            var mfgTask = RunCommandAsync($"-s {serial} shell getprop ro.product.manufacturer", ct);
            var releaseTask = RunCommandAsync($"-s {serial} shell getprop ro.build.version.release", ct);
            var oneUiTask = RunCommandAsync($"-s {serial} shell getprop ro.build.version.oneui", ct);
            var dexDisplayTask = FindDeXDisplayIdAsync(serial, ct);

            try
            {
                await Task.WhenAll(modelTask, mfgTask, releaseTask, oneUiTask, dexDisplayTask).ConfigureAwait(false);

                var modelRes = await modelTask.ConfigureAwait(false);
                var mfgRes = await mfgTask.ConfigureAwait(false);
                var relRes = await releaseTask.ConfigureAwait(false);
                var oneUiRes = await oneUiTask.ConfigureAwait(false);
                var dexDisplayId = await dexDisplayTask.ConfigureAwait(false);

                if (modelRes.Succeeded && !string.IsNullOrWhiteSpace(modelRes.Output))
                    info.Model = modelRes.Output.Trim();

                if (mfgRes.Succeeded && !string.IsNullOrWhiteSpace(mfgRes.Output))
                    info.Manufacturer = mfgRes.Output.Trim();

                if (relRes.Succeeded && !string.IsNullOrWhiteSpace(relRes.Output))
                    info.AndroidVersion = relRes.Output.Trim();

                if (oneUiRes.Succeeded && !string.IsNullOrWhiteSpace(oneUiRes.Output))
                    info.OneUiVersion = oneUiRes.Output.Trim();

                if (dexDisplayId.HasValue)
                    info.DeXDisplayId = dexDisplayId.Value;
            }
            catch (Exception)
            {
                // Best effort property assignment
            }

            return info;
        }

        #endregion

        #region Samsung DeX Display Detection

        /// <summary>
        /// Queries the device for an active Samsung DeX / ScrcpyDeX virtual display ID using dumpsys display.
        /// </summary>
        public async Task<int?> FindDeXDisplayIdAsync(string serial, CancellationToken ct = default)
        {
            try
            {
                var result = await _resilience.ExecuteWithRetryAsync(
                    $"-s {serial} shell dumpsys display",
                    maxRetries: 1,
                    timeout: TimeSpan.FromSeconds(5),
                    ct: ct).ConfigureAwait(false);

                if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Output))
                {
                    return null;
                }

                return ParseDeXDisplayId(result.Output);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Polls dumpsys display until the DeX display ID is recognized or timeout expires.
        /// </summary>
        public async Task<int?> WaitForDeXDisplayIdAsync(string serial, TimeSpan timeout, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout && !ct.IsCancellationRequested)
            {
                int? displayId = await FindDeXDisplayIdAsync(serial, ct).ConfigureAwait(false);
                if (displayId.HasValue) return displayId;

                await Task.Delay(300, ct).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Robust parser for Samsung DeX display IDs extracted from Android dumpsys display output.
        /// </summary>
        public static int? ParseDeXDisplayId(string dumpsysOutput)
        {
            if (string.IsNullOrWhiteSpace(dumpsysOutput)) return null;

            // Pattern 1: DisplayInfo{"ScrcpyDeX", displayId 2, ...} or {"Samsung DeX", displayId 2}
            var match1 = Regex.Match(dumpsysOutput, @"DisplayInfo\{""(?:ScrcpyDeX|Samsung DeX|Desktop)[^""]*"",\s*displayId\s+(\d+)", RegexOptions.IgnoreCase);
            if (match1.Success && int.TryParse(match1.Groups[1].Value, out int id1))
                return id1;

            // Pattern 2: Display 2: ... mPrimaryDisplayDevice=ScrcpyDeX or name="ScrcpyDeX"
            var match2 = Regex.Match(dumpsysOutput, @"Display\s+(\d+)[\s\S]*?(?:mPrimaryDisplayDevice=ScrcpyDeX|name=""ScrcpyDeX""|name=""Samsung DeX"")", RegexOptions.IgnoreCase);
            if (match2.Success && int.TryParse(match2.Groups[1].Value, out int id2))
                return id2;

            // Pattern 3: DisplayDeviceInfo{"ScrcpyDeX"... displayId = 2 or displayId 2
            var match3 = Regex.Match(dumpsysOutput, @"DisplayDeviceInfo\{""(?:ScrcpyDeX|Samsung DeX|Desktop)""[^}]*?displayId\s*[:=]?\s*(\d+)", RegexOptions.IgnoreCase);
            if (match3.Success && int.TryParse(match3.Groups[1].Value, out int id3))
                return id3;

            // Pattern 4: mDisplayId=2 ... ScrcpyDeX
            var match4 = Regex.Match(dumpsysOutput, @"mDisplayId=(\d+)[\s\S]{1,200}?(?:ScrcpyDeX|Samsung DeX)", RegexOptions.IgnoreCase);
            if (match4.Success && int.TryParse(match4.Groups[1].Value, out int id4))
                return id4;

            // Pattern 5: Server activation standard output log tokens
            var match5 = Regex.Match(dumpsysOutput, @"(?:DEX_ACTIVATED_DISPLAY_ID|ready for streaming!\s*ID)=(\d+)", RegexOptions.IgnoreCase);
            if (match5.Success && int.TryParse(match5.Groups[1].Value, out int id5))
                return id5;

            return null;
        }

        #endregion

        #region DeX Server Deployment & Lifecycle

        /// <summary>
        /// Pushes scrcpydex-server.jar to the device, cleans previous sessions, launches activator,
        /// keeps the activator daemon alive, and returns the acquired DeX display ID.
        /// </summary>
        public async Task<int?> DeployAndActivateDeXAsync(string serial, string? serverJarPath = null, CancellationToken ct = default)
        {
            string? jar = serverJarPath ?? ScrcpyPathResolver.ResolveServerJar();
            if (string.IsNullOrEmpty(jar) || !File.Exists(jar))
            {
                throw new FileNotFoundException($"scrcpydex-server.jar not found at '{jar}'.", jar);
            }

            // Always ensure resilience circuit breaker is clear before fresh deployment
            ResetResilienceState();

            // 1. Clean up stale sessions on device
            await StopDeXSessionAsync(serial, ct).ConfigureAwait(false);
            ResetResilienceState();

            // 2. Push server jar to /data/local/tmp/scrcpydex-server.jar
            var pushResult = await _resilience.ExecuteWithRetryAsync(
                $"-s {serial} push \"{jar}\" /data/local/tmp/scrcpydex-server.jar",
                maxRetries: 2,
                timeout: TimeSpan.FromSeconds(15),
                ct: ct).ConfigureAwait(false);

            if (!pushResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to push server JAR to device: {pushResult.Error}");
            }

            // 3. Launch activator background process on device
            var psi = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = $"-s {serial} shell \"CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server activate\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            if (!proc.Start())
            {
                throw new InvalidOperationException("Failed to start ADB activator process.");
            }

            // Assign spawned process directly to Windows Job Object
            _jobTracker.AssignProcess(proc);

            var stream = new NonBlockingProcessStream(1024);
            int? detectedId = null;

            stream.EntryReceived += entry =>
            {
                if (entry.Text.Contains("DEX_ACTIVATED_DISPLAY_ID=") || entry.Text.Contains("ready for streaming! ID="))
                {
                    int? parsed = ParseDeXDisplayId(entry.Text);
                    if (parsed.HasValue) detectedId = parsed;
                }
            };
            stream.Bind(proc);

            // Keep reference so garbage collection or pipe closure doesn't kill the activator daemon
            _activatorProcess = proc;
            _activatorStream = stream;

            // 4. Poll for active display ID up to 8 seconds
            var timeout = TimeSpan.FromSeconds(8);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout && !detectedId.HasValue && !ct.IsCancellationRequested)
            {
                if (proc.HasExited)
                {
                    break;
                }
                int? dumpId = await FindDeXDisplayIdAsync(serial, ct).ConfigureAwait(false);
                if (dumpId.HasValue)
                {
                    detectedId = dumpId;
                    break;
                }
                await Task.Delay(250, ct).ConfigureAwait(false);
            }

            return detectedId;
        }

        /// <summary>
        /// Cleans up DeX server processes on the device and terminates the host activator process.
        /// Performs orderly shutdown: unregisters virtual display first, then terminates background processes and removes port forwards.
        /// </summary>
        public async Task StopDeXSessionAsync(string? serial = null, CancellationToken ct = default)
        {
            string target = string.IsNullOrWhiteSpace(serial) ? string.Empty : $"-s {serial} ";

            // Terminate host-side activator process
            try
            {
                if (_activatorProcess != null && !_activatorProcess.HasExited)
                {
                    _activatorProcess.Kill(entireProcessTree: true);
                }
            }
            catch { }
            finally
            {
                _activatorProcess?.Dispose();
                _activatorProcess = null;
            }

            if (_activatorStream != null)
            {
                try { await _activatorStream.DisposeAsync().ConfigureAwait(false); } catch { }
                _activatorStream = null;
            }

            try
            {
                // Step 1: Send clean disconnect to server jar so SemWifiDisplayConfig unregisters display cleanly
                await _resilience.ExecuteDirectAsync(
                    $"{target}shell \"CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect\"",
                    TimeSpan.FromSeconds(2),
                    ct).ConfigureAwait(false);
            }
            catch { }

            try
            {
                // Step 2: Fallback broadcast to stop desktop mode launcher and terminate remaining server processes
                await _resilience.ExecuteDirectAsync(
                    $"{target}shell \"am broadcast -a com.sec.android.app.desktoplauncher.ACTION_STOP_DESKTOP_MODE ; am broadcast -a com.samsung.android.desktopmode.action.DESKTOP_MODE_UPDATE --ez enabled false ; pkill -f com.scrcpydex.server.Server ; pkill -f scrcpy ; pkill -f DexTriggerTest\"",
                    TimeSpan.FromSeconds(2),
                    ct).ConfigureAwait(false);
            }
            catch { }

            // Step 3: Remove port forwards asynchronously without blocking
            _ = _resilience.ExecuteDirectAsync($"{target}forward --remove tcp:27183", TimeSpan.FromMilliseconds(500), ct);
            _ = _resilience.ExecuteDirectAsync($"{target}forward --remove tcp:27184", TimeSpan.FromMilliseconds(500), ct);
        }

        #endregion

        #region Command Execution

        public Task<AdbExecutionResult> RunCommandAsync(string arguments, CancellationToken ct = default)
        {
            return _resilience.ExecuteWithRetryAsync(arguments, maxRetries: 2, ct: ct);
        }

        #endregion

        #region Legacy Static Compatibility Methods

        /// <summary>
        /// Synchronous device detection wrapper for backward compatibility.
        /// </summary>
        public static DeviceInfo DetectDevice()
        {
            try
            {
                return Task.Run(() => Instance.DetectDeviceAsync()).GetAwaiter().GetResult();
            }
            catch
            {
                return new DeviceInfo { State = "disconnected" };
            }
        }

        /// <summary>
        /// Launches a DeX scrcpy session directly using ScrcpyExecutionService.
        /// </summary>
        public static Process? LaunchDeXSession(DisplayConfig config, string rootDir)
        {
            var executionService = new ScrcpyExecutionService(ProcessJobTracker.Default, Instance);
            var startTask = executionService.StartAsync(config);
            startTask.GetAwaiter().GetResult();
            return null;
        }

        /// <summary>
        /// Synchronous cleanup method for backward compatibility.
        /// </summary>
        public static void StopDeXSession(string? rootDir = null)
        {
            try
            {
                Task.Run(() => Instance.StopDeXSessionAsync()).GetAwaiter().GetResult();
            }
            catch { }
        }

        #endregion
    }
}
