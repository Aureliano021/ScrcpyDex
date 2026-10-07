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
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Models;
using ScrcpyDex.Services;

namespace ScrcpyDex.Cli.Commands
{
    public static class DevicesCommand
    {
        public static async Task<int> ExecuteAsync(CancellationToken ct = default)
        {
            AnsiConsole.Info("Scanning for connected Android devices via ADB...");

            IReadOnlyList<DeviceInfo> devices;
            try
            {
                devices = await AdbService.Instance.GetConnectedDevicesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AnsiConsole.Error($"Failed to enumerate ADB devices: {ex.Message}");
                return 1;
            }

            if (devices.Count == 0)
            {
                AnsiConsole.Warning("No connected Android devices detected.");
                Console.WriteLine("  Ensure USB debugging is enabled on your phone and the USB cable is securely connected.");
                return 0;
            }

            Console.WriteLine();
            var headers = new[] { "SERIAL", "STATE", "MANUFACTURER", "MODEL", "ONE UI", "DEX DISPLAY", "COMPATIBILITY" };
            var rows = new List<string[]>();

            foreach (var d in devices)
            {
                bool isSamsung = d.Manufacturer.Contains("Samsung", StringComparison.OrdinalIgnoreCase);
                bool isAuthorized = string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase);
                bool isUnauthorized = string.Equals(d.State, "unauthorized", StringComparison.OrdinalIgnoreCase);

                string stateDisplay = isAuthorized
                    ? AnsiConsole.Colorize("authorized", AnsiConsole.Green)
                    : (isUnauthorized ? AnsiConsole.Colorize("unauthorized", AnsiConsole.Red) : d.State);

                string compatDisplay;
                if (isUnauthorized)
                {
                    compatDisplay = AnsiConsole.Colorize("[UNAUTHORIZED - Check device prompt]", AnsiConsole.Red);
                }
                else if (isSamsung)
                {
                    compatDisplay = AnsiConsole.Colorize("[Samsung DeX Compatible]", AnsiConsole.Green);
                }
                else
                {
                    compatDisplay = AnsiConsole.Colorize("[Generic Android]", AnsiConsole.Gray);
                }

                string displayId = d.DeXDisplayId > 0 ? d.DeXDisplayId.ToString() : "-";

                rows.Add(new[]
                {
                    d.Serial,
                    stateDisplay,
                    string.IsNullOrWhiteSpace(d.Manufacturer) ? "-" : d.Manufacturer,
                    string.IsNullOrWhiteSpace(d.Model) ? "-" : d.Model,
                    string.IsNullOrWhiteSpace(d.OneUiVersion) ? "-" : d.OneUiVersion,
                    displayId,
                    compatDisplay
                });
            }

            AnsiConsole.PrintTable(headers, rows);
            Console.WriteLine();
            AnsiConsole.Info($"Total devices detected: {devices.Count}");
            return 0;
        }
    }
}
