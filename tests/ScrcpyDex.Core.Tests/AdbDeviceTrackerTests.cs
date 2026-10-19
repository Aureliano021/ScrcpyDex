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
using System.Collections.Generic;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Models;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class AdbDeviceTrackerTests
    {
        [Fact]
        public void ParseDeviceList_EmptyOrWhitespace_ReturnsEmptyList()
        {
            var res1 = AdbDeviceTracker.ParseDeviceList("");
            var res2 = AdbDeviceTracker.ParseDeviceList("   \n\r  ");

            Assert.Empty(res1);
            Assert.Empty(res2);
        }

        [Fact]
        public void ParseDeviceList_TrackDevicesLongFormat_ExtractsModelAndProperties()
        {
            // Synthetic test fixture per privacy guidelines
            string raw = "R5CW123456\tdevice product:dm1qxxx model:SM_S911B device:dm1q transport_id:9\n";

            var list = AdbDeviceTracker.ParseDeviceList(raw);

            Assert.Single(list);
            var dev = list[0];
            Assert.Equal("R5CW123456", dev.Serial);
            Assert.Equal("device", dev.State);
            Assert.Equal("SM_S911B", dev.Model);
            Assert.Equal("Samsung Galaxy S23", dev.MarketingName);
            Assert.Equal("SM-S911B", dev.ModelCode);
            Assert.Equal("S23", dev.FrameLabel);
        }

        [Fact]
        public void ParseDeviceList_MultipleDevices_ParsesAllAccurately()
        {
            string raw = "R5CW123456\tdevice product:dm1qxxx model:SM_S911B device:dm1q transport_id:9\n" +
                         "R5CW654321\tunauthorized\n";

            var list = AdbDeviceTracker.ParseDeviceList(raw);

            Assert.Equal(2, list.Count);
            Assert.Equal("R5CW123456", list[0].Serial);
            Assert.Equal("device", list[0].State);
            Assert.Equal("SM_S911B", list[0].Model);

            Assert.Equal("R5CW654321", list[1].Serial);
            Assert.Equal("unauthorized", list[1].State);
            Assert.Equal("Unknown", list[1].Model);
        }

        [Theory]
        [InlineData("SM-S911B", "Samsung Galaxy S23", "S23")]
        [InlineData("SM_S911B", "Samsung Galaxy S23", "S23")]
        [InlineData("SM-S918B", "Samsung Galaxy S23 Ultra", "S23 Ultra")]
        [InlineData("SM-S928B", "Samsung Galaxy S24 Ultra", "S24 Ultra")]
        [InlineData("SM-S9210", "Samsung Galaxy S24", "S24")]
        [InlineData("SM-F946B", "Samsung Galaxy Z Fold 5", "Z Fold 5")]
        [InlineData("SM-F731B", "Samsung Galaxy Z Flip 5", "Z Flip 5")]
        [InlineData("SM-X910", "Samsung Galaxy Tab S9 Ultra", "Galaxy Tab")]
        [InlineData("Unknown", "Samsung Galaxy Device", "GALAXY")]
        public void SamsungDeviceModelResolver_ResolvesKnownModels(string rawModel, string expectedMarketing, string expectedFrame)
        {
            string marketing = SamsungDeviceModelResolver.ResolveMarketingName(rawModel);
            string frame = SamsungDeviceModelResolver.ResolveFrameLabel(rawModel);

            Assert.Equal(expectedMarketing, marketing);
            Assert.Equal(expectedFrame, frame);
        }
    }
}
