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

            if (OperatingSystem.IsWindows())
            {
                Assert.DoesNotContain(args, a => a.StartsWith("--render-driver"));
            }
            else
            {
                Assert.Contains("--render-driver=opengl", args);
            }
        }

        [Theory]
        [InlineData("Direct3D 9", "direct3d")]
        [InlineData("direct3d9", "direct3d")]
        [InlineData("d3d9", "direct3d")]
        [InlineData("direct3d", "direct3d")]
        [InlineData("Direct3D 11", "direct3d11")]
        [InlineData("d3d11", "direct3d11")]
        [InlineData("direct3d11", "direct3d11")]
        [InlineData("OpenGL", "opengl")]
        [InlineData("Software", "software")]
        public void BuildArgumentsList_RenderDriver_Aliases_NormalizeCorrectly(string configuredDriver, string expectedWindowsDriver)
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.RenderDriver = configuredDriver;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            if (OperatingSystem.IsWindows())
            {
                Assert.Contains($"--render-driver={expectedWindowsDriver}", args);
            }
            else
            {
                string expectedPosix = (expectedWindowsDriver == "direct3d" || expectedWindowsDriver == "direct3d11")
                    ? "opengl"
                    : expectedWindowsDriver;
                Assert.Contains($"--render-driver={expectedPosix}", args);
            }
        }

        [Fact]
        public void BuildArgumentsList_RenderDriver_Auto_HandlesOperatingSystemNegotiation()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.RenderDriver = "auto";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            if (OperatingSystem.IsWindows())
            {
                Assert.DoesNotContain(args, a => a.StartsWith("--render-driver"));
            }
            else
            {
                Assert.Contains("--render-driver=opengl", args);
            }
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

        [Fact]
        public void BuildArgumentsList_WithRecordFile_IncludesRecordArgument()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.RecordFile = @"C:\Records\session.mp4";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains(@"--record=C:\Records\session.mp4", args);
        }

        [Fact]
        public void BuildArgumentsList_WithForwardShortcuts_IncludesUhidKeyboard()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Input.ForwardShortcuts = true;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--keyboard=uhid", args);
        }

        [Fact]
        public void BuildArgumentsList_WithLabeledResolution_ParsesMaxSizeCorrectly()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.Resolution = "2560x1080 (Ultrawide DeX)";
            settings.Display.Width = null;
            settings.Display.Height = null;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--max-size=2560", args);
        }

        [Theory]
        [InlineData("1600x900 (HD+)", 1600)]
        [InlineData("1920x1080 (FHD)", 1920)]
        [InlineData("2560x1080 (Ultrawide DeX)", 2560)]
        [InlineData("2560x1440 (QHD)", 2560)]
        [InlineData("3440x1440 (UWQHD 34\")", 3440)]
        [InlineData("3840x2160 (4K UHD)", 3840)]
        public void BuildArgumentsList_ResolutionPreset_OverridesDefault1920x1080Dimensions(string preset, int expectedMaxSize)
        {
            var settings = new ScrcpyDeXSettings();
            // Simulate legacy settings.json where Width=1920 and Height=1080 were persisted alongside a non-1080p preset
            settings.Display.Resolution = preset;
            settings.Display.Width = 1920;
            settings.Display.Height = 1080;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains($"--max-size={expectedMaxSize}", args);
        }

        [Theory]
        [InlineData("Native Screen Resolution")]
        [InlineData("native")]
        public void BuildArgumentsList_NativeResolution_OmitsMaxSizeArgument(string nativePreset)
        {
            var settings = new ScrcpyDeXSettings();
            settings.Display.Resolution = nativePreset;

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.DoesNotContain(args, a => a.StartsWith("--max-size"));
        }

        [Fact]
        public void DisplayConfig_SettingResolutionPreset_UpdatesWidthAndHeight()
        {
            var config = new DisplayConfig
            {
                Resolution = "2560x1440 (QHD)"
            };

            Assert.Equal(2560, config.Width);
            Assert.Equal(1440, config.Height);

            var mapped = ScrcpyExecutionService.MapConfigToSettings(config);
            List<string> args = ScrcpyExecutionService.BuildArgumentsList(mapped);
            Assert.Contains("--max-size=2560", args);
        }

        [Theory]
        [InlineData("UHID (Relative Mouse)", "uhid")]
        [InlineData("uhid", "uhid")]
        [InlineData("SDK (Touchscreen / Absolute)", "sdk")]
        [InlineData("touch", "sdk")]
        [InlineData("touchscreen", "sdk")]
        [InlineData("AOA (Physical USB)", "aoa")]
        [InlineData("Disabled", "disabled")]
        [InlineData("none", "disabled")]
        public void NormalizeMouseDriver_MapsLabelsAndAliasesCorrectly(string rawDriver, string expected)
        {
            string normalized = ScrcpyExecutionService.NormalizeMouseDriver(rawDriver);

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void BuildArgumentsList_TouchscreenMouseDriver_IncludesSdkMouseFlag()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Input.MouseDriver = "SDK (Touchscreen / Absolute)";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--mouse=sdk", args);
        }

        [Fact]
        public void BuildArgumentsList_DisabledMouseDriver_IncludesNoMouseFlag()
        {
            var settings = new ScrcpyDeXSettings();
            settings.Input.MouseDriver = "Disabled";

            List<string> args = ScrcpyExecutionService.BuildArgumentsList(settings);

            Assert.Contains("--no-mouse", args);
            Assert.DoesNotContain(args, a => a.StartsWith("--mouse="));
        }

        [Fact]
        public void TryResolveDisplayDimensions_CustomResolution_UsesExplicitDimensions()
        {
            var display = new DisplaySection
            {
                Resolution = "Custom",
                Width = 2560,
                Height = 1080
            };

            bool resolved = ScrcpyExecutionService.TryResolveDisplayDimensions(display, out int width, out int height);

            Assert.True(resolved);
            Assert.Equal(2560, width);
            Assert.Equal(1080, height);
            Assert.Equal(2560, ScrcpyExecutionService.ResolveMaxSize(display));
        }

        [Fact]
        public void TryResolveDisplayDimensions_NativeResolution_ReturnsFalse()
        {
            var display = new DisplaySection
            {
                Resolution = "Native Screen Resolution"
            };

            bool resolved = ScrcpyExecutionService.TryResolveDisplayDimensions(display, out int width, out int height);

            Assert.False(resolved);
            Assert.Equal(0, width);
            Assert.Equal(0, height);
        }
    }
}
