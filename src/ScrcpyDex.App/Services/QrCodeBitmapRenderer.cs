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
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ScrcpyDex.Core.Adb;

namespace ScrcpyDex.App.Services
{
    /// <summary>
    /// Renders QR matrices into high-contrast Avalonia WriteableBitmap instances with a clean quiet zone.
    /// </summary>
    public static class QrCodeBitmapRenderer
    {
        public static WriteableBitmap Render(string payload, int pixelsPerModule = 4, int quietZoneModules = 2)
        {
            bool[,] modules = QrCodeMatrixGenerator.Generate(payload);
            int moduleCount = modules.GetLength(0);
            int totalModules = moduleCount + quietZoneModules * 2;
            int pixelSize = totalModules * pixelsPerModule;

            var bitmap = new WriteableBitmap(
                new PixelSize(pixelSize, pixelSize),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using var fb = bitmap.Lock();
            int stride = fb.RowBytes;
            byte[] buffer = new byte[stride * pixelSize];

            // BGRA8888 colors: White background (#FFFFFFFF), Dark charcoal foreground (#FF0E0E12)
            for (int py = 0; py < pixelSize; py++)
            {
                int my = py / pixelsPerModule - quietZoneModules;
                int rowOffset = py * stride;
                for (int px = 0; px < pixelSize; px++)
                {
                    int mx = px / pixelsPerModule - quietZoneModules;
                    bool isDark = mx >= 0 && mx < moduleCount && my >= 0 && my < moduleCount && modules[my, mx];

                    int idx = rowOffset + px * 4;
                    if (isDark)
                    {
                        buffer[idx] = 0x00;     // B
                        buffer[idx + 1] = 0x00; // G
                        buffer[idx + 2] = 0x00; // R
                        buffer[idx + 3] = 0xFF; // A
                    }
                    else
                    {
                        buffer[idx] = 0xFF;     // B
                        buffer[idx + 1] = 0xFF; // G
                        buffer[idx + 2] = 0xFF; // R
                        buffer[idx + 3] = 0xFF; // A
                    }
                }
            }

            Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
            return bitmap;
        }
    }
}
