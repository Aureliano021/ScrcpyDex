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
using System.Text.RegularExpressions;

namespace ScrcpyDex.Models
{
    /// <summary>
    /// Utility to map Samsung hardware model codes (e.g. SM-S911B) to user-friendly marketing names and frame labels.
    /// </summary>
    public static class SamsungDeviceModelResolver
    {
        public static string NormalizeModelCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string clean = raw.Trim().Replace('_', '-');
            return clean;
        }

        public static string ResolveMarketingName(string? rawModel)
        {
            if (string.IsNullOrWhiteSpace(rawModel) || rawModel.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return "Samsung Galaxy Device";
            }

            string model = NormalizeModelCode(rawModel);

            // Galaxy S24 Family
            if (model.StartsWith("SM-S928", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S24 Ultra";
            if (model.StartsWith("SM-S926", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S24+";
            if (model.StartsWith("SM-S921", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S24";
            if (model.StartsWith("SM-S721", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S24 FE";

            // Galaxy S23 Family
            if (model.StartsWith("SM-S918", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S23 Ultra";
            if (model.StartsWith("SM-S916", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S23+";
            if (model.StartsWith("SM-S911", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S23";
            if (model.StartsWith("SM-S711", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S23 FE";

            // Galaxy S22 Family
            if (model.StartsWith("SM-S908", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S22 Ultra";
            if (model.StartsWith("SM-S906", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S22+";
            if (model.StartsWith("SM-S901", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S22";

            // Galaxy S21 Family
            if (model.StartsWith("SM-G998", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S21 Ultra";
            if (model.StartsWith("SM-G996", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S21+";
            if (model.StartsWith("SM-G991", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S21";
            if (model.StartsWith("SM-G990", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S21 FE";

            // Galaxy S20 Family
            if (model.StartsWith("SM-G988", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S20 Ultra";
            if (model.StartsWith("SM-G986", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S20+";
            if (model.StartsWith("SM-G981", StringComparison.OrdinalIgnoreCase) || model.StartsWith("SM-G980", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S20";
            if (model.StartsWith("SM-G781", StringComparison.OrdinalIgnoreCase) || model.StartsWith("SM-G780", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S20 FE";

            // Galaxy S10 Family
            if (model.StartsWith("SM-G975", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S10+";
            if (model.StartsWith("SM-G973", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S10";
            if (model.StartsWith("SM-G970", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S10e";
            if (model.StartsWith("SM-G770", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy S10 Lite";

            // Galaxy Note 20 Family
            if (model.StartsWith("SM-N986", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Note 20 Ultra";
            if (model.StartsWith("SM-N981", StringComparison.OrdinalIgnoreCase) || model.StartsWith("SM-N980", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Note 20";

            // Galaxy Note 10 Family
            if (model.StartsWith("SM-N975", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Note 10+";
            if (model.StartsWith("SM-N970", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Note 10";

            // Galaxy Z Fold Family
            if (model.StartsWith("SM-F956", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Fold 6";
            if (model.StartsWith("SM-F946", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Fold 5";
            if (model.StartsWith("SM-F936", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Fold 4";
            if (model.StartsWith("SM-F926", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Fold 3";
            if (model.StartsWith("SM-F916", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Fold 2";
            if (model.StartsWith("SM-F900", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Fold";

            // Galaxy Z Flip Family
            if (model.StartsWith("SM-F741", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Flip 6";
            if (model.StartsWith("SM-F731", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Flip 5";
            if (model.StartsWith("SM-F721", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Flip 4";
            if (model.StartsWith("SM-F711", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Flip 3";
            if (model.StartsWith("SM-F700", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Z Flip";

            // Galaxy Tab S Family
            if (model.StartsWith("SM-X92", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S10 Ultra";
            if (model.StartsWith("SM-X82", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S10+";
            if (model.StartsWith("SM-X91", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S9 Ultra";
            if (model.StartsWith("SM-X81", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S9+";
            if (model.StartsWith("SM-X71", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S9";
            if (model.StartsWith("SM-X90", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S8 Ultra";
            if (model.StartsWith("SM-X80", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S8+";
            if (model.StartsWith("SM-X70", StringComparison.OrdinalIgnoreCase)) return "Samsung Galaxy Tab S8";

            if (model.Contains("Galaxy", StringComparison.OrdinalIgnoreCase))
            {
                return model.StartsWith("Samsung", StringComparison.OrdinalIgnoreCase) ? model : $"Samsung {model}";
            }

            if (model.StartsWith("SM-", StringComparison.OrdinalIgnoreCase))
            {
                return $"Samsung Galaxy ({model})";
            }

            return $"Samsung {model}";
        }

        public static string ResolveFrameLabel(string? rawModel)
        {
            if (string.IsNullOrWhiteSpace(rawModel) || rawModel.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return "GALAXY";
            }

            string model = NormalizeModelCode(rawModel);

            if (model.StartsWith("SM-S928", StringComparison.OrdinalIgnoreCase)) return "S24 Ultra";
            if (model.StartsWith("SM-S926", StringComparison.OrdinalIgnoreCase)) return "S24+";
            if (model.StartsWith("SM-S921", StringComparison.OrdinalIgnoreCase)) return "S24";
            if (model.StartsWith("SM-S721", StringComparison.OrdinalIgnoreCase)) return "S24 FE";

            if (model.StartsWith("SM-S918", StringComparison.OrdinalIgnoreCase)) return "S23 Ultra";
            if (model.StartsWith("SM-S916", StringComparison.OrdinalIgnoreCase)) return "S23+";
            if (model.StartsWith("SM-S911", StringComparison.OrdinalIgnoreCase)) return "S23";
            if (model.StartsWith("SM-S711", StringComparison.OrdinalIgnoreCase)) return "S23 FE";

            if (model.StartsWith("SM-S908", StringComparison.OrdinalIgnoreCase)) return "S22 Ultra";
            if (model.StartsWith("SM-S906", StringComparison.OrdinalIgnoreCase)) return "S22+";
            if (model.StartsWith("SM-S901", StringComparison.OrdinalIgnoreCase)) return "S22";

            if (model.StartsWith("SM-G998", StringComparison.OrdinalIgnoreCase)) return "S21 Ultra";
            if (model.StartsWith("SM-G996", StringComparison.OrdinalIgnoreCase)) return "S21+";
            if (model.StartsWith("SM-G991", StringComparison.OrdinalIgnoreCase)) return "S21";

            if (model.StartsWith("SM-G988", StringComparison.OrdinalIgnoreCase)) return "S20 Ultra";
            if (model.StartsWith("SM-G986", StringComparison.OrdinalIgnoreCase)) return "S20+";
            if (model.StartsWith("SM-G981", StringComparison.OrdinalIgnoreCase) || model.StartsWith("SM-G980", StringComparison.OrdinalIgnoreCase)) return "S20";

            if (model.StartsWith("SM-N986", StringComparison.OrdinalIgnoreCase)) return "Note 20 Ultra";
            if (model.StartsWith("SM-N981", StringComparison.OrdinalIgnoreCase)) return "Note 20";

            if (model.StartsWith("SM-F956", StringComparison.OrdinalIgnoreCase)) return "Z Fold 6";
            if (model.StartsWith("SM-F946", StringComparison.OrdinalIgnoreCase)) return "Z Fold 5";
            if (model.StartsWith("SM-F936", StringComparison.OrdinalIgnoreCase)) return "Z Fold 4";
            if (model.StartsWith("SM-F926", StringComparison.OrdinalIgnoreCase)) return "Z Fold 3";

            if (model.StartsWith("SM-F741", StringComparison.OrdinalIgnoreCase)) return "Z Flip 6";
            if (model.StartsWith("SM-F731", StringComparison.OrdinalIgnoreCase)) return "Z Flip 5";
            if (model.StartsWith("SM-F721", StringComparison.OrdinalIgnoreCase)) return "Z Flip 4";
            if (model.StartsWith("SM-F711", StringComparison.OrdinalIgnoreCase)) return "Z Flip 3";

            if (model.StartsWith("SM-X", StringComparison.OrdinalIgnoreCase)) return "Galaxy Tab";

            return "GALAXY";
        }

        public static string ExtractModelCode(string? rawModel, string? serial = null)
        {
            if (string.IsNullOrWhiteSpace(rawModel)) return serial ?? string.Empty;
            string clean = NormalizeModelCode(rawModel);
            if (clean.StartsWith("SM-", StringComparison.OrdinalIgnoreCase)) return clean;
            return clean;
        }
    }
}
