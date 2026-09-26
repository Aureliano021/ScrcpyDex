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
using System.Text.Json.Serialization;

namespace ScrcpyDex.Core.Configuration
{
    public sealed class ScrcpyDeXSettings
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("display")]
        public DisplaySection Display { get; set; } = new DisplaySection();

        [JsonPropertyName("audio")]
        public AudioSection Audio { get; set; } = new AudioSection();

        [JsonPropertyName("input")]
        public InputSection Input { get; set; } = new InputSection();

        [JsonPropertyName("device")]
        public DeviceSection Device { get; set; } = new DeviceSection();

        [JsonPropertyName("ui")]
        public UiSection Ui { get; set; } = new UiSection();
    }

    public sealed class DisplaySection
    {
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = "1920x1080";

        [JsonPropertyName("width")]
        public int? Width { get; set; } = 1920;

        [JsonPropertyName("height")]
        public int? Height { get; set; } = 1080;

        [JsonPropertyName("dpi")]
        public int? Dpi { get; set; } = 320;

        [JsonPropertyName("fps")]
        public int Fps { get; set; } = 60;

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; } = 8000000;

        [JsonPropertyName("codec")]
        public string Codec { get; set; } = "h264";

        [JsonPropertyName("windowMode")]
        public string WindowMode { get; set; } = "normal";
    }

    public sealed class AudioSection
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("bufferMs")]
        public int BufferMs { get; set; } = 50;
    }

    public sealed class InputSection
    {
        [JsonPropertyName("mouseDriver")]
        public string MouseDriver { get; set; } = "uhid";

        [JsonPropertyName("releaseKey")]
        public string ReleaseKey { get; set; } = "LeftAlt";

        [JsonPropertyName("forwardGamepad")]
        public bool ForwardGamepad { get; set; } = false;
    }

    public sealed class DeviceSection
    {
        [JsonPropertyName("turnScreenOff")]
        public bool TurnScreenOff { get; set; } = true;

        [JsonPropertyName("stayAwake")]
        public bool StayAwake { get; set; } = true;

        [JsonPropertyName("autoConnect")]
        public bool AutoConnect { get; set; } = false;
    }

    public sealed class UiSection
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "dark";

        [JsonPropertyName("startMinimized")]
        public bool StartMinimized { get; set; } = false;

        [JsonPropertyName("showLogPanel")]
        public bool ShowLogPanel { get; set; } = true;
    }
}
