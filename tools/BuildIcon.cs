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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ScrcpyDeX.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string inputJpg = @"C:\Users\aurel\.gemini\antigravity\brain\c2534b4e-21ee-4609-bce2-3fccb40f8d70\.user_uploaded\media_1790430875843.jpg";
            
            if (!File.Exists(inputJpg))
            {
                Console.WriteLine("Input image not found: " + inputJpg);
                return;
            }

            using (Bitmap src = new Bitmap(inputJpg))
            {
                // Bounding box of monitor: minX=335, maxX=689, minY=89, maxY=398
                // Center is X=512, Y=244. Size is 354x309.
                int centerX = 512;
                int centerY = 244;
                int cropBox = 380; // provides ~13px padding around 354px monitor width
                int startX = centerX - cropBox / 2; // 322
                int startY = centerY - cropBox / 2; // 54

                using (Bitmap cropped = new Bitmap(cropBox, cropBox, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(cropped))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(src, new Rectangle(0, 0, cropBox, cropBox), startX, startY, cropBox, cropBox, GraphicsUnit.Pixel);
                    }

                    // Perform BFS Flood Fill from edges to transparentize outside background
                    int w = cropped.Width;
                    int h = cropped.Height;
                    bool[,] visited = new bool[w, h];
                    Queue<Point> q = new Queue<Point>();

                    for (int x = 0; x < w; x++)
                    {
                        q.Enqueue(new Point(x, 0));
                        q.Enqueue(new Point(x, h - 1));
                        visited[x, 0] = true;
                        visited[x, h - 1] = true;
                    }
                    for (int y = 0; y < h; y++)
                    {
                        q.Enqueue(new Point(0, y));
                        q.Enqueue(new Point(w - 1, y));
                        visited[0, y] = true;
                        visited[w - 1, y] = true;
                    }

                    while (q.Count > 0)
                    {
                        Point p = q.Dequeue();
                        Color c = cropped.GetPixel(p.X, p.Y);
                        int brightness = (c.R + c.G + c.B) / 3;

                        // Monitor frame dark boundary is R~41, G~48, B~54 (brightness ~48).
                        // Background outside is R~230, G~235, B~236 (brightness ~234).
                        if (brightness > 130)
                        {
                            cropped.SetPixel(p.X, p.Y, Color.FromArgb(0, 0, 0, 0));

                            int[] dx = { 1, -1, 0, 0 };
                            int[] dy = { 0, 0, 1, -1 };
                            for (int i = 0; i < 4; i++)
                            {
                                int nx = p.X + dx[i];
                                int ny = p.Y + dy[i];
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                                {
                                    visited[nx, ny] = true;
                                    q.Enqueue(new Point(nx, ny));
                                }
                            }
                        }
                    }

                    // Enhance screen background inside the monitor (make it clean white for maximum pop)
                    // Any non-transparent pixel inside the screen that is light gray (R>200, G>200, B>200) becomes #FFFFFF
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            Color c = cropped.GetPixel(x, y);
                            if (c.A > 0)
                            {
                                // If it's the light screen background (not the green android or dark border)
                                if (c.R > 210 && c.G > 215 && c.B > 215)
                                {
                                    cropped.SetPixel(x, y, Color.FromArgb(255, 255, 255, 255));
                                }
                            }
                        }
                    }

                    string assetsDir = Path.Combine(baseDir, "..", "assets");
                    if (!Directory.Exists(assetsDir))
                    {
                        Directory.CreateDirectory(assetsDir);
                    }
                    assetsDir = Path.GetFullPath(assetsDir);

                    // 1. Save 256x256 Master PNG
                    using (Bitmap icon256 = ResizeBitmap(cropped, 256, 256))
                    {
                        string pngPath = Path.Combine(assetsDir, "app_icon.png");
                        icon256.Save(pngPath, ImageFormat.Png);
                        Console.WriteLine("Saved: " + pngPath);

                        // Also save in root as icon.png
                        string rootPng = Path.Combine(baseDir, "..", "icon.png");
                        icon256.Save(rootPng, ImageFormat.Png);
                    }

                    // 2. Build multi-resolution ICO file
                    int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
                    List<byte[]> pngBuffers = new List<byte[]>();

                    foreach (int s in sizes)
                    {
                        using (Bitmap resized = ResizeBitmap(cropped, s, s))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                resized.Save(ms, ImageFormat.Png);
                                pngBuffers.Add(ms.ToArray());
                            }
                        }
                    }

                    string icoPath = Path.Combine(assetsDir, "app_icon.ico");
                    string rootIco = Path.Combine(baseDir, "..", "icon.ico");
                    WriteIcoFile(sizes, pngBuffers, icoPath);
                    WriteIcoFile(sizes, pngBuffers, rootIco);
                    Console.WriteLine("Saved: " + icoPath);
                    Console.WriteLine("Saved: " + rootIco);
                }
            }
        }

        static Bitmap ResizeBitmap(Bitmap src, int width, int height)
        {
            Bitmap dest = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, width, height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);
            }
            return dest;
        }

        static void WriteIcoFile(int[] sizes, List<byte[]> pngBuffers, string outputPath)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((ushort)0); // idReserved
                bw.Write((ushort)1); // idType (1 = ICO)
                bw.Write((ushort)sizes.Length); // idCount

                // Calculate image data offsets
                int offset = 6 + (sizes.Length * 16);

                // ICONDIRENTRY entries
                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    byte bWidth = (sz >= 256) ? (byte)0 : (byte)sz;
                    byte bHeight = (sz >= 256) ? (byte)0 : (byte)sz;

                    bw.Write(bWidth);
                    bw.Write(bHeight);
                    bw.Write((byte)0); // bColorCount
                    bw.Write((byte)0); // bReserved
                    bw.Write((ushort)1); // wPlanes
                    bw.Write((ushort)32); // wBitCount
                    bw.Write((uint)pngBuffers[i].Length); // dwBytesInRes
                    bw.Write((uint)offset); // dwImageOffset

                    offset += pngBuffers[i].Length;
                }

                // Write PNG image bytes for each entry
                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngBuffers[i]);
                }
            }
        }
    }
}
