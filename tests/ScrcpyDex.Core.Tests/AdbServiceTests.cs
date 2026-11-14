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

using ScrcpyDex.Services;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class AdbServiceTests
    {
        [Fact]
        public void ParseDeXDisplayId_DisplayDeviceInfoPattern_ReturnsDisplayId()
        {
            const string dumpsysOutput = "DisplayDeviceInfo{\"ScrcpyDeX\", displayId 2, FLAG_SECURE}";

            int? displayId = AdbService.ParseDeXDisplayId(dumpsysOutput);

            Assert.Equal(2, displayId);
        }

        [Fact]
        public void ParseDeXDisplayId_DisplayBlockWithDeviceNamePattern_ReturnsDisplayId()
        {
            const string dumpsysOutput = "Display 3:\n  mName=\"ScrcpyDeX\"";

            int? displayId = AdbService.ParseDeXDisplayId(dumpsysOutput);

            Assert.Equal(3, displayId);
        }

        [Fact]
        public void ParseDeXDisplayId_ServerActivationDisplayIdTokenPattern_ReturnsDisplayId()
        {
            const string dumpsysOutput = "[Server] DEX_ACTIVATED_DISPLAY_ID=4";

            int? displayId = AdbService.ParseDeXDisplayId(dumpsysOutput);

            Assert.Equal(4, displayId);
        }

        [Fact]
        public void ParseDeXDisplayId_ServerReadyStreamingTokenPattern_ReturnsDisplayId()
        {
            const string dumpsysOutput = "[Server] ready for streaming! ID=5";

            int? displayId = AdbService.ParseDeXDisplayId(dumpsysOutput);

            Assert.Equal(5, displayId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unrelated dumpsys content without scrcpydex tokens")]
        [InlineData("Display 1:\n  mName=\"Built-in Screen\"")]
        [InlineData("DisplayDeviceInfo{\"Primary\", displayId 0}")]
        public void ParseDeXDisplayId_WithNullEmptyOrUnrelatedOutput_ReturnsNull(string? output)
        {
            int? displayId = AdbService.ParseDeXDisplayId(output!);

            Assert.Null(displayId);
        }

        [Theory]
        [InlineData("", 2, 160)]
        [InlineData("   ", 2, 160)]
        [InlineData("R5CW123456", 0, 160)]
        [InlineData("R5CW123456", -1, 160)]
        [InlineData("R5CW123456", 2, 50)]
        [InlineData("R5CW123456", 2, 800)]
        public async System.Threading.Tasks.Task SetDisplayDpiAsync_WithInvalidArguments_ReturnsFalseWithoutExecutingAdb(
            string serial, int displayId, int dpi)
        {
            var service = new AdbService();

            bool applied = await service.SetDisplayDpiAsync(serial, displayId, dpi);

            Assert.False(applied);
        }

        [Theory]
        [InlineData("", 2, 1920, 1080)]
        [InlineData("   ", 2, 1920, 1080)]
        [InlineData("R5CW123456", 0, 1920, 1080)]
        [InlineData("R5CW123456", -1, 1920, 1080)]
        [InlineData("R5CW123456", 2, 320, 1080)]
        [InlineData("R5CW123456", 2, 9000, 1080)]
        [InlineData("R5CW123456", 2, 1920, 240)]
        [InlineData("R5CW123456", 2, 1920, 5000)]
        public async System.Threading.Tasks.Task SetDisplaySizeAsync_WithInvalidArguments_ReturnsFalseWithoutExecutingAdb(
            string serial, int displayId, int width, int height)
        {
            var service = new AdbService();

            bool applied = await service.SetDisplaySizeAsync(serial, displayId, width, height);

            Assert.False(applied);
        }

        [Fact]
        public void ConfigurePaths_ResolvesSiblingAdbFromScrcpyPath()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ScrcpyDeX_AdbTest_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                string scrcpyBin = System.OperatingSystem.IsWindows() ? "scrcpy.exe" : "scrcpy";
                string adbBin = System.OperatingSystem.IsWindows() ? "adb.exe" : "adb";
                string fakeScrcpy = System.IO.Path.Combine(tempDir, scrcpyBin);
                string fakeAdb = System.IO.Path.Combine(tempDir, adbBin);

                System.IO.File.WriteAllText(fakeScrcpy, "dummy");
                System.IO.File.WriteAllText(fakeAdb, "dummy");

                var service = new AdbService();
                service.ConfigurePaths(customAdbPath: null, scrcpyPath: fakeScrcpy);

                Assert.Equal(System.IO.Path.GetFullPath(fakeAdb), service.AdbPath);
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
