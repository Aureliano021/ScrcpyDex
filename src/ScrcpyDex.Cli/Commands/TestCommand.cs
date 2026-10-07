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
using System.Threading.Tasks;
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Platform.Linux;

namespace ScrcpyDex.Cli.Commands
{
    public static class TestCommand
    {
        public static async Task<int> ExecuteAsync()
        {
            AnsiConsole.Header("=== Running ScrcpyDeX CLI & Linux Platform Self-Tests ===");
            int passed = 0;
            int failed = 0;

            void RunTest(string testName, Action testAction)
            {
                try
                {
                    testAction();
                    AnsiConsole.Success($"Test '{testName}' passed.");
                    passed++;
                }
                catch (Exception ex)
                {
                    AnsiConsole.Error($"Test '{testName}' failed: {ex.Message}");
                    failed++;
                }
            }

            async Task RunTestAsync(string testName, Func<Task> testAction)
            {
                try
                {
                    await testAction().ConfigureAwait(false);
                    AnsiConsole.Success($"Test '{testName}' passed.");
                    passed++;
                }
                catch (Exception ex)
                {
                    AnsiConsole.Error($"Test '{testName}' failed: {ex.Message}");
                    failed++;
                }
            }

            // 1. Test CLI Option Parsing - Start Command Flags
            RunTest("CliOptions.Parse Start Command Flags", () =>
            {
                string[] args =
                {
                    "start",
                    "-s", "<DEVICE_SERIAL>",
                    "--resolution", "1920x1080",
                    "--fps", "60",
                    "--bitrate", "8M",
                    "--codec", "h264",
                    "--stay-awake",
                    "--turn-screen-off",
                    "--fullscreen",
                    "--mouse", "uhid",
                    "--render-driver", "opengl"
                };

                var opts = CliOptions.Parse(args);
                Assert(opts.Command == CliCommandType.Start, "Command should be Start");
                Assert(opts.Serial == "<DEVICE_SERIAL>", "Serial should match placeholder");
                Assert(opts.Resolution == "1920x1080", "Resolution should be 1920x1080");
                Assert(opts.Fps == 60, "Fps should be 60");
                Assert(opts.Bitrate == 8_000_000, "Bitrate should be 8000000");
                Assert(opts.Codec == "h264", "Codec should be h264");
                Assert(opts.StayAwake == true, "StayAwake should be true");
                Assert(opts.TurnScreenOff == true, "TurnScreenOff should be true");
                Assert(opts.Fullscreen == true, "Fullscreen should be true");
                Assert(opts.MouseDriver == "uhid", "MouseDriver should be uhid");
                Assert(opts.RenderDriver == "opengl", "RenderDriver should be opengl");
            });

            // 2. Test CLI Option Parsing - Bitrate units
            RunTest("CliOptions.Parse Bitrate Parsing Units", () =>
            {
                var opts1 = CliOptions.Parse(new[] { "start", "--bitrate", "12M" });
                Assert(opts1.Bitrate == 12_000_000, "12M parsed as 12000000");

                var opts2 = CliOptions.Parse(new[] { "start", "--bitrate", "500k" });
                Assert(opts2.Bitrate == 500_000, "500k parsed as 500000");

                var opts3 = CliOptions.Parse(new[] { "start", "--bitrate", "4000000" });
                Assert(opts3.Bitrate == 4_000_000, "4000000 parsed directly");
            });

            // 3. Test CLI Option Parsing - Config Subcommands
            RunTest("CliOptions.Parse Config Subcommand", () =>
            {
                var opts = CliOptions.Parse(new[] { "config", "set", "display.bitrate", "12000000" });
                Assert(opts.Command == CliCommandType.Config, "Command should be Config");
                Assert(opts.ConfigSubcommand == "set", "ConfigSubcommand should be set");
                Assert(opts.ConfigKey == "display.bitrate", "ConfigKey should be display.bitrate");
                Assert(opts.ConfigValue == "12000000", "ConfigValue should be 12000000");
            });

            // 4. Test LinuxSetupChecker Diagnostic Audits
            RunTest("LinuxSetupChecker System Audits", () =>
            {
                var report = LinuxSetupChecker.Instance.RunAllChecks();
                Assert(report != null, "Report should not be null");
                Assert(report!.Items.Count >= 7, $"Report should have >= 7 items, found {report.Items.Count}");

                foreach (var item in report.Items)
                {
                    Assert(!string.IsNullOrWhiteSpace(item.Name), "Item name should not be empty");
                    Assert(!string.IsNullOrWhiteSpace(item.Summary), "Item summary should not be empty");
                }
            });

            // 5. Test LinuxProcessLifetimeManager
            RunTest("LinuxProcessLifetimeManager Allocation & Teardown", () =>
            {
                using var mgr = new LinuxProcessLifetimeManager();
                bool cleanupRan = false;
                mgr.RegisterCleanupAction(() => cleanupRan = true);

                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                    Arguments = OperatingSystem.IsWindows() ? "/c exit 0" : "-c exit 0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    mgr.AssignProcess(proc);
                    proc.WaitForExit(1000);
                }

                mgr.TerminateAllProcesses(gracePeriodMs: 200);
                Assert(cleanupRan, "Cleanup action should have executed");
            });

            // 6. Test Atomic JSON Config Repository
            await RunTestAsync("AtomicJsonConfigRepository Save & Load Cycle", async () =>
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"scrcpydex_test_{Guid.NewGuid():N}.json");
                try
                {
                    var repo = new AtomicJsonConfigRepository<ScrcpyDeXSettings>(tempFile);
                    var settings = await repo.LoadAsync().ConfigureAwait(false);
                    Assert(settings != null, "Settings loaded");
                    settings!.Display.Bitrate = 9999999;
                    settings.Display.Codec = "h264";
                    await repo.SaveAsync(settings).ConfigureAwait(false);

                    var loaded = await repo.LoadAsync().ConfigureAwait(false);
                    Assert(loaded.Display.Bitrate == 9999999, "Bitrate persisted correctly");
                    Assert(loaded.Display.Codec == "h264", "Codec persisted correctly");
                }
                finally
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                    try { if (File.Exists(tempFile + ".bak")) File.Delete(tempFile + ".bak"); } catch { }
                    try { if (File.Exists(tempFile + ".tmp")) File.Delete(tempFile + ".tmp"); } catch { }
                }
            });

            Console.WriteLine();
            string finalSummary = $"Self-Test Results: {passed} passed, {failed} failed.";
            if (failed == 0)
            {
                AnsiConsole.Success(finalSummary);
                return 0;
            }
            else
            {
                AnsiConsole.Error(finalSummary);
                return 1;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Assertion failed: {message}");
            }
        }
    }
}
