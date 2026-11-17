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
using System.Collections.Generic;
using System.Text;

namespace ScrcpyDex.Core.Adb
{
    /// <summary>
    /// Dependency-free ISO/IEC 18004 Byte-mode QR Code matrix generator (Versions 1-6, Error Correction Level L).
    /// Supports ADB wireless pairing payloads (WIFI:T:ADB;S:...;P:...;;) across all platforms.
    /// </summary>
    public static class QrCodeMatrixGenerator
    {
        private readonly record struct VersionSpec(int Version, int DataCodewords, int EcCodewordsPerBlock, int Blocks, int[] AlignmentPositions);

        private static readonly VersionSpec[] Specs =
        {
            new(1, 19, 7, 1, Array.Empty<int>()),
            new(2, 34, 10, 1, new[] { 6, 18 }),
            new(3, 55, 15, 1, new[] { 6, 22 }),
            new(4, 80, 20, 1, new[] { 6, 26 }),
            new(5, 108, 26, 1, new[] { 6, 30 }),
            new(6, 136, 18, 2, new[] { 6, 34 })
        };

        private static readonly byte[] GfExp = new byte[512];
        private static readonly byte[] GfLog = new byte[256];

        static QrCodeMatrixGenerator()
        {
            int x = 1;
            for (int i = 0; i < 255; i++)
            {
                GfExp[i] = (byte)x;
                GfLog[x] = (byte)i;
                x <<= 1;
                if ((x & 0x100) != 0)
                {
                    x ^= 0x11D;
                }
            }
            for (int i = 255; i < 512; i++)
            {
                GfExp[i] = GfExp[i - 255];
            }
        }

        public static bool[,] Generate(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("QR content cannot be empty.", nameof(content));
            }

            byte[] payload = Encoding.UTF8.GetBytes(content);
            VersionSpec spec = SelectVersion(payload.Length);
            int size = spec.Version * 4 + 17;

            byte[] dataCodewords = EncodeDataCodewords(payload, spec.DataCodewords);
            byte[] finalCodewords = BuildInterleavedCodewords(dataCodewords, spec);

            bool[,] modules = new bool[size, size];
            bool[,] isFunction = new bool[size, size];

            DrawFunctionPatterns(modules, isFunction, spec, size);
            DrawCodewords(modules, isFunction, finalCodewords, size);

            int bestMask = 0;
            int bestPenalty = int.MaxValue;
            bool[,] bestModules = modules;

            for (int mask = 0; mask < 8; mask++)
            {
                var candidate = (bool[,])modules.Clone();
                ApplyMask(candidate, isFunction, size, mask);
                DrawFormatInfo(candidate, size, mask);
                int penalty = CalculatePenalty(candidate, size);
                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestMask = mask;
                    bestModules = candidate;
                }
            }

            return bestModules;
        }

        private static VersionSpec SelectVersion(int byteLength)
        {
            foreach (var spec in Specs)
            {
                // Byte mode overhead: 4-bit mode + 8-bit count + 4-bit terminator (max) <= 2 bytes
                int capacityBytes = spec.DataCodewords - 2;
                if (byteLength <= capacityBytes)
                {
                    return spec;
                }
            }

            throw new ArgumentException($"Payload length {byteLength} exceeds supported QR capacity (134 bytes).", nameof(byteLength));
        }

        private static byte[] EncodeDataCodewords(byte[] payload, int dataCodewords)
        {
            var bits = new List<bool>(dataCodewords * 8);
            // Mode indicator: 0100 (Byte mode)
            AppendBits(bits, 0b0100, 4);
            // Character count (8 bits for Versions 1-9)
            AppendBits(bits, (uint)payload.Length, 8);
            foreach (byte b in payload)
            {
                AppendBits(bits, b, 8);
            }

            int totalBits = dataCodewords * 8;
            int terminatorBits = Math.Min(4, totalBits - bits.Count);
            for (int i = 0; i < terminatorBits; i++)
            {
                bits.Add(false);
            }

            while (bits.Count % 8 != 0)
            {
                bits.Add(false);
            }

            var codewords = new byte[dataCodewords];
            int idx = 0;
            for (int i = 0; i < bits.Count; i += 8)
            {
                byte val = 0;
                for (int b = 0; b < 8; b++)
                {
                    if (bits[i + b]) val |= (byte)(1 << (7 - b));
                }
                codewords[idx++] = val;
            }

            bool toggle = true;
            while (idx < dataCodewords)
            {
                codewords[idx++] = toggle ? (byte)0xEC : (byte)0x11;
                toggle = !toggle;
            }

            return codewords;
        }

        private static byte[] BuildInterleavedCodewords(byte[] data, VersionSpec spec)
        {
            int blockSize = spec.DataCodewords / spec.Blocks;
            var dataBlocks = new List<byte[]>(spec.Blocks);
            var ecBlocks = new List<byte[]>(spec.Blocks);
            byte[] generator = BuildRsGenerator(spec.EcCodewordsPerBlock);

            int offset = 0;
            for (int b = 0; b < spec.Blocks; b++)
            {
                var block = new byte[blockSize];
                Array.Copy(data, offset, block, 0, blockSize);
                offset += blockSize;

                dataBlocks.Add(block);
                ecBlocks.Add(ComputeRsRemainder(block, generator));
            }

            var result = new List<byte>(spec.DataCodewords + spec.Blocks * spec.EcCodewordsPerBlock);
            for (int i = 0; i < blockSize; i++)
            {
                for (int b = 0; b < spec.Blocks; b++)
                {
                    result.Add(dataBlocks[b][i]);
                }
            }
            for (int i = 0; i < spec.EcCodewordsPerBlock; i++)
            {
                for (int b = 0; b < spec.Blocks; b++)
                {
                    result.Add(ecBlocks[b][i]);
                }
            }

            return result.ToArray();
        }

        private static byte[] BuildRsGenerator(int degree)
        {
            var poly = new byte[degree];
            poly[degree - 1] = 1;
            byte root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    poly[j] = GfMultiply(poly[j], root);
                    if (j + 1 < degree)
                    {
                        poly[j] ^= poly[j + 1];
                    }
                }
                root = GfMultiply(root, 2);
            }
            return poly;
        }

        private static byte[] ComputeRsRemainder(byte[] data, byte[] generator)
        {
            var result = new byte[generator.Length];
            foreach (byte b in data)
            {
                byte factor = (byte)(b ^ result[0]);
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (int i = 0; i < generator.Length; i++)
                {
                    result[i] ^= GfMultiply(generator[i], factor);
                }
            }
            return result;
        }

        private static byte GfMultiply(byte a, byte b)
        {
            if (a == 0 || b == 0) return 0;
            return GfExp[GfLog[a] + GfLog[b]];
        }

        private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, VersionSpec spec, int size)
        {
            // Timing patterns
            for (int i = 0; i < size; i++)
            {
                SetFunctionModule(modules, isFunction, 6, i, i % 2 == 0);
                SetFunctionModule(modules, isFunction, i, 6, i % 2 == 0);
            }

            // Finder patterns (3 corners)
            DrawFinderPattern(modules, isFunction, 3, 3, size);
            DrawFinderPattern(modules, isFunction, size - 4, 3, size);
            DrawFinderPattern(modules, isFunction, 3, size - 4, size);

            // Alignment patterns
            int[] align = spec.AlignmentPositions;
            for (int i = 0; i < align.Length; i++)
            {
                for (int j = 0; j < align.Length; j++)
                {
                    bool isCorner = (i == 0 && j == 0) ||
                                    (i == 0 && j == align.Length - 1) ||
                                    (i == align.Length - 1 && j == 0);
                    if (!isCorner)
                    {
                        DrawAlignmentPattern(modules, isFunction, align[i], align[j]);
                    }
                }
            }

            // Format info placeholders + Dark module
            for (int i = 0; i <= 8; i++)
            {
                if (i != 6)
                {
                    SetFunctionModule(modules, isFunction, 8, i, false);
                    SetFunctionModule(modules, isFunction, i, 8, false);
                }
            }
            for (int i = size - 8; i < size; i++)
            {
                SetFunctionModule(modules, isFunction, 8, i, false);
                SetFunctionModule(modules, isFunction, i, 8, false);
            }
            SetFunctionModule(modules, isFunction, 8, size - 8, true);
        }

        private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int cx, int cy, int size)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || x >= size || y < 0 || y >= size) continue;
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    bool dark = dist != 2 && dist != 4;
                    SetFunctionModule(modules, isFunction, x, y, dark);
                }
            }
        }

        private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int cx, int cy)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunctionModule(modules, isFunction, cx + dx, cy + dy, dist != 1);
                }
            }
        }

        private static void DrawCodewords(bool[,] modules, bool[,] isFunction, byte[] data, int size)
        {
            int bitIndex = 0;
            int totalBits = data.Length * 8;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (int vert = 0; vert < size; vert++)
                {
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vert : vert;
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        if (!isFunction[y, x] && bitIndex < totalBits)
                        {
                            bool bit = ((data[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                            modules[y, x] = bit;
                            bitIndex++;
                        }
                    }
                }
            }
        }

        private static void ApplyMask(bool[,] modules, bool[,] isFunction, int size, int mask)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (isFunction[y, x]) continue;
                    bool invert = mask switch
                    {
                        0 => (x + y) % 2 == 0,
                        1 => y % 2 == 0,
                        2 => x % 3 == 0,
                        3 => (x + y) % 3 == 0,
                        4 => (x / 3 + y / 2) % 2 == 0,
                        5 => ((x * y) % 2) + ((x * y) % 3) == 0,
                        6 => (((x * y) % 2) + ((x * y) % 3)) % 2 == 0,
                        7 => (((x + y) % 2) + ((x * y) % 3)) % 2 == 0,
                        _ => false
                    };
                    if (invert) modules[y, x] = !modules[y, x];
                }
            }
        }

        private static void DrawFormatInfo(bool[,] modules, int size, int mask)
        {
            // ECC level L indicator = 01
            int data = (0b01 << 3) | mask;
            int rem = data;
            for (int i = 0; i < 10; i++)
            {
                rem = (rem << 1) ^ (((rem >> 9) & 1) * 0x537);
            }
            int bits = ((data << 10) | rem) ^ 0x5412;

            // Top-left format copy: bits 14..8 along row 8 (left-to-right), bit 7 at (8,8), bits 6..0 along col 8 (bottom-to-top)
            for (int i = 0; i <= 5; i++) modules[8, i] = ((bits >> (14 - i)) & 1) != 0;
            modules[8, 7] = ((bits >> 8) & 1) != 0;
            modules[8, 8] = ((bits >> 7) & 1) != 0;
            modules[7, 8] = ((bits >> 6) & 1) != 0;
            for (int i = 0; i <= 5; i++) modules[5 - i, 8] = ((bits >> (5 - i)) & 1) != 0;

            // Second format copy: bits 14..8 along col 8 (bottom-to-top), bits 7..0 along row 8 (left-to-right)
            for (int i = 0; i <= 6; i++) modules[size - 1 - i, 8] = ((bits >> (14 - i)) & 1) != 0;
            for (int i = 0; i <= 7; i++) modules[8, size - 8 + i] = ((bits >> (7 - i)) & 1) != 0;
            modules[size - 8, 8] = true;
        }

        private static int CalculatePenalty(bool[,] modules, int size)
        {
            int penalty = 0;

            // Rule 1: Adjacent same-color modules in row/col
            for (int y = 0; y < size; y++)
            {
                int runX = 1;
                int runY = 1;
                for (int x = 1; x < size; x++)
                {
                    if (modules[y, x] == modules[y, x - 1]) runX++;
                    else
                    {
                        if (runX >= 5) penalty += 3 + (runX - 5);
                        runX = 1;
                    }

                    if (modules[x, y] == modules[x - 1, y]) runY++;
                    else
                    {
                        if (runY >= 5) penalty += 3 + (runY - 5);
                        runY = 1;
                    }
                }
                if (runX >= 5) penalty += 3 + (runX - 5);
                if (runY >= 5) penalty += 3 + (runY - 5);
            }

            // Rule 2: 2x2 blocks
            for (int y = 0; y < size - 1; y++)
            {
                for (int x = 0; x < size - 1; x++)
                {
                    bool c = modules[y, x];
                    if (modules[y, x + 1] == c && modules[y + 1, x] == c && modules[y + 1, x + 1] == c)
                    {
                        penalty += 3;
                    }
                }
            }

            // Rule 4: Dark module ratio
            int darkCount = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (modules[y, x]) darkCount++;
                }
            }
            int percentDiff = Math.Abs(darkCount * 100 / (size * size) - 50) / 5;
            penalty += percentDiff * 10;

            return penalty;
        }

        private static void SetFunctionModule(bool[,] modules, bool[,] isFunction, int x, int y, bool dark)
        {
            modules[y, x] = dark;
            isFunction[y, x] = true;
        }

        private static void AppendBits(List<bool> bits, uint value, int bitCount)
        {
            for (int i = bitCount - 1; i >= 0; i--)
            {
                bits.Add(((value >> i) & 1) != 0);
            }
        }
    }
}
