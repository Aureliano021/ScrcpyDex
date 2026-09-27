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
using System.Text.Json.Serialization;

namespace ScrcpyDex.Models
{
    public class DisplayConfig
    {
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = "1920x1080";

        [JsonPropertyName("width")]
        public int Width { get; set; } = 1920;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 1080;

        [JsonPropertyName("dpi")]
        public int Dpi { get; set; } = 160;

        [JsonPropertyName("fps")]
        public int Fps { get; set; } = 60;

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; } = 8000000;

        [JsonPropertyName("codec")]
        public string Codec { get; set; } = "h264";

        [JsonPropertyName("windowMode")]
        public string WindowMode { get; set; } = "normal";

        [JsonPropertyName("audioEnabled")]
        public bool AudioEnabled { get; set; } = true;

        [JsonPropertyName("turnScreenOff")]
        public bool TurnScreenOff { get; set; } = false;

        [JsonPropertyName("stayAwake")]
        public bool StayAwake { get; set; } = false;

        [JsonPropertyName("mouseDriver")]
        public string MouseDriver { get; set; } = "uhid";

        [JsonPropertyName("releaseKey")]
        public string ReleaseKey { get; set; } = "LeftAlt";

        public static DisplayConfig CreateDefault()
        {
            return new DisplayConfig();
        }
    }
}
