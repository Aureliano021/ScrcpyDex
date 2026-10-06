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

namespace ScrcpyDex.Models
{
    public class DeviceInfo
    {
        public string Serial { get; set; } = string.Empty;
        public string Model { get; set; } = "Unknown";
        public string Manufacturer { get; set; } = "Samsung";
        public string State { get; set; } = "device";
        public string AndroidVersion { get; set; } = "";
        public string OneUiVersion { get; set; } = "";
        public bool IsConnected
        {
            get => State == "device";
            set => State = value ? "device" : "offline";
        }
        public int DeXDisplayId { get; set; } = -1;

        public DeviceInfo() { }

        public DeviceInfo(string serial, string model, string state)
        {
            Serial = serial ?? string.Empty;
            Model = model ?? "Unknown";
            State = state ?? "offline";
        }
    }
}

namespace ScrcpyDex.WinUI.Models
{
    public class DeviceInfo : ScrcpyDex.Models.DeviceInfo
    {
        public DeviceInfo() { }
        public DeviceInfo(string serial, string model, string state) : base(serial, model, state) { }
    }
}
