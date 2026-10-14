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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ScrcpyDex.Models;

namespace ScrcpyDex.App.Services
{
    /// <summary>
    /// Service that maps connected Samsung devices to physical device render images.
    /// Returns null when no rendered image exists, indicating the UI should display the graphical placeholder frame.
    /// </summary>
    public static class DeviceImageService
    {
        private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new(StringComparer.OrdinalIgnoreCase);

        // User explicit processed images directory
        private static readonly string UserDownloadsPath = @"C:\Users\aurel\Downloads\imagens aparelhos\processadas";

        /// <summary>
        /// Retrieves the device image bitmap for the given device, or null if no image is available.
        /// </summary>
        public static Bitmap? GetDeviceImage(DeviceInfo? device)
        {
            if (device == null || !device.IsConnected)
            {
                return null;
            }

            string lookupKey = $"{device.Model}|{device.MarketingName}|{device.FrameLabel}";
            if (BitmapCache.TryGetValue(lookupKey, out var cached) && cached != null)
            {
                return cached;
            }

            var resolved = ResolveBitmap(device);
            if (resolved != null)
            {
                BitmapCache[lookupKey] = resolved;
            }

            return resolved;
        }

        private static Bitmap? ResolveBitmap(DeviceInfo device)
        {
            var candidates = GetCandidateFilenames(device);
            if (candidates.Count == 0) return null;

            foreach (var filename in candidates)
            {
                var bitmap = TryLoadBitmap(filename);
                if (bitmap != null)
                {
                    return bitmap;
                }
            }

            return null;
        }

        private static IReadOnlyList<string> GetCandidateFilenames(DeviceInfo device)
        {
            var candidates = new List<string>();

            string model = device.Model ?? string.Empty;
            string marketing = device.MarketingName ?? string.Empty;
            string label = device.FrameLabel ?? string.Empty;
            string combined = $"{model} {marketing} {label}".ToLowerInvariant();
            string normalized = combined.Replace(" ", "").Replace("-", "").Replace("_", "");

            // 1. Direct label and model code matches
            string cleanLabel = label.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
            if (!string.IsNullOrWhiteSpace(cleanLabel))
            {
                candidates.Add($"{cleanLabel}.png");
            }

            // 2. Galaxy S23 Family
            if (normalized.Contains("s23ultra") || normalized.Contains("sms918"))
            {
                candidates.Add("s23ultra.png");
            }
            else if (normalized.Contains("s23+") || normalized.Contains("s23plus") || normalized.Contains("sms916"))
            {
                candidates.Add("s23+.png");
            }
            else if (normalized.Contains("s23fe") || normalized.Contains("sms711"))
            {
                candidates.Add("s23fe.png");
                candidates.Add("s23.png");
            }
            else if (normalized.Contains("s23") || normalized.Contains("sms911"))
            {
                candidates.Add("s23.png");
            }

            // 3. Galaxy S24 Family
            if (normalized.Contains("s24ultra") || normalized.Contains("sms928"))
            {
                candidates.Add("s24ultra.png");
            }
            else if (normalized.Contains("s24+") || normalized.Contains("s24plus") || normalized.Contains("sms926"))
            {
                candidates.Add("s24+.png");
            }
            else if (normalized.Contains("s24fe") || normalized.Contains("sms721"))
            {
                candidates.Add("s24fe.png");
                candidates.Add("s24.png");
            }
            else if (normalized.Contains("s24") || normalized.Contains("sms921"))
            {
                candidates.Add("s24.png");
            }

            // 4. Galaxy S25 Family
            if (normalized.Contains("s25ultra") || normalized.Contains("sms938"))
            {
                candidates.Add("s25ultra.png");
            }
            else if (normalized.Contains("s25+") || normalized.Contains("s25plus") || normalized.Contains("sms936"))
            {
                candidates.Add("s25+.png");
                candidates.Add("s25+_trio.png");
            }
            else if (normalized.Contains("s25") || normalized.Contains("sms931"))
            {
                candidates.Add("s25.png");
            }

            // 5. Galaxy S26 Family
            if (normalized.Contains("s26ultra") || normalized.Contains("sms948"))
            {
                candidates.Add("s26ultra.png");
            }
            else if (normalized.Contains("s26fe") || normalized.Contains("sms741"))
            {
                candidates.Add("s26fe.png");
            }
            else if (normalized.Contains("s26") || normalized.Contains("sms941"))
            {
                candidates.Add("s26.png");
            }

            // 6. Galaxy Z Fold Family
            if (normalized.Contains("zfold8ultra") || normalized.Contains("fold8ultra"))
            {
                candidates.Add("zfold8-ultra.png");
            }
            else if (normalized.Contains("zfold8") || normalized.Contains("fold8") || normalized.Contains("zfold") || normalized.Contains("fold"))
            {
                candidates.Add("zfold8.png");
                candidates.Add("zfold8-ultra.png");
            }

            return candidates;
        }

        private static Bitmap? TryLoadBitmap(string filename)
        {
            try
            {
                // 1. Primary check: User's explicit downloads folder
                if (Directory.Exists(UserDownloadsPath))
                {
                    string path = Path.Combine(UserDownloadsPath, filename);
                    if (File.Exists(path))
                    {
                        using var stream = File.OpenRead(path);
                        return new Bitmap(stream);
                    }
                }

                // 2. Application local Assets/Devices directory
                var searchDirs = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Assets", "Devices"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Devices"),
                    Path.Combine(Directory.GetCurrentDirectory(), "ScrcpyDex", "src", "ScrcpyDex.App", "Assets", "Devices"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "ScrcpyDex.App", "Assets", "Devices")
                };

                foreach (var dir in searchDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        string path = Path.Combine(dir, filename);
                        if (File.Exists(path))
                        {
                            using var stream = File.OpenRead(path);
                            return new Bitmap(stream);
                        }
                    }
                }

                // 3. Avalonia embedded resource
                var avaresUri = new Uri($"avares://ScrcpyDex.App/Assets/Devices/{filename}");
                if (AssetLoader.Exists(avaresUri))
                {
                    using var stream = AssetLoader.Open(avaresUri);
                    return new Bitmap(stream);
                }
            }
            catch
            {
                // Return null if bitmap cannot be decoded
            }

            return null;
        }
    }
}
