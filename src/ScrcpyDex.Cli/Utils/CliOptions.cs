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

namespace ScrcpyDex.Cli.Utils
{
    public enum CliCommandType
    {
        None,
        Start,
        Stop,
        Devices,
        Status,
        Doctor,
        Config,
        Help,
        Version,
        Test
    }

    public sealed class CliOptions
    {
        public CliCommandType Command { get; set; } = CliCommandType.None;
        public string? Serial { get; set; }
        public string? Resolution { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Dpi { get; set; }
        public int? Fps { get; set; }
        public int? Bitrate { get; set; }
        public string? Codec { get; set; }
        public bool? StayAwake { get; set; }
        public bool? TurnScreenOff { get; set; }
        public bool? Fullscreen { get; set; }
        public string? MouseDriver { get; set; }
        public string? RenderDriver { get; set; }
        public bool? AudioEnabled { get; set; }
        public string? CustomConfigPath { get; set; }

        // Config subcommand details
        public string? ConfigSubcommand { get; set; }
        public string? ConfigKey { get; set; }
        public string? ConfigValue { get; set; }

        public bool HelpRequested { get; set; }
        public bool VersionRequested { get; set; }
        public List<string> UnrecognizedArgs { get; } = new();

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();

            if (args == null || args.Length == 0)
            {
                options.Command = CliCommandType.Help;
                return options;
            }

            int index = 0;
            string firstArg = args[0].ToLowerInvariant();

            // Check for help/version flags as first arg
            if (firstArg == "--help" || firstArg == "-h" || firstArg == "help")
            {
                options.Command = CliCommandType.Help;
                options.HelpRequested = true;
                return options;
            }

            if (firstArg == "--version" || firstArg == "-v" || firstArg == "version")
            {
                options.Command = CliCommandType.Version;
                options.VersionRequested = true;
                return options;
            }

            // Subcommand parsing
            switch (firstArg)
            {
                case "start":
                    options.Command = CliCommandType.Start;
                    index++;
                    break;
                case "stop":
                    options.Command = CliCommandType.Stop;
                    index++;
                    break;
                case "devices":
                case "list":
                    options.Command = CliCommandType.Devices;
                    index++;
                    break;
                case "status":
                    options.Command = CliCommandType.Status;
                    index++;
                    break;
                case "doctor":
                case "check":
                    options.Command = CliCommandType.Doctor;
                    index++;
                    break;
                case "test":
                case "--test":
                    options.Command = CliCommandType.Test;
                    index++;
                    break;
                case "config":
                    options.Command = CliCommandType.Config;
                    index++;
                    // Read config specific sub-arguments
                    if (index < args.Length && !args[index].StartsWith("-"))
                    {
                        options.ConfigSubcommand = args[index].ToLowerInvariant();
                        index++;

                        if (index < args.Length && !args[index].StartsWith("-"))
                        {
                            options.ConfigKey = args[index];
                            index++;

                            if (index < args.Length && !args[index].StartsWith("-"))
                            {
                                options.ConfigValue = args[index];
                                index++;
                            }
                        }
                    }
                    else
                    {
                        options.ConfigSubcommand = "list";
                    }
                    break;
                default:
                    // If no explicit command, check if user provided flags
                    if (firstArg.StartsWith("-"))
                    {
                        options.Command = CliCommandType.Start;
                    }
                    else
                    {
                        options.UnrecognizedArgs.Add(firstArg);
                        index++;
                    }
                    break;
            }

            // Flag parsing
            while (index < args.Length)
            {
                string rawArg = args[index];
                string opt = rawArg;
                string? inlineVal = null;
                int eqIdx = rawArg.IndexOf('=');
                if (eqIdx > 0 && rawArg.StartsWith("-"))
                {
                    opt = rawArg.Substring(0, eqIdx);
                    inlineVal = rawArg.Substring(eqIdx + 1);
                }

                string? GetVal()
                {
                    if (inlineVal != null) return inlineVal;
                    if (index + 1 < args.Length) return args[++index];
                    return null;
                }

                switch (opt)
                {
                    case "-h":
                    case "--help":
                        options.HelpRequested = true;
                        break;
                    case "-v":
                    case "--version":
                        options.VersionRequested = true;
                        break;
                    case "-s":
                    case "--serial":
                        options.Serial = GetVal();
                        break;
                    case "--resolution":
                    case "--max-size":
                        options.Resolution = GetVal();
                        break;
                    case "--width":
                        if (int.TryParse(GetVal(), out int width) && width > 0)
                            options.Width = width;
                        break;
                    case "--height":
                        if (int.TryParse(GetVal(), out int height) && height > 0)
                            options.Height = height;
                        break;
                    case "--dpi":
                        if (int.TryParse(GetVal(), out int dpi) && dpi > 0)
                            options.Dpi = dpi;
                        break;
                    case "--fps":
                    case "--max-fps":
                        if (int.TryParse(GetVal(), out int fps))
                            options.Fps = fps;
                        break;
                    case "--bitrate":
                        options.Bitrate = ParseBitrate(GetVal());
                        break;
                    case "--codec":
                        options.Codec = GetVal()?.ToLowerInvariant();
                        break;
                    case "--stay-awake":
                        options.StayAwake = true;
                        break;
                    case "--turn-screen-off":
                        options.TurnScreenOff = true;
                        break;
                    case "--fullscreen":
                        options.Fullscreen = true;
                        break;
                    case "--mouse":
                        options.MouseDriver = GetVal();
                        break;
                    case "--touch":
                    case "--touchscreen":
                        options.MouseDriver = "sdk";
                        break;
                    case "--render-driver":
                        options.RenderDriver = GetVal();
                        break;
                    case "--no-audio":
                        options.AudioEnabled = false;
                        break;
                    case "--config":
                    case "--config-path":
                        options.CustomConfigPath = GetVal();
                        break;
                    default:
                        options.UnrecognizedArgs.Add(rawArg);
                        break;
                }

                index++;
            }

            return options;
        }

        private static int? ParseBitrate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string cleaned = raw.Trim().ToLowerInvariant();
            if (cleaned.EndsWith("m"))
            {
                if (double.TryParse(cleaned[..^1], out double megabits))
                    return (int)(megabits * 1_000_000);
            }
            if (cleaned.EndsWith("k"))
            {
                if (double.TryParse(cleaned[..^1], out double kilobits))
                    return (int)(kilobits * 1_000);
            }
            if (int.TryParse(cleaned, out int directVal))
            {
                return directVal;
            }

            return null;
        }
    }
}
