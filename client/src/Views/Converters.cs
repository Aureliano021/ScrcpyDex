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
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ScrcpyDex.WinUI.Views
{
    public class DeviceStatusToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush GreenBg = new SolidColorBrush(Color.FromArgb(220, 24, 48, 28));
        private static readonly SolidColorBrush GreenBorder = new SolidColorBrush(Color.FromRgb(46, 125, 50));
        private static readonly SolidColorBrush GreenDot = new SolidColorBrush(Color.FromRgb(76, 217, 100));
        private static readonly SolidColorBrush GreenText = new SolidColorBrush(Color.FromRgb(165, 235, 175));

        private static readonly SolidColorBrush OrangeBg = new SolidColorBrush(Color.FromArgb(220, 50, 36, 14));
        private static readonly SolidColorBrush OrangeBorder = new SolidColorBrush(Color.FromRgb(180, 110, 20));
        private static readonly SolidColorBrush OrangeDot = new SolidColorBrush(Color.FromRgb(255, 149, 0));
        private static readonly SolidColorBrush OrangeText = new SolidColorBrush(Color.FromRgb(255, 205, 120));

        private static readonly SolidColorBrush RedBg = new SolidColorBrush(Color.FromArgb(220, 50, 20, 20));
        private static readonly SolidColorBrush RedBorder = new SolidColorBrush(Color.FromRgb(160, 40, 40));
        private static readonly SolidColorBrush RedDot = new SolidColorBrush(Color.FromRgb(255, 59, 48));
        private static readonly SolidColorBrush RedText = new SolidColorBrush(Color.FromRgb(255, 140, 140));

        static DeviceStatusToBrushConverter()
        {
            GreenBg.Freeze();
            GreenBorder.Freeze();
            GreenDot.Freeze();
            GreenText.Freeze();

            OrangeBg.Freeze();
            OrangeBorder.Freeze();
            OrangeDot.Freeze();
            OrangeText.Freeze();

            RedBg.Freeze();
            RedBorder.Freeze();
            RedDot.Freeze();
            RedText.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string color = value as string ?? "Red";
            string role = parameter as string ?? "Background";

            switch (color.ToLowerInvariant())
            {
                case "green":
                    return role switch
                    {
                        "Border" => GreenBorder,
                        "Dot" => GreenDot,
                        "Text" => GreenText,
                        _ => GreenBg
                    };

                case "orange":
                    return role switch
                    {
                        "Border" => OrangeBorder,
                        "Dot" => OrangeDot,
                        "Text" => OrangeText,
                        _ => OrangeBg
                    };

                default:
                    return role switch
                    {
                        "Border" => RedBorder,
                        "Dot" => RedDot,
                        "Text" => RedText,
                        _ => RedBg
                    };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public class BitrateLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int bps)
            {
                int mbps = bps / 1000000;
                return $"{mbps} Mbps";
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class LogEntryColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush ErrBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100));
        private static readonly SolidColorBrush WarnBrush = new SolidColorBrush(Color.FromRgb(255, 200, 80));
        private static readonly SolidColorBrush InfoBrush = new SolidColorBrush(Color.FromRgb(220, 225, 230));
        private static readonly SolidColorBrush AutoBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255));
        private static readonly SolidColorBrush StateBrush = new SolidColorBrush(Color.FromRgb(180, 130, 255));

        static LogEntryColorConverter()
        {
            ErrBrush.Freeze();
            WarnBrush.Freeze();
            InfoBrush.Freeze();
            AutoBrush.Freeze();
            StateBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string line = value?.ToString() ?? string.Empty;
            if (line.Contains("[ERR]") || line.Contains("[CRIT]")) return ErrBrush;
            if (line.Contains("[WARN]")) return WarnBrush;
            if (line.Contains("[AUTO]")) return AutoBrush;
            if (line.Contains("[State Machine]") || line.Contains("[Session]")) return StateBrush;
            return InfoBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
