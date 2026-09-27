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
using System.ComponentModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScrcpyDex.Core.Configuration
{
    public sealed class ScrcpyDeXSettings : ObservableObject
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.1";

        private DisplaySection _display = new DisplaySection();
        private AudioSection _audio = new AudioSection();
        private InputSection _input = new InputSection();
        private DeviceSection _device = new DeviceSection();
        private PathsSection _paths = new PathsSection();
        private UiSection _ui = new UiSection();

        [JsonPropertyName("display")]
        public DisplaySection Display
        {
            get => _display;
            set
            {
                if (_display != null) _display.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _display, value ?? new DisplaySection());
                if (_display != null) _display.PropertyChanged += OnChildPropertyChanged;
            }
        }

        [JsonPropertyName("audio")]
        public AudioSection Audio
        {
            get => _audio;
            set
            {
                if (_audio != null) _audio.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _audio, value ?? new AudioSection());
                if (_audio != null) _audio.PropertyChanged += OnChildPropertyChanged;
            }
        }

        [JsonPropertyName("input")]
        public InputSection Input
        {
            get => _input;
            set
            {
                if (_input != null) _input.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _input, value ?? new InputSection());
                if (_input != null) _input.PropertyChanged += OnChildPropertyChanged;
            }
        }

        [JsonPropertyName("device")]
        public DeviceSection Device
        {
            get => _device;
            set
            {
                if (_device != null) _device.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _device, value ?? new DeviceSection());
                if (_device != null) _device.PropertyChanged += OnChildPropertyChanged;
            }
        }

        [JsonPropertyName("paths")]
        public PathsSection Paths
        {
            get => _paths;
            set
            {
                if (_paths != null) _paths.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _paths, value ?? new PathsSection());
                if (_paths != null) _paths.PropertyChanged += OnChildPropertyChanged;
            }
        }

        [JsonPropertyName("ui")]
        public UiSection Ui
        {
            get => _ui;
            set
            {
                if (_ui != null) _ui.PropertyChanged -= OnChildPropertyChanged;
                SetProperty(ref _ui, value ?? new UiSection());
                if (_ui != null) _ui.PropertyChanged += OnChildPropertyChanged;
            }
        }

        public event PropertyChangedEventHandler? SettingChanged;

        public ScrcpyDeXSettings()
        {
            AttachEvents();
        }

        private void AttachEvents()
        {
            _display.PropertyChanged += OnChildPropertyChanged;
            _audio.PropertyChanged += OnChildPropertyChanged;
            _input.PropertyChanged += OnChildPropertyChanged;
            _device.PropertyChanged += OnChildPropertyChanged;
            _paths.PropertyChanged += OnChildPropertyChanged;
            _ui.PropertyChanged += OnChildPropertyChanged;
        }

        private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            string prefix = sender switch
            {
                DisplaySection => "Display.",
                AudioSection => "Audio.",
                InputSection => "Input.",
                DeviceSection => "Device.",
                PathsSection => "Paths.",
                UiSection => "Ui.",
                _ => string.Empty
            };
            SettingChanged?.Invoke(this, new PropertyChangedEventArgs(prefix + e.PropertyName));
        }
    }

    public sealed class DisplaySection : ObservableObject
    {
        private string _resolution = "1920x1080";
        private int? _width = 1920;
        private int? _height = 1080;
        private int? _dpi = 320;
        private int _fps = 60;
        private int _bitrate = 8000000;
        private string _codec = "h264";
        private string _windowMode = "normal";

        [JsonPropertyName("resolution")]
        public string Resolution { get => _resolution; set => SetProperty(ref _resolution, value); }

        [JsonPropertyName("width")]
        public int? Width { get => _width; set => SetProperty(ref _width, value); }

        [JsonPropertyName("height")]
        public int? Height { get => _height; set => SetProperty(ref _height, value); }

        [JsonPropertyName("dpi")]
        public int? Dpi { get => _dpi; set => SetProperty(ref _dpi, value); }

        [JsonPropertyName("fps")]
        public int Fps { get => _fps; set => SetProperty(ref _fps, value); }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get => _bitrate; set => SetProperty(ref _bitrate, value); }

        [JsonPropertyName("codec")]
        public string Codec { get => _codec; set => SetProperty(ref _codec, value); }

        [JsonPropertyName("windowMode")]
        public string WindowMode { get => _windowMode; set => SetProperty(ref _windowMode, value); }
    }

    public sealed class AudioSection : ObservableObject
    {
        private bool _enabled = true;
        private int _bufferMs = 50;

        [JsonPropertyName("enabled")]
        public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }

        [JsonPropertyName("bufferMs")]
        public int BufferMs { get => _bufferMs; set => SetProperty(ref _bufferMs, value); }
    }

    public sealed class InputSection : ObservableObject
    {
        private string _mouseDriver = "uhid";
        private string _releaseKey = "LeftAlt";
        private bool _forwardGamepad = false;

        [JsonPropertyName("mouseDriver")]
        public string MouseDriver { get => _mouseDriver; set => SetProperty(ref _mouseDriver, value); }

        [JsonPropertyName("releaseKey")]
        public string ReleaseKey { get => _releaseKey; set => SetProperty(ref _releaseKey, value); }

        [JsonPropertyName("forwardGamepad")]
        public bool ForwardGamepad { get => _forwardGamepad; set => SetProperty(ref _forwardGamepad, value); }
    }

    public sealed class DeviceSection : ObservableObject
    {
        private bool _turnScreenOff = false;
        private bool _stayAwake = false;
        private bool _autoConnect = false;

        [JsonPropertyName("turnScreenOff")]
        public bool TurnScreenOff { get => _turnScreenOff; set => SetProperty(ref _turnScreenOff, value); }

        [JsonPropertyName("stayAwake")]
        public bool StayAwake { get => _stayAwake; set => SetProperty(ref _stayAwake, value); }

        [JsonPropertyName("autoConnect")]
        public bool AutoConnect { get => _autoConnect; set => SetProperty(ref _autoConnect, value); }
    }

    public sealed class PathsSection : ObservableObject
    {
        private string? _scrcpyPath;
        private string? _adbPath;

        [JsonPropertyName("scrcpyPath")]
        public string? ScrcpyPath { get => _scrcpyPath; set => SetProperty(ref _scrcpyPath, value); }

        [JsonPropertyName("adbPath")]
        public string? AdbPath { get => _adbPath; set => SetProperty(ref _adbPath, value); }
    }

    public sealed class UiSection : ObservableObject
    {
        private string _theme = "dark";
        private bool _startMinimized = false;
        private bool _showLogPanel = true;

        [JsonPropertyName("theme")]
        public string Theme { get => _theme; set => SetProperty(ref _theme, value); }

        [JsonPropertyName("startMinimized")]
        public bool StartMinimized { get => _startMinimized; set => SetProperty(ref _startMinimized, value); }

        [JsonPropertyName("showLogPanel")]
        public bool ShowLogPanel { get => _showLogPanel; set => SetProperty(ref _showLogPanel, value); }
    }
}
