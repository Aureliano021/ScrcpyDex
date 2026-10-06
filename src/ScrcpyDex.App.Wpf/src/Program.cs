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
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;
using ScrcpyDex.Models;
using ScrcpyDex.Services;

namespace ScrcpyDex
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && (args[0] == "--test" || args[0] == "--self-test"))
            {
                return RunSelfTests();
            }

            try
            {
                var app = new ScrcpyDex.WinUI.App();
                app.InitializeComponent();
                var mainWindow = new ScrcpyDex.WinUI.Views.MainWindow();
                return app.Run(mainWindow);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "ScrcpyDeX Fatal Startup Error:\n\n" + ex.ToString(),
                    "ScrcpyDeX — Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return 1;
            }
        }

        private static int RunSelfTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  ScrcpyDeX Core Systems & Engine Self-Test Suite ");
            Console.WriteLine("==================================================");

            int passed = 0;
            int failed = 0;

            void Assert(string testName, bool condition, string detail = "")
            {
                if (condition)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[PASS] ");
                    Console.ResetColor();
                    Console.WriteLine($"{testName} {detail}");
                    passed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[FAIL] ");
                    Console.ResetColor();
                    Console.WriteLine($"{testName} - FAILED {detail}");
                    failed++;
                }
            }

            // Test 1: Path Resolution
            Console.WriteLine("\n--- 1. Dynamic Path Resolution ---");
            string scrcpyPath = ScrcpyPathResolver.ResolveScrcpyExecutable();
            Assert("Scrcpy Resolution", !string.IsNullOrWhiteSpace(scrcpyPath), $"Resolved: {scrcpyPath}");

            string adbPath = ScrcpyPathResolver.ResolveAdbExecutable();
            Assert("ADB Resolution", !string.IsNullOrWhiteSpace(adbPath), $"Resolved: {adbPath}");

            string? serverJar = ScrcpyPathResolver.ResolveServerJar();
            Assert("Server JAR Resolution", !string.IsNullOrWhiteSpace(serverJar) && File.Exists(serverJar), $"Resolved: {serverJar}");

            // Test 2: Scrcpy Argument Building
            Console.WriteLine("\n--- 2. Scrcpy Argument Generation ---");
            var settings = new ScrcpyDeXSettings();
            settings.Display.Bitrate = 12000000;
            settings.Display.Fps = 60;
            settings.Display.Resolution = "1920x1080";
            settings.Display.Codec = "h265";
            settings.Audio.Enabled = true;
            settings.Audio.BufferMs = 40;
            settings.Device.TurnScreenOff = true;
            settings.Device.StayAwake = true;
            settings.Input.MouseDriver = "uhid";

            var argsList = ScrcpyExecutionService.BuildArgumentsList(settings, "R5CW123456", 2);
            string argsJoined = string.Join(" ", argsList);

            Assert("Render Driver D3D11", argsJoined.Contains("--render-driver=direct3d11"));
            Assert("UHID Mouse Injection", argsJoined.Contains("--mouse=uhid"));
            Assert("Video Bitrate", argsJoined.Contains("--video-bit-rate=12000000"));
            Assert("Max FPS", argsJoined.Contains("--max-fps=60"));
            Assert("Max Size", argsJoined.Contains("--max-size=1920"));
            Assert("Video Codec", argsJoined.Contains("--video-codec=h265"));
            Assert("Audio Buffer", argsJoined.Contains("--audio-buffer=40"));
            Assert("Turn Screen Off", argsJoined.Contains("--turn-screen-off"));
            Assert("Stay Awake", argsJoined.Contains("--stay-awake"));
            Assert("Target Device Serial", argsJoined.Contains("-s R5CW123456"));
            Assert("Target DeX Display ID", argsJoined.Contains("--display-id=2"));

            // Test 2b: Audio disabled & Fullscreen
            settings.Audio.Enabled = false;
            settings.Display.WindowMode = "fullscreen";
            var altArgs = ScrcpyExecutionService.BuildArgumentsList(settings);
            string altJoined = string.Join(" ", altArgs);
            Assert("No Audio flag", altJoined.Contains("--no-audio"));
            Assert("Fullscreen flag", altJoined.Contains("--fullscreen"));

            // Test 3: DeX Display Parser
            Console.WriteLine("\n--- 3. Samsung DeX Display Parser ---");
            string sampleDumpsys1 = "Display Devices:\n  DisplayDeviceInfo{\"ScrcpyDeX\", displayId 2, FLAG_SECURE}";
            int? parsedId1 = AdbService.ParseDeXDisplayId(sampleDumpsys1);
            Assert("Parser Pattern 1 (DisplayDeviceInfo)", parsedId1 == 2, $"Got: {parsedId1}");

            string sampleDumpsys2 = "Display 3:\n  mName=\"ScrcpyDeX\"\n  mPrimaryDisplayDevice=ScrcpyDeX";
            int? parsedId2 = AdbService.ParseDeXDisplayId(sampleDumpsys2);
            Assert("Parser Pattern 2 (Display ID Header)", parsedId2 == 3, $"Got: {parsedId2}");

            string sampleLog1 = "[Server] DEX_ACTIVATED_DISPLAY_ID=4";
            int? parsedId3 = AdbService.ParseDeXDisplayId(sampleLog1);
            Assert("Parser Pattern 3 (DEX_ACTIVATED_DISPLAY_ID)", parsedId3 == 4, $"Got: {parsedId3}");

            string sampleLog2 = "[Server] ready for streaming! ID=5";
            int? parsedId4 = AdbService.ParseDeXDisplayId(sampleLog2);
            Assert("Parser Pattern 4 (ready for streaming ID)", parsedId4 == 5, $"Got: {parsedId4}");

            // Test 4: ProcessJobTracker
            Console.WriteLine("\n--- 4. Windows Job Object Containment ---");
            try
            {
                var tracker = ProcessJobTracker.Default;
                Assert("Job Tracker Singleton", tracker != null);

                // Spawn a short-lived process and assign to job
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c echo scrcpydex_job_test",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var testProc = Process.Start(psi);
                if (testProc != null)
                {
                    tracker!.AssignProcess(testProc);
                    testProc.WaitForExit(2000);
                    Assert("AssignProcess To Job Object", true, $"Assigned PID: {testProc.Id}");
                }
            }
            catch (Exception ex)
            {
                Assert("AssignProcess To Job Object", false, ex.Message);
            }

            // Test 5: Non-Blocking Process Stream
            Console.WriteLine("\n--- 5. Non-Blocking Process Stream ---");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"echo line_stdout & echo line_stderr 1>&2\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var streamProc = Process.Start(psi);
                bool gotOut = false;
                bool gotErr = false;

                if (streamProc != null)
                {
                    var stream = new NonBlockingProcessStream(128);
                    stream.EntryReceived += entry =>
                    {
                        if (entry.Text.Contains("line_stdout")) gotOut = true;
                        if (entry.Text.Contains("line_stderr")) gotErr = true;
                    };

                    stream.Bind(streamProc);
                    streamProc.WaitForExit(2000);
                    Thread.Sleep(100);

                    Assert("Non-Blocking Stream Stdout Drain", gotOut);
                    Assert("Non-Blocking Stream Stderr Drain", gotErr);
                    stream.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Assert("Non-Blocking Stream Test", false, ex.Message);
            }

            // Test 6: ScrcpyExecutionService Instantiation
            Console.WriteLine("\n--- 6. ScrcpyExecutionService & AdbService ---");
            try
            {
                var adb = new AdbService();
                Assert("AdbService Instantiation", adb != null);

                var execService = new ScrcpyExecutionService(ProcessJobTracker.Default, adb);
                Assert("ScrcpyExecutionService Instantiation", execService != null);
                Assert("Execution Service Initial State", !execService!.IsRunning);
                Assert("Resolved Scrcpy Path", !string.IsNullOrWhiteSpace(execService!.ResolvedScrcpyPath));
            }
            catch (Exception ex)
            {
                Assert("Services Instantiation", false, ex.Message);
            }

            // Test 7: MVVM Presentation & ViewModel Layer
            Console.WriteLine("\n--- 7. MVVM Presentation & ViewModel Layer ---");
            try
            {
                var vm = new ScrcpyDex.ViewModels.MainViewModel();
                Assert("MainViewModel Instantiation", vm != null);
                Assert("Initial State IsIdle", vm!.IsIdle);
                Assert("Initial Streaming State", !vm.IsStreaming);
                Assert("DiagnosticLogs Ring Buffer", vm.DiagnosticLogs != null);
                Assert("StartDeXCommand", vm.StartDeXCommand != null);
                Assert("StopDeXCommand", vm.StopDeXCommand != null);
                Assert("EmergencyKillCommand", vm.EmergencyKillCommand != null);
                Assert("SaveSettingsCommand", vm.SaveSettingsCommand != null);
                Assert("ResetDefaultsCommand", vm.ResetDefaultsCommand != null);
                Assert("ClearLogsCommand", vm.ClearLogsCommand != null);
                Assert("Settings Resolution", !string.IsNullOrWhiteSpace(vm.Settings?.Display?.Resolution));

                // Test Diagnostics Logging
                vm.Log("Self-test diagnostic log verification", "TEST");
                Assert("DiagnosticLogs Push", vm.DiagnosticLogs!.Count > 0);

                // Test Clear Command
                vm.ClearLogsCommand!.Execute(null);
                Assert("DiagnosticLogs Clear Command", vm.DiagnosticLogs.Count == 1); // 1 because ClearLogs logs a confirmation entry
            }
            catch (Exception ex)
            {
                Assert("MainViewModel Verification", false, ex.Message);
            }

            Console.WriteLine("\n==================================================");
            Console.WriteLine($"Self-Test Results: {passed} PASSED, {failed} FAILED.");
            Console.WriteLine("==================================================");

            Environment.Exit(failed == 0 ? 0 : 1);
            return failed == 0 ? 0 : 1;
        }
    }
}
