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
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.Platform;

namespace ScrcpyDex.Cli.Commands
{
    public static class ConfigCommand
    {
        public static async Task<int> ExecuteAsync(CliOptions options, CancellationToken ct = default)
        {
            string configDir = DefaultPlatformPaths.Instance.GetConfigDirectory();
            string configPath = !string.IsNullOrWhiteSpace(options.CustomConfigPath)
                ? options.CustomConfigPath
                : Path.Combine(configDir, "settings.json");

            var repo = new AtomicJsonConfigRepository<ScrcpyDeXSettings>(configPath);
            var settings = await repo.LoadAsync(ct).ConfigureAwait(false);

            string subcmd = options.ConfigSubcommand?.ToLowerInvariant() ?? "list";

            switch (subcmd)
            {
                case "path":
                    Console.WriteLine(configPath);
                    return 0;

                case "list":
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(settings, jsonOptions);
                    Console.WriteLine(json);
                    return 0;

                case "get":
                    if (string.IsNullOrWhiteSpace(options.ConfigKey))
                    {
                        AnsiConsole.Error("Missing key argument for 'config get <key>'.");
                        return 1;
                    }

                    string? val = GetConfigValue(settings, options.ConfigKey);
                    if (val == null)
                    {
                        AnsiConsole.Warning($"Key '{options.ConfigKey}' not found.");
                        return 1;
                    }
                    Console.WriteLine(val);
                    return 0;

                case "set":
                    if (string.IsNullOrWhiteSpace(options.ConfigKey) || options.ConfigValue == null)
                    {
                        AnsiConsole.Error("Usage: scrcpy-dex config set <key> <value>");
                        return 1;
                    }

                    bool updated = SetConfigValue(settings, options.ConfigKey, options.ConfigValue);
                    if (!updated)
                    {
                        AnsiConsole.Error($"Failed to set key '{options.ConfigKey}'. Verify the key name exists.");
                        return 1;
                    }

                    await repo.SaveAsync(settings, ct).ConfigureAwait(false);
                    AnsiConsole.Success($"Configuration updated: {options.ConfigKey} = {options.ConfigValue}");
                    return 0;

                default:
                    AnsiConsole.Error($"Unknown config command '{subcmd}'. Use: list, get, set, path.");
                    return 1;
            }
        }

        private static string? GetConfigValue(ScrcpyDeXSettings settings, string key)
        {
            var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return null;

            object? section = parts[0].ToLowerInvariant() switch
            {
                "display" => settings.Display,
                "audio" => settings.Audio,
                "input" => settings.Input,
                "device" => settings.Device,
                "paths" => settings.Paths,
                "ui" => settings.Ui,
                _ => null
            };

            if (section == null) return null;

            var prop = section.GetType().GetProperty(parts[1], BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;

            var val = prop.GetValue(section);
            return val?.ToString();
        }

        private static bool SetConfigValue(ScrcpyDeXSettings settings, string key, string value)
        {
            var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            object? section = parts[0].ToLowerInvariant() switch
            {
                "display" => settings.Display,
                "audio" => settings.Audio,
                "input" => settings.Input,
                "device" => settings.Device,
                "paths" => settings.Paths,
                "ui" => settings.Ui,
                _ => null
            };

            if (section == null) return false;

            var prop = section.GetType().GetProperty(parts[1], BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite) return false;

            try
            {
                Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                object? convertedVal;
                if (targetType == typeof(bool))
                {
                    convertedVal = bool.Parse(value);
                }
                else if (targetType == typeof(int))
                {
                    convertedVal = int.Parse(value);
                }
                else if (targetType == typeof(string))
                {
                    convertedVal = value;
                }
                else
                {
                    convertedVal = Convert.ChangeType(value, targetType);
                }

                prop.SetValue(section, convertedVal);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
