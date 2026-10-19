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

namespace ScrcpyDex.App.Converters
{
    /// <summary>
    /// Value converter that maps boolean values to boolean visibility flags (IsVisible property).
    /// Supports an optional "inverse" parameter to invert the condition.
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool boolValue = false;
            if (value is bool b)
            {
                boolValue = b;
            }
            else if (value is string s)
            {
                boolValue = !string.IsNullOrWhiteSpace(s);
            }
            else if (value != null)
            {
                boolValue = true;
            }

            if (parameter is string paramStr && paramStr.Equals("inverse", StringComparison.OrdinalIgnoreCase))
            {
                boolValue = !boolValue;
            }

            return boolValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (parameter is string paramStr && paramStr.Equals("inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return !b;
                }
                return b;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts a boolean value to its inverted boolean counterpart.
    /// </summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}
