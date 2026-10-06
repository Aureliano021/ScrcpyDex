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
using ScrcpyDex.Core.Configuration;
using ScrcpyDex.Models;
using ScrcpyDex.Services;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class ScrcpyExecutionServiceTests
    {
        [Fact]
        public void BuildArgumentsList_IncludesUhidMouseDriverByDefault()
        {
            var settings = new ScrcpyDeXSettings();

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--mouse=uhid", args);
        }

        [Fact]
        public void BuildArgumentsList_CustomMouseDriver_GeneratesExpectedArgument()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Input.MouseDriver = "aoa";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--mouse=aoa", args);
        }

        [Fact]
        public void BuildArgumentsList_RenderDriver_MatchesConfiguredDriver()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.RenderDriver = "software";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--render-driver=software", args);
        }

        [Fact]
        public void BuildArgumentsList_RenderDriver_FallsBackToOperatingSystemDefault()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.RenderDriver = null;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            string expectedDriver = OperatingSystem.IsWindows() ? "direct3d11" : "opengl";
            Assert.Contains($"--render-driver={expectedDriver}", args);
        }

        [Fact]
        public void BuildArgumentsList_WithDisplayId_IncludesDisplayIdArgument()
        {
            var settings = new ScrcpyDeXSettings();

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings, displayId: 3);

            Assert.Contains("--display-id=3", args);
        }

        [Fact]
        public void BuildArgumentsList_WithoutDisplayId_OmitsDisplayIdArgument()
        {
            var settings = new ScrcpyDeXSettings();

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings, displayId: null);

            Assert.DoesNotContain(args, a => a.StartsWith("--display-id"));
        }

        [Fact]
        public void BuildArgumentsList_IncludesVideoEncodingParameters()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.Bitrate = 12000000;
            settings.Display.Fps = 90;
            settings.Display.Width = 2560;
            settings.Display.Height = 1440;
            settings.Display.Codec = "h265";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--video-bit-rate=12000000", args);
            Assert.Contains("--max-fps=90", args);
            Assert.Contains("--max-size=2560", args);
            Assert.Contains("--video-codec=h265", args);
        }

        [Fact]
        public void BuildArgumentsList_AudioDisabled_AddsNoAudioArgument()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Audio.Enabled = false;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--no-audio", args);
        }

        [Fact]
        public void BuildArgumentsList_AudioEnabled_OmitsNoAudioArgument()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Audio.Enabled = true;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.DoesNotContain("--no-audio", args);
        }

        [Theory]
        [InlineData("fullscreen", "--fullscreen")]
        [InlineData("borderless", "--window-borderless")]
        public void BuildArgumentsList_WindowMode_GeneratesExpectedFlag(string windowMode, string expectedFlag)
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.WindowMode = windowMode;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains(expectedFlag, args);
        }

        [Fact]
        public void BuildArgumentsList_NormalWindowMode_OmitsFullscreenAndBorderlessFlags()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.WindowMode = "normal";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.DoesNotContain("--fullscreen", args);
            Assert.DoesNotContain("--window-borderless", args);
        }

        [Fact]
        public void BuildArgumentsList_WithSerial_AddsSerialParameters()
        {
            var settings = new ScrcpyDeXSettings();
            const string serial = "DEVICE_SERIAL_12345";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings, serial: serial);

            int flagIndex = args.IndexOf("-s");
            Assert.True(flagIndex >= 0, "Arguments must contain '-s' flag");
            Assert.True(flagIndex + 1 < args.Count, "Serial must follow '-s' flag");
            Assert.Equal(serial, args[flagIndex + 1]);
        }

        [Fact]
        public void BuildArgumentsList_WithoutSerial_OmitsSerialParameters()
        {
            var settings = new ScrcpyDeXSettings();

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings, serial: null);

            Assert.DoesNotContain("-s", args);
        }

        [Fact]
        public void MapConfigToSettings_MapsDisplayConfigFieldsCorrectly()
        {
            var config = new DisplayConfig
            {
                Resolution = "2560x1440",
                Width = 2560,
                Height = 1440,
                Dpi = 240,
                Fps = 120,
                Bitrate = 16000000,
                Codec = "h265",
                WindowMode = "borderless",
                RenderDriver = "direct3d11",
                AudioEnabled = false,
                MouseDriver = "uhid",
                ReleaseKey = "RightControl",
                TurnScreenOff = true,
                StayAwake = true
            };

            ScrcpyDeXSettings settings = ScrcpyExecutionService.MapConfigToSettings(config);

            Assert.NotNull(settings);
            Assert.NotNull(settings.Display);
            Assert.Equal("2560x1440", settings.Display.Resolution);
            Assert.Equal(2560, settings.Display.Width);
            Assert.Equal(1440, settings.Display.Height);
            Assert.Equal(240, settings.Display.Dpi);
            Assert.Equal(120, settings.Display.Fps);
            Assert.Equal(16000000, settings.Display.Bitrate);
            Assert.Equal("h265", settings.Display.Codec);
            Assert.Equal("borderless", settings.Display.WindowMode);
            Assert.Equal("direct3d11", settings.Display.RenderDriver);

            Assert.NotNull(settings.Audio);
            Assert.False(settings.Audio.Enabled);

            Assert.NotNull(settings.Input);
            Assert.Equal("uhid", settings.Input.MouseDriver);
            Assert.Equal("RightControl", settings.Input.ReleaseKey);

            Assert.NotNull(settings.Device);
            Assert.True(settings.Device.TurnScreenOff);
            Assert.True(settings.Device.StayAwake);
        }
    }
}
