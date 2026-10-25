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
using ScrcpyDex.Core.IO;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class NonBlockingProcessStreamTests
    {
        [Theory]
        [InlineData("1 file pushed, 0 skipped. 17.5 MB/s (123456 bytes in 0.007s)", true, ProcessLogLevel.Info)]
        [InlineData("INFO: scrcpy 3.1 <https://github.com/Genymobile/scrcpy>", true, ProcessLogLevel.Info)]
        [InlineData("[server] INFO: Device: Samsung SM-S928B (Android 14)", true, ProcessLogLevel.Info)]
        [InlineData("* daemon not running; starting now at tcp:5037", true, ProcessLogLevel.Info)]
        [InlineData("* daemon started successfully", true, ProcessLogLevel.Info)]
        [InlineData("Renderer: direct3d11", false, ProcessLogLevel.Info)]
        [InlineData("Display: 2 (1920x1080)", false, ProcessLogLevel.Info)]
        public void ClassifyLogLevel_InformationalMessages_ReturnsInfo(string message, bool isStderr, ProcessLogLevel expected)
        {
            var level = NonBlockingProcessStream.ClassifyLogLevel(message, isStderr);
            Assert.Equal(expected, level);
        }

        [Theory]
        [InlineData("[server] WARN: Display doesn't have FLAG_SUPPORTS_PROTECTED_BUFFERS flag, mirroring can be restricted", true, ProcessLogLevel.Warning)]
        [InlineData("WARN: Demuxer 'video': option --video-codec is deprecated", true, ProcessLogLevel.Warning)]
        [InlineData("Warning: device connection latency is elevated", true, ProcessLogLevel.Warning)]
        [InlineData("Some unexpected diagnostic notice on stderr", true, ProcessLogLevel.Warning)]
        public void ClassifyLogLevel_WarningAndStderrAlerts_ReturnsWarning(string message, bool isStderr, ProcessLogLevel expected)
        {
            var level = NonBlockingProcessStream.ClassifyLogLevel(message, isStderr);
            Assert.Equal(expected, level);
        }

        [Theory]
        [InlineData("ERROR: Exception in thread \"main\" java.lang.RuntimeException", true, ProcessLogLevel.Error)]
        [InlineData("[server] ERROR: Exception during initialization", true, ProcessLogLevel.Error)]
        [InlineData("FATAL: Could not open video stream", true, ProcessLogLevel.Error)]
        [InlineData("ERROR: Could not open video stream", false, ProcessLogLevel.Error)]
        [InlineData("CRITICAL: Device disconnected abnormally", true, ProcessLogLevel.Error)]
        public void ClassifyLogLevel_CriticalErrors_ReturnsError(string message, bool isStderr, ProcessLogLevel expected)
        {
            var level = NonBlockingProcessStream.ClassifyLogLevel(message, isStderr);
            Assert.Equal(expected, level);
        }

        [Fact]
        public void ProcessLogEntry_Properties_ReflectLogLevelCorrectly()
        {
            var now = DateTime.UtcNow;

            var infoEntry = new ProcessLogEntry(now, "Normal message", ProcessLogLevel.Info);
            Assert.False(infoEntry.IsError);
            Assert.False(infoEntry.IsWarning);
            Assert.Equal(ProcessLogLevel.Info, infoEntry.Level);
            Assert.Contains("[OUT]", infoEntry.ToString());

            var warnEntry = new ProcessLogEntry(now, "Alert message", ProcessLogLevel.Warning);
            Assert.False(warnEntry.IsError);
            Assert.True(warnEntry.IsWarning);
            Assert.Equal(ProcessLogLevel.Warning, warnEntry.Level);
            Assert.Contains("[WARN]", warnEntry.ToString());

            var errEntry = new ProcessLogEntry(now, "Fatal failure", ProcessLogLevel.Error);
            Assert.True(errEntry.IsError);
            Assert.False(errEntry.IsWarning);
            Assert.Equal(ProcessLogLevel.Error, errEntry.Level);
            Assert.Contains("[ERR]", errEntry.ToString());

            var legacyBoolErr = new ProcessLogEntry(now, "Legacy error", true);
            Assert.True(legacyBoolErr.IsError);
            Assert.Equal(ProcessLogLevel.Error, legacyBoolErr.Level);

            var legacyBoolInfo = new ProcessLogEntry(now, "Legacy info", false);
            Assert.False(legacyBoolInfo.IsError);
            Assert.Equal(ProcessLogLevel.Info, legacyBoolInfo.Level);
        }
    }
}
