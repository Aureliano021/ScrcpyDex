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
    }
}
