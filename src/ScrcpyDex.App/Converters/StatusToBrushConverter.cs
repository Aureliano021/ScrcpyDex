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
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScrcpyDex.Core.Lifecycle;

namespace ScrcpyDex.App.Converters
{
    /// <summary>
    /// Maps SessionState enum values to Avalonia SolidColorBrush instances.
    /// Green for active streaming, orange for negotiation/handshaking, red for error, and gray for idle.
    /// </summary>
    public sealed class StatusToBrushConverter : IValueConverter
    {
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#4CAF50"));
        private static readonly IBrush OrangeBrush = new SolidColorBrush(Color.Parse("#FF9800"));
        private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse("#F44336"));
        private static readonly IBrush GrayBrush = new SolidColorBrush(Color.Parse("#9E9E9E"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SessionState state)
            {
                return state switch
                {
                    SessionState.StreamingActive => GreenBrush,
                    SessionState.DeviceConnecting or
                    SessionState.ServerDeploying or
                    SessionState.DisplayNegotiating or
                    SessionState.Teardown => OrangeBrush,
                    SessionState.Error => RedBrush,
                    SessionState.Idle => GrayBrush,
                    _ => GrayBrush
                };
            }

            if (value is string strState)
            {
                return strState.ToLowerInvariant() switch
                {
                    "streamingactive" or "streaming" or "connected" or "ready" => GreenBrush,
                    "handshaking" or "connecting" or "deploying" or "negotiating" => OrangeBrush,
                    "error" or "failed" => RedBrush,
                    _ => GrayBrush
                };
            }

            return GrayBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
