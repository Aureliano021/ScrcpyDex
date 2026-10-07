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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Cli.Commands;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Platform.Linux;

namespace ScrcpyDex.Cli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Configure Linux lifetime manager if executing on Linux or POSIX systems
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                PlatformLifetimeManager.Default = LinuxProcessLifetimeManager.Default;
            }

            var options = CliOptions.Parse(args);

            if (options.VersionRequested || options.Command == CliCommandType.Version)
            {
                PrintVersion();
                return 0;
            }

            if (options.HelpRequested || options.Command == CliCommandType.Help)
            {
                PrintHelp();
                return 0;
            }

            using var cts = new CancellationTokenSource();

            try
            {
                return options.Command switch
                {
                    CliCommandType.Start => await StartCommand.ExecuteAsync(options, cts.Token),
                    CliCommandType.Stop => await StopCommand.ExecuteAsync(options.Serial, cts.Token),
                    CliCommandType.Devices => await DevicesCommand.ExecuteAsync(cts.Token),
                    CliCommandType.Status => await StatusCommand.ExecuteAsync(options.Serial, cts.Token),
                    CliCommandType.Doctor => DoctorCommand.Execute(),
                    CliCommandType.Config => await ConfigCommand.ExecuteAsync(options, cts.Token),
                    CliCommandType.Test => await TestCommand.ExecuteAsync(),
                    _ => HandleUnknownCommand(options)
                };
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.Info("Operation canceled.");
                return 130;
            }
            catch (Exception ex)
            {
                AnsiConsole.Error($"Unhandled execution failure: {ex.Message}");
                if (Environment.GetEnvironmentVariable("SCRCPYDEX_DEBUG") == "1")
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        private static int HandleUnknownCommand(CliOptions options)
        {
            if (options.UnrecognizedArgs.Count > 0)
            {
                AnsiConsole.Error($"Unrecognized argument or command: '{string.Join(" ", options.UnrecognizedArgs)}'");
            }
            else
            {
                AnsiConsole.Error("No valid command provided.");
            }
            Console.WriteLine("Run 'scrcpy-dex --help' for command reference.");
            return 1;
        }

        private static void PrintVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
            Console.WriteLine($"scrcpy-dex version {version}");
        }

        private static void PrintHelp()
        {
            Console.WriteLine(AnsiConsole.Colorize("ScrcpyDeX Command Line Interface", AnsiConsole.Bold + AnsiConsole.Cyan));
            Console.WriteLine("Seamless Samsung DeX desktop mode client using scrcpy.\n");

            Console.WriteLine(AnsiConsole.Colorize("USAGE:", AnsiConsole.Bold));
            Console.WriteLine("  scrcpy-dex <command> [options]\n");

            Console.WriteLine(AnsiConsole.Colorize("COMMANDS:", AnsiConsole.Bold));
            Console.WriteLine("  start              Activate DeX display on device and initiate scrcpy streaming");
            Console.WriteLine("  stop               Disconnect active DeX session on device and terminate streaming");
            Console.WriteLine("  devices, list      List connected Android devices and report compatibility");
            Console.WriteLine("  status             Show active session, device state, and configuration profile");
            Console.WriteLine("  doctor, check      Run environment diagnostics and verify system prerequisites");
            Console.WriteLine("  config             View or modify configuration parameters in settings.json\n");

            Console.WriteLine(AnsiConsole.Colorize("OPTIONS (for 'start' command):", AnsiConsole.Bold));
            Console.WriteLine("  -s, --serial <ID>       Specify target device serial number");
            Console.WriteLine("  --resolution <WxH>      Custom display resolution (e.g., 1920x1080, 2560x1440)");
            Console.WriteLine("  --fps <FPS>             Maximum video frame rate (e.g., 60, 120)");
            Console.WriteLine("  --bitrate <RATE>        Video encoding bit rate (e.g., 8M, 12000000)");
            Console.WriteLine("  --codec <CODEC>         Video codec: h264 or h265 (default: h264)");
            Console.WriteLine("  --mouse <DRIVER>        Mouse input driver: uhid (default) or sdk");
            Console.WriteLine("  --render-driver <NAME>  Rendering backend: opengl (Linux default) or direct3d11");
            Console.WriteLine("  --stay-awake            Keep mobile device awake during active session");
            Console.WriteLine("  --turn-screen-off       Turn mobile physical display off while streaming");
            Console.WriteLine("  --fullscreen            Open scrcpy window in fullscreen mode");
            Console.WriteLine("  --no-audio              Disable device audio tunneling");
            Console.WriteLine("  --config <PATH>         Path to custom settings.json configuration file\n");

            Console.WriteLine(AnsiConsole.Colorize("CONFIG SUBCOMMANDS:", AnsiConsole.Bold));
            Console.WriteLine("  scrcpy-dex config [list]         Display all configuration keys in JSON format");
            Console.WriteLine("  scrcpy-dex config get <KEY>      Retrieve value for a configuration key (e.g. display.bitrate)");
            Console.WriteLine("  scrcpy-dex config set <KEY> <V>  Update configuration key to specified value");
            Console.WriteLine("  scrcpy-dex config path           Show path to active settings.json file\n");

            Console.WriteLine(AnsiConsole.Colorize("GLOBAL FLAGS:", AnsiConsole.Bold));
            Console.WriteLine("  -h, --help              Show this help reference");
            Console.WriteLine("  -v, --version           Display application version\n");

            Console.WriteLine(AnsiConsole.Colorize("EXAMPLES:", AnsiConsole.Bold));
            Console.WriteLine("  scrcpy-dex doctor");
            Console.WriteLine("  scrcpy-dex devices");
            Console.WriteLine("  scrcpy-dex start");
            Console.WriteLine("  scrcpy-dex start -s <DEVICE_SERIAL> --resolution 1920x1080 --fps 60");
            Console.WriteLine("  scrcpy-dex start --fullscreen --turn-screen-off");
            Console.WriteLine("  scrcpy-dex stop");
            Console.WriteLine("  scrcpy-dex config set display.bitrate 12000000");
        }
    }
}
