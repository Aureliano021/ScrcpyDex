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
using System.Net;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScrcpyDex.Models
{
    public enum DeviceTransport
    {
        Unknown = 0,
        Usb = 1,
        Wireless = 2
    }

    public static class DeviceTransportHelper
    {
        private static readonly Regex Ipv4PortRegex = new Regex(
            @"^\d{1,3}(?:\.\d{1,3}){3}:\d{1,5}$",
            RegexOptions.Compiled);

        private static readonly Regex Ipv6BracketPortRegex = new Regex(
            @"^\[[0-9a-fA-F:]+\]:\d{1,5}$",
            RegexOptions.Compiled);

        private static readonly Regex MdnsConnectHardwareSerialRegex = new Regex(
            @"^adb-(.+)-[A-Za-z0-9]+\._adb-tls-connect\._tcp$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsNativeMdnsTlsSerial(string? serial) =>
            !string.IsNullOrWhiteSpace(serial) &&
            serial.Contains("._adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase);

        public static string ExtractHardwareSerial(string? serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return string.Empty;
            string trimmed = serial.Trim();

            var mdnsMatch = MdnsConnectHardwareSerialRegex.Match(trimmed);
            if (mdnsMatch.Success)
            {
                return mdnsMatch.Groups[1].Value;
            }

            if (ClassifyTransport(trimmed) == DeviceTransport.Usb)
            {
                return trimmed;
            }

            return string.Empty;
        }

        /// <summary>
        /// Classifies the ADB transport type (USB or Wireless) from the device serial and optional raw 'adb devices -l' line.
        /// </summary>
        public static DeviceTransport ClassifyTransport(string? serial, string? rawDeviceLine = null)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return DeviceTransport.Unknown;
            }

            string trimmedSerial = serial.Trim();

            if (Ipv4PortRegex.IsMatch(trimmedSerial))
            {
                int colonIdx = trimmedSerial.LastIndexOf(':');
                if (colonIdx > 0 &&
                    IPAddress.TryParse(trimmedSerial.Substring(0, colonIdx), out _) &&
                    int.TryParse(trimmedSerial.Substring(colonIdx + 1), out int port) &&
                    port is >= 1 and <= 65535)
                {
                    return DeviceTransport.Wireless;
                }
            }

            if (Ipv6BracketPortRegex.IsMatch(trimmedSerial))
            {
                return DeviceTransport.Wireless;
            }

            if (trimmedSerial.Contains("._adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase) ||
                trimmedSerial.Contains("._adb._tcp", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceTransport.Wireless;
            }

            if (trimmedSerial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceTransport.Unknown;
            }

            if (!string.IsNullOrWhiteSpace(rawDeviceLine) &&
                Regex.IsMatch(rawDeviceLine, @"(?:^|\s)usb:[^\s]+", RegexOptions.IgnoreCase))
            {
                return DeviceTransport.Usb;
            }

            return DeviceTransport.Usb;
        }
    }

    public partial class DeviceInfo : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModelCode))]
        private string _serial = string.Empty;

        partial void OnSerialChanged(string value)
        {
            if (Transport == DeviceTransport.Unknown && !string.IsNullOrWhiteSpace(value))
            {
                Transport = DeviceTransportHelper.ClassifyTransport(value);
            }
            if (string.IsNullOrWhiteSpace(HardwareSerial))
            {
                HardwareSerial = DeviceTransportHelper.ExtractHardwareSerial(value);
            }
            if (DeviceTransportHelper.IsNativeMdnsTlsSerial(value))
            {
                IsTlsWireless = true;
            }
        }

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWireless))]
        [NotifyPropertyChangedFor(nameof(IsUsb))]
        [NotifyPropertyChangedFor(nameof(IsLegacyTcpIp))]
        private DeviceTransport _transport = DeviceTransport.Unknown;

        [ObservableProperty]
        private string _hardwareSerial = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLegacyTcpIp))]
        private bool _isTlsWireless;

        public bool IsWireless => Transport == DeviceTransport.Wireless;
        public bool IsUsb => Transport == DeviceTransport.Usb;
        public bool IsLegacyTcpIp => IsWireless && !IsTlsWireless;

        public DeviceCapability Capability => SamsungDeviceModelResolver.ResolveCapability(Model, Manufacturer);
        public bool IsDexSupported => Capability == DeviceCapability.SamsungDeX;
        public bool IsSamsung => Capability != DeviceCapability.GenericMirrorOnly;

        public string MarketingName => SamsungDeviceModelResolver.ResolveMarketingName(Model, Manufacturer);
        public string ModelCode => SamsungDeviceModelResolver.ExtractModelCode(Model, Serial);
        public string FrameLabel => SamsungDeviceModelResolver.ResolveFrameLabel(Model, Manufacturer);

        public DeviceInfo() { }

        public DeviceInfo(string serial, string model, string state, string? manufacturer = null, DeviceTransport? transport = null)
        {
            Serial = serial ?? string.Empty;
            Model = model ?? "Unknown";
            State = state ?? "offline";
            Manufacturer = manufacturer ?? string.Empty;
            Transport = transport ?? DeviceTransportHelper.ClassifyTransport(Serial);
        }
    }
}
