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
using ScrcpyDex.Core.IO;

namespace ScrcpyDex.App.Converters
{
    /// <summary>
    /// Converts a ProcessLogEntry or boolean to a foreground brush for terminal display.
    /// </summary>
    public sealed class LogEntryBrushConverter : IValueConverter
    {
        private static readonly IBrush StandardBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
        private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6B6B"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ProcessLogEntry entry)
            {
                return entry.IsError ? ErrorBrush : StandardBrush;
            }

            if (value is bool isError)
            {
                return isError ? ErrorBrush : StandardBrush;
            }

            return StandardBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
