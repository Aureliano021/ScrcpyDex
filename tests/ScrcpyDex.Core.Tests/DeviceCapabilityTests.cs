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
using ScrcpyDex.Models;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class DeviceCapabilityTests
    {
        [Theory]
        [InlineData("Redmi Note 8", "Xiaomi", DeviceCapability.GenericMirrorOnly, "Xiaomi Redmi Note 8", "?")]
        [InlineData("Redmi_Note_8", "xiaomi", DeviceCapability.GenericMirrorOnly, "Xiaomi Redmi Note 8", "?")]
        [InlineData("Pixel 7", "Google", DeviceCapability.GenericMirrorOnly, "Google Pixel 7", "?")]
        [InlineData("moto g84 5G", "motorola", DeviceCapability.GenericMirrorOnly, "Motorola moto g84 5G", "?")]
        [InlineData("2201116SG", "Xiaomi", DeviceCapability.GenericMirrorOnly, "Xiaomi 2201116SG", "?")]
        public void NonSamsungDevices_ClassifiedAsGenericMirrorOnly(
            string model,
            string manufacturer,
            DeviceCapability expectedCapability,
            string expectedMarketing,
            string expectedFrame)
        {
            var capability = SamsungDeviceModelResolver.ResolveCapability(model, manufacturer);
            var marketing = SamsungDeviceModelResolver.ResolveMarketingName(model, manufacturer);
            var frame = SamsungDeviceModelResolver.ResolveFrameLabel(model, manufacturer);

            Assert.Equal(expectedCapability, capability);
            Assert.Equal(expectedMarketing, marketing);
            Assert.Equal(expectedFrame, frame);
            Assert.False(SamsungDeviceModelResolver.IsSamsungDevice(model, manufacturer));
            Assert.False(SamsungDeviceModelResolver.IsDeXSupportedSamsungModel(model));
        }

        [Theory]
        [InlineData("SM-A546B", "Samsung", DeviceCapability.SamsungMirrorOnly, "Samsung Galaxy A (SM-A546B)", "GALAXY")]
        [InlineData("SM-A346M", "samsung", DeviceCapability.SamsungMirrorOnly, "Samsung Galaxy A (SM-A346M)", "GALAXY")]
        [InlineData("SM-M536B", "Samsung", DeviceCapability.SamsungMirrorOnly, "Samsung Galaxy M (SM-M536B)", "GALAXY")]
        [InlineData("SM-F731B", "Samsung", DeviceCapability.SamsungMirrorOnly, "Samsung Galaxy Z Flip 5", "Z Flip 5")]
        [InlineData("SM-F700F", "Samsung", DeviceCapability.SamsungMirrorOnly, "Samsung Galaxy Z Flip", "Z Flip")]
        public void SamsungNonDexDevices_ClassifiedAsSamsungMirrorOnly(
            string model,
            string manufacturer,
            DeviceCapability expectedCapability,
            string expectedMarketing,
            string expectedFrame)
        {
            var capability = SamsungDeviceModelResolver.ResolveCapability(model, manufacturer);
            var marketing = SamsungDeviceModelResolver.ResolveMarketingName(model, manufacturer);
            var frame = SamsungDeviceModelResolver.ResolveFrameLabel(model, manufacturer);

            Assert.Equal(expectedCapability, capability);
            Assert.Equal(expectedMarketing, marketing);
            Assert.Equal(expectedFrame, frame);
            Assert.True(SamsungDeviceModelResolver.IsSamsungDevice(model, manufacturer));
            Assert.False(SamsungDeviceModelResolver.IsDeXSupportedSamsungModel(model));
        }

        [Theory]
        [InlineData("SM-S911B", "Samsung", DeviceCapability.SamsungDeX, "Samsung Galaxy S23", "S23")]
        [InlineData("SM-S928B", "Samsung", DeviceCapability.SamsungDeX, "Samsung Galaxy S24 Ultra", "S24 Ultra")]
        [InlineData("SM-F946B", "Samsung", DeviceCapability.SamsungDeX, "Samsung Galaxy Z Fold 5", "Z Fold 5")]
        [InlineData("SM-N986B", "Samsung", DeviceCapability.SamsungDeX, "Samsung Galaxy Note 20 Ultra", "Note 20 Ultra")]
        [InlineData("SM-X910", "Samsung", DeviceCapability.SamsungDeX, "Samsung Galaxy Tab S9 Ultra", "Galaxy Tab")]
        public void SamsungDeXSupportedDevices_ClassifiedAsSamsungDeX(
            string model,
            string manufacturer,
            DeviceCapability expectedCapability,
            string expectedMarketing,
            string expectedFrame)
        {
            var capability = SamsungDeviceModelResolver.ResolveCapability(model, manufacturer);
            var marketing = SamsungDeviceModelResolver.ResolveMarketingName(model, manufacturer);
            var frame = SamsungDeviceModelResolver.ResolveFrameLabel(model, manufacturer);

            Assert.Equal(expectedCapability, capability);
            Assert.Equal(expectedMarketing, marketing);
            Assert.Equal(expectedFrame, frame);
            Assert.True(SamsungDeviceModelResolver.IsSamsungDevice(model, manufacturer));
            Assert.True(SamsungDeviceModelResolver.IsDeXSupportedSamsungModel(model));
        }

        [Fact]
        public void DeviceInfo_ReflectsCapabilitiesAccurately()
        {
            // Synthetic test fixture per privacy guidelines
            var xiaomi = new DeviceInfo("R5CW000001", "Redmi Note 8", "device", "Xiaomi");
            Assert.Equal(DeviceCapability.GenericMirrorOnly, xiaomi.Capability);
            Assert.False(xiaomi.IsDexSupported);
            Assert.False(xiaomi.IsSamsung);
            Assert.Equal("Xiaomi Redmi Note 8", xiaomi.MarketingName);
            Assert.Equal("?", xiaomi.FrameLabel);

            var galaxyA = new DeviceInfo("R5CW000002", "SM-A546B", "device", "Samsung");
            Assert.Equal(DeviceCapability.SamsungMirrorOnly, galaxyA.Capability);
            Assert.False(galaxyA.IsDexSupported);
            Assert.True(galaxyA.IsSamsung);
            Assert.Equal("GALAXY", galaxyA.FrameLabel);

            var galaxyS = new DeviceInfo("R5CW000003", "SM-S928B", "device", "Samsung");
            Assert.Equal(DeviceCapability.SamsungDeX, galaxyS.Capability);
            Assert.True(galaxyS.IsDexSupported);
            Assert.True(galaxyS.IsSamsung);
            Assert.Equal("Samsung Galaxy S24 Ultra", galaxyS.MarketingName);
            Assert.Equal("S24 Ultra", galaxyS.FrameLabel);
        }
    }
}
