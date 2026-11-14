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
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScrcpyDex.Models
{
    public partial class DeviceInfo : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModelCode))]
        private string _serial = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Capability))]
        [NotifyPropertyChangedFor(nameof(IsDexSupported))]
        [NotifyPropertyChangedFor(nameof(IsSamsung))]
        [NotifyPropertyChangedFor(nameof(MarketingName))]
        [NotifyPropertyChangedFor(nameof(ModelCode))]
        [NotifyPropertyChangedFor(nameof(FrameLabel))]
        private string _model = "Unknown";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Capability))]
        [NotifyPropertyChangedFor(nameof(IsDexSupported))]
        [NotifyPropertyChangedFor(nameof(IsSamsung))]
        [NotifyPropertyChangedFor(nameof(MarketingName))]
        [NotifyPropertyChangedFor(nameof(FrameLabel))]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsConnected))]
        private string _state = "device";

        [ObservableProperty]
        private string _androidVersion = "";

        [ObservableProperty]
        private string _oneUiVersion = "";

        public bool IsConnected
        {
            get => State == "device";
            set => State = value ? "device" : "offline";
        }

        [ObservableProperty]
        private int _deXDisplayId = -1;

        public DeviceCapability Capability => SamsungDeviceModelResolver.ResolveCapability(Model, Manufacturer);
        public bool IsDexSupported => Capability == DeviceCapability.SamsungDeX;
        public bool IsSamsung => Capability != DeviceCapability.GenericMirrorOnly;

        public string MarketingName => SamsungDeviceModelResolver.ResolveMarketingName(Model, Manufacturer);
        public string ModelCode => SamsungDeviceModelResolver.ExtractModelCode(Model, Serial);
        public string FrameLabel => SamsungDeviceModelResolver.ResolveFrameLabel(Model, Manufacturer);

        public DeviceInfo() { }

        public DeviceInfo(string serial, string model, string state, string? manufacturer = null)
        {
            Serial = serial ?? string.Empty;
            Model = model ?? "Unknown";
            State = state ?? "offline";
            Manufacturer = manufacturer ?? string.Empty;
        }
    }
}
