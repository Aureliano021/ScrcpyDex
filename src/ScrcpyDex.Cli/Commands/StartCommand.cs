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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Models;
using ScrcpyDex.Platform.Linux;
using ScrcpyDex.Services;

namespace ScrcpyDex.Cli.Commands
{
    public static class StartCommand
    {
        public static async Task<int> ExecuteAsync(CliOptions options, CancellationToken parentCt = default)
        {
            AnsiConsole.Header("=== Starting ScrcpyDeX Streaming Session ===");

            // 1. Check prerequisites
            if (!ScrcpyPathResolver.TryResolveAdbExecutable(out string adbPath))
            {
                AnsiConsole.Error("adb executable not found. Ensure Android SDK platform-tools are installed and in PATH.");
                return 1;
            }

            if (!ScrcpyPathResolver.TryResolveScrcpyExecutable(out string scrcpyPath))
            {
                AnsiConsole.Error("scrcpy executable not found. Ensure scrcpy >= 2.4.0 is installed.");
                return 1;
            }

            var scrcpyVersion = ScrcpyPathResolver.QueryScrcpyVersion(scrcpyPath);
            if (scrcpyVersion != null && scrcpyVersion < new Version(2, 4))
            {
                AnsiConsole.Warning($"Installed scrcpy version ({scrcpyVersion}) is < 2.4.0.");
                AnsiConsole.Warning("Older versions lack --mouse=uhid and --display-id flags and may not stream DeX properly.");
            }

            // 2. Select target device
            string? serial = options.Serial;
            if (string.IsNullOrWhiteSpace(serial))
            {
                var connectedDevices = await AdbService.Instance.GetConnectedDevicesAsync(parentCt).ConfigureAwait(false);
                var activeDevices = connectedDevices.Where(d => d.State == "device").ToList();

                if (activeDevices.Count == 0)
                {
                    AnsiConsole.Error("No authorized Android devices detected over USB/ADB.");
                    Console.WriteLine("  Verify your device is plugged in and USB debugging is enabled.");
                    return 1;
                }
                else if (activeDevices.Count == 1)
                {
                    serial = activeDevices[0].Serial;
                }
                else
                {
                    // Multiple devices detected
                    if (!Console.IsInputRedirected)
                    {
                        Console.WriteLine(AnsiConsole.Colorize("Multiple devices detected:", AnsiConsole.Bold));
                        for (int i = 0; i < activeDevices.Count; i++)
                        {
                            var d = activeDevices[i];
                            string tag = d.Manufacturer.Contains("Samsung", StringComparison.OrdinalIgnoreCase) ? " [Samsung]" : "";
                            Console.WriteLine($"  [{i + 1}] {d.Serial} ({d.Model}{tag})");
                        }

                        Console.Write($"Select device [1-{activeDevices.Count}]: ");
                        string? input = Console.ReadLine();
                        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= activeDevices.Count)
                        {
                            serial = activeDevices[choice - 1].Serial;
                        }
                        else
                        {
                            AnsiConsole.Warning("Invalid selection; choosing first available device.");
                            serial = activeDevices[0].Serial;
                        }
                    }
                    else
                    {
                        // Automated/scripted environment: prioritize Samsung device
                        var samsungDevice = activeDevices.FirstOrDefault(d => d.Manufacturer.Contains("Samsung", StringComparison.OrdinalIgnoreCase));
                        serial = (samsungDevice ?? activeDevices[0]).Serial;
                        AnsiConsole.Info($"Multiple devices detected in non-interactive shell. Selected: {serial}");
                    }
                }
            }

            // 3. Query device telemetry
            var devInfo = await AdbService.Instance.QueryDeviceInfoAsync(serial, parentCt).ConfigureAwait(false);
            AnsiConsole.Info($"Target: {devInfo.Model} ({devInfo.Manufacturer}), Android {devInfo.AndroidVersion}, One UI {devInfo.OneUiVersion}");

            // 4. Load configuration & apply CLI overrides
            string configDir = DefaultPlatformPaths.Instance.GetConfigDirectory();
            string configPath = !string.IsNullOrWhiteSpace(options.CustomConfigPath)
                ? options.CustomConfigPath
                : Path.Combine(configDir, "settings.json");

            var repo = new AtomicJsonConfigRepository<ScrcpyDeXSettings>(configPath);
            var settings = await repo.LoadAsync(parentCt).ConfigureAwait(false);

            ApplyCliOverrides(settings, options);

            // 5. Deploy & activate on-device Samsung DeX server
            AnsiConsole.Info("Deploying and activating Samsung DeX session on device...");
            int? displayId = null;
            try
            {
                displayId = await AdbService.Instance.DeployAndActivateDeXAsync(serial, null, parentCt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AnsiConsole.Warning($"DeX activation notice: {ex.Message}");
            }

            if (!displayId.HasValue)
            {
                displayId = await AdbService.Instance.FindDeXDisplayIdAsync(serial, parentCt).ConfigureAwait(false);
            }

            if (displayId.HasValue)
            {
                AnsiConsole.Success($"Samsung DeX virtual display allocated (Display ID: {displayId.Value}).");
            }
            else
            {
                AnsiConsole.Warning("Could not determine specific virtual display ID. Launching scrcpy with auto-detected display.");
            }

            // 6. Setup cancellation & process lifetime management
            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
            ConsoleCancelEventHandler cancelHandler = (s, e) =>
            {
                e.Cancel = true;
                AnsiConsole.Info("Interrupt signal received (SIGINT / Ctrl+C). Initiating clean session shutdown...");
                try { sessionCts.Cancel(); } catch { }
            };
            Console.CancelKeyPress += cancelHandler;

            LinuxProcessLifetimeManager.Default.ShutdownRequested += (s, e) =>
            {
                try { sessionCts.Cancel(); } catch { }
            };

            // Register cleanup action to unregister DeX session cleanly on device
            LinuxProcessLifetimeManager.Default.RegisterCleanupAction(async () =>
            {
                await AdbService.Instance.StopDeXSessionAsync(serial).ConfigureAwait(false);
            });

            // 7. Launch scrcpy streaming client
            AnsiConsole.Info("Launching scrcpy desktop client...");
            var executionService = new ScrcpyExecutionService();
            executionService.LogReceived += (s, entry) =>
            {
                Console.WriteLine($"{AnsiConsole.Colorize("[scrcpy]", AnsiConsole.Gray)} {entry.Text}");
            };
            executionService.ErrorOccurred += (s, err) =>
            {
                AnsiConsole.Error(err);
            };

            try
            {
                bool started = await executionService.StartAsync(settings, serial, displayId, sessionCts.Token).ConfigureAwait(false);
                if (!started)
                {
                    AnsiConsole.Error("Failed to start scrcpy streaming client.");
                    await CleanupSessionAsync(serial).ConfigureAwait(false);
                    return 1;
                }

                AnsiConsole.Success("Streaming active. Press Ctrl+C in this terminal or close scrcpy window to disconnect.");

                // Keep process alive while streaming
                while (executionService.IsRunning && !sessionCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(200, sessionCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
                AnsiConsole.Info("Terminating session and restoring target device display...");
                try
                {
                    await executionService.StopAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch { }

                await CleanupSessionAsync(serial).ConfigureAwait(false);
                AnsiConsole.Success("Session finished. Device display restored.");
            }

            return 0;
        }

        private static void ApplyCliOverrides(ScrcpyDeXSettings settings, CliOptions options)
        {
            if (settings.Display == null) settings.Display = new DisplaySection();
            if (settings.Input == null) settings.Input = new InputSection();
            if (settings.Device == null) settings.Device = new DeviceSection();
            if (settings.Audio == null) settings.Audio = new AudioSection();

            if (!string.IsNullOrWhiteSpace(options.Resolution))
            {
                settings.Display.Resolution = options.Resolution;
            }
            if (options.Fps.HasValue && options.Fps.Value > 0)
            {
                settings.Display.Fps = options.Fps.Value;
            }
            if (options.Bitrate.HasValue && options.Bitrate.Value > 0)
            {
                settings.Display.Bitrate = options.Bitrate.Value;
            }
            if (!string.IsNullOrWhiteSpace(options.Codec))
            {
                settings.Display.Codec = options.Codec;
            }
            if (options.StayAwake == true)
            {
                settings.Device.StayAwake = true;
            }
            if (options.TurnScreenOff == true)
            {
                settings.Device.TurnScreenOff = true;
            }
            if (options.Fullscreen == true)
            {
                settings.Display.WindowMode = "fullscreen";
            }
            if (!string.IsNullOrWhiteSpace(options.MouseDriver))
            {
                settings.Input.MouseDriver = options.MouseDriver;
            }
            if (!string.IsNullOrWhiteSpace(options.RenderDriver))
            {
                settings.Display.RenderDriver = options.RenderDriver;
            }
            if (options.AudioEnabled.HasValue)
            {
                settings.Audio.Enabled = options.AudioEnabled.Value;
            }

            // Defaults for Linux if not explicitly set
            if (OperatingSystem.IsLinux())
            {
                if (string.IsNullOrWhiteSpace(settings.Display.RenderDriver))
                {
                    settings.Display.RenderDriver = "opengl";
                }
                if (string.IsNullOrWhiteSpace(settings.Input.MouseDriver))
                {
                    settings.Input.MouseDriver = "uhid";
                }
            }
        }

        private static async Task CleanupSessionAsync(string? serial)
        {
            try
            {
                await AdbService.Instance.StopDeXSessionAsync(serial).ConfigureAwait(false);
            }
            catch { }
        }
    }
}
