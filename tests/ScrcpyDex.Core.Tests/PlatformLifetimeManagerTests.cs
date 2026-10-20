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
using System.Runtime.Versioning;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Core.Platform.Windows;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class PlatformLifetimeManagerTests
    {
        [Fact]
        public void Default_ReturnsNonNullInstance()
        {
            var manager = PlatformLifetimeManager.Default;
            Assert.NotNull(manager);
        }

        [Fact]
        public void Default_OnWindows_ReturnsWindowsJobObjectLifetimeManager()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var manager = PlatformLifetimeManager.Default;
            Assert.IsType<WindowsJobObjectLifetimeManager>(manager);
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void WindowsJobObjectLifetimeManager_AssignProcess_ThrowsOnNull()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using var manager = new WindowsJobObjectLifetimeManager();
            Assert.Throws<ArgumentNullException>(() => manager.AssignProcess(null!));
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void WindowsJobObjectLifetimeManager_AssignProcess_ThrowsAfterDispose()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var manager = new WindowsJobObjectLifetimeManager();
            manager.Dispose();

            using var proc = Process.GetCurrentProcess();
            Assert.Throws<ObjectDisposedException>(() => manager.AssignProcess(proc));
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void WindowsJobObjectLifetimeManager_CanAssignLiveProcess()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using var manager = new WindowsJobObjectLifetimeManager();
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c ping 127.0.0.1 -n 2 > nul",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);

            try
            {
                manager.AssignProcess(process);
                Assert.False(process.HasExited);
            }
            finally
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { }
            }
        }
    }
}
