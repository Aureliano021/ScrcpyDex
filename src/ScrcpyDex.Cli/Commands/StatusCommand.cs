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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Models;
using ScrcpyDex.Services;

namespace ScrcpyDex.Cli.Commands
{
    public static class StatusCommand
    {
        public static async Task<int> ExecuteAsync(string? specificSerial = null, CancellationToken ct = default)
        {
            AnsiConsole.Header("=== ScrcpyDeX Session & Device Status ===");
            Console.WriteLine();

            // 1. Device status
            DeviceInfo device;
            try
            {
                if (!string.IsNullOrWhiteSpace(specificSerial))
                {
                    device = await AdbService.Instance.QueryDeviceInfoAsync(specificSerial, ct).ConfigureAwait(false);
                }
                else
                {
                    device = await AdbService.Instance.DetectDeviceAsync(ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.Error($"Failed to query device status: {ex.Message}");
                return 1;
            }

            Console.WriteLine(AnsiConsole.Colorize("Device Connection:", AnsiConsole.Bold));
            if (string.IsNullOrWhiteSpace(device.Serial) || device.State == "disconnected")
            {
                Console.WriteLine($"  State:         {AnsiConsole.Colorize("Disconnected", AnsiConsole.Yellow)}");
                Console.WriteLine("  Notice:        No target phone currently detected over ADB.");
            }
            else
            {
                string stateColor = device.IsConnected ? AnsiConsole.Green : AnsiConsole.Yellow;
                Console.WriteLine($"  Serial:        {device.Serial}");
                Console.WriteLine($"  Model:         {device.Model} ({device.Manufacturer})");
                Console.WriteLine($"  Android:       {device.AndroidVersion}");
                Console.WriteLine($"  One UI:        {device.OneUiVersion}");
                Console.WriteLine($"  State:         {AnsiConsole.Colorize(device.State, stateColor)}");

                int? displayId = await AdbService.Instance.FindDeXDisplayIdAsync(device.Serial, ct).ConfigureAwait(false);
                if (displayId.HasValue)
                {
                    Console.WriteLine($"  DeX Display:   {AnsiConsole.Colorize($"Active (Display ID: {displayId.Value})", AnsiConsole.Green)}");
                }
                else
                {
                    Console.WriteLine($"  DeX Display:   {AnsiConsole.Colorize("Inactive (No virtual display allocated)", AnsiConsole.Gray)}");
                }
            }

            Console.WriteLine();

            // 2. Host streaming process status
            Console.WriteLine(AnsiConsole.Colorize("Host Streaming Subsystem:", AnsiConsole.Bold));
            var runningScrcpy = Process.GetProcessesByName("scrcpy");
            if (runningScrcpy.Length > 0)
            {
                var p = runningScrcpy[0];
                Console.WriteLine($"  scrcpy Client: {AnsiConsole.Colorize($"Active (PID {p.Id})", AnsiConsole.Green)}");
            }
            else
            {
                Console.WriteLine($"  scrcpy Client: {AnsiConsole.Colorize("Idle (No process active)", AnsiConsole.Gray)}");
            }

            Console.WriteLine();

            // 3. Active configuration summary
            Console.WriteLine(AnsiConsole.Colorize("Default Profile Configuration:", AnsiConsole.Bold));
            string configDir = DefaultPlatformPaths.Instance.GetConfigDirectory();
            string configPath = Path.Combine(configDir, "settings.json");
            var repo = new AtomicJsonConfigRepository<ScrcpyDeXSettings>(configPath);
            var settings = await repo.LoadAsync(ct).ConfigureAwait(false);

            Console.WriteLine($"  Resolution:    {settings.Display?.Resolution ?? "1920x1080"}");
            Console.WriteLine($"  Framerate:     {settings.Display?.Fps ?? 30} fps");
            Console.WriteLine($"  Bitrate:       {(settings.Display?.Bitrate ?? 8000000) / 1000000.0:F1} Mbps");
            Console.WriteLine($"  Video Codec:   {settings.Display?.Codec ?? "h264"}");
            Console.WriteLine($"  Render Driver: {settings.Display?.RenderDriver ?? (OperatingSystem.IsWindows() ? "direct3d11" : "opengl")}");
            Console.WriteLine($"  Mouse Driver:  {settings.Input?.MouseDriver ?? "uhid"}");
            Console.WriteLine($"  Audio Tunnel:  {(settings.Audio?.Enabled == false ? "Disabled" : "Enabled")}");

            return 0;
        }
    }
}
