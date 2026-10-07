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
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Services;

namespace ScrcpyDex.Cli.Commands
{
    public static class StopCommand
    {
        public static async Task<int> ExecuteAsync(string? serial = null, CancellationToken ct = default)
        {
            string targetText = !string.IsNullOrWhiteSpace(serial) ? $" on device {serial}" : string.Empty;
            AnsiConsole.Info($"Stopping Samsung DeX session{targetText}...");

            try
            {
                // 1. Send clean disconnect to on-device DeX server
                await AdbService.Instance.StopDeXSessionAsync(serial, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AnsiConsole.Warning($"ADB session teardown notice: {ex.Message}");
            }

            // 2. Terminate any running scrcpy client processes
            try
            {
                var scrcpyProcs = Process.GetProcessesByName("scrcpy");
                foreach (var p in scrcpyProcs)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            AnsiConsole.Success("Samsung DeX session disconnected and processes cleaned.");
            return 0;
        }
    }
}
