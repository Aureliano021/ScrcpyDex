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
using System.IO;
using System.Linq;

namespace ScrcpyDex.Core.IO
{
    /// <summary>
    /// Dynamically resolves filesystem paths for scrcpy binaries, adb, and scrcpydex-server.jar.
    /// Traverses execution base directories, repo roots, PATH, and standard Windows package manager locations.
    /// </summary>
    public static class ScrcpyPathResolver
    {
        private static readonly string[] ExecutableExtensions = { ".exe", "" };

        /// <summary>
        /// Resolves the absolute path to scrcpy.exe.
        /// </summary>
        public static string ResolveScrcpyExecutable(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }

            string baseDir = AppContext.BaseDirectory;

            // 1. Check relative to base directory and upward directory hierarchy (up to 5 levels)
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                string[] candidates =
                {
                    Path.Combine(dir.FullName, "scrcpy", "scrcpy.exe"),
                    Path.Combine(dir.FullName, "scrcpy.exe"),
                    Path.Combine(dir.FullName, "tools", "scrcpy", "scrcpy.exe"),
                    Path.Combine(dir.FullName, "tools", "scrcpy.exe"),
                    Path.Combine(dir.FullName, "bin", "scrcpy", "scrcpy.exe")
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }

                dir = dir.Parent;
            }

            // 2. Search in PATH
            string? pathFromEnv = FindInPath("scrcpy");
            if (pathFromEnv != null) return pathFromEnv;

            // 3. Search WinGet Packages
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                string wingetPackagesDir = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
                if (Directory.Exists(wingetPackagesDir))
                {
                    try
                    {
                        var scrcpyDir = Directory.GetDirectories(wingetPackagesDir, "Genymobile.scrcpy*")
                            .FirstOrDefault();
                        if (scrcpyDir != null)
                        {
                            var match = Directory.GetFiles(scrcpyDir, "scrcpy.exe", SearchOption.AllDirectories)
                                .FirstOrDefault();
                            if (match != null) return Path.GetFullPath(match);
                        }
                    }
                    catch { }
                }

                string wingetLinks = Path.Combine(localAppData, "Microsoft", "WinGet", "Links", "scrcpy.exe");
                if (File.Exists(wingetLinks)) return Path.GetFullPath(wingetLinks);
            }

            // 4. Search Program Files
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                string pfScrcpy = Path.Combine(programFiles, "scrcpy", "scrcpy.exe");
                if (File.Exists(pfScrcpy)) return Path.GetFullPath(pfScrcpy);
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                string pfScrcpy86 = Path.Combine(programFilesX86, "scrcpy", "scrcpy.exe");
                if (File.Exists(pfScrcpy86)) return Path.GetFullPath(pfScrcpy86);
            }

            // Fallback
            return "scrcpy.exe";
        }

        /// <summary>
        /// Resolves the absolute path to adb.exe.
        /// </summary>
        public static string ResolveAdbExecutable(string? customPath = null, string? scrcpyPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }

            // 1. Sibling to resolved scrcpy executable (official releases bundle adb together)
            if (!string.IsNullOrWhiteSpace(scrcpyPath) && File.Exists(scrcpyPath))
            {
                string? scrcpyDir = Path.GetDirectoryName(scrcpyPath);
                if (!string.IsNullOrEmpty(scrcpyDir))
                {
                    string siblingAdb = Path.Combine(scrcpyDir, "adb.exe");
                    if (File.Exists(siblingAdb)) return Path.GetFullPath(siblingAdb);
                }
            }

            string baseDir = AppContext.BaseDirectory;

            // 2. Search upward directory hierarchy
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                string[] candidates =
                {
                    Path.Combine(dir.FullName, "scrcpy", "adb.exe"),
                    Path.Combine(dir.FullName, "tools", "adb.exe"),
                    Path.Combine(dir.FullName, "platform-tools", "adb.exe"),
                    Path.Combine(dir.FullName, "adb.exe")
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }

                dir = dir.Parent;
            }

            // 3. Search in PATH
            string? pathFromEnv = FindInPath("adb");
            if (pathFromEnv != null) return pathFromEnv;

            // 4. Android SDK Platform Tools
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                string sdkAdb = Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe");
                if (File.Exists(sdkAdb)) return Path.GetFullPath(sdkAdb);

                string wingetPackagesDir = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
                if (Directory.Exists(wingetPackagesDir))
                {
                    try
                    {
                        var scrcpyDir = Directory.GetDirectories(wingetPackagesDir, "Genymobile.scrcpy*")
                            .FirstOrDefault();
                        if (scrcpyDir != null)
                        {
                            var match = Directory.GetFiles(scrcpyDir, "adb.exe", SearchOption.AllDirectories)
                                .FirstOrDefault();
                            if (match != null) return Path.GetFullPath(match);
                        }
                    }
                    catch { }
                }

                string wingetLinks = Path.Combine(localAppData, "Microsoft", "WinGet", "Links", "adb.exe");
                if (File.Exists(wingetLinks)) return Path.GetFullPath(wingetLinks);
            }

            return "adb.exe";
        }

        /// <summary>
        /// Resolves the absolute path to scrcpydex-server.jar.
        /// </summary>
        public static string? ResolveServerJar(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                return Path.GetFullPath(customPath);
            }

            string baseDir = AppContext.BaseDirectory;

            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                string[] candidates =
                {
                    Path.Combine(dir.FullName, "server", "scrcpydex-server.jar"),
                    Path.Combine(dir.FullName, "server", "build", "scrcpydex-server.jar"),
                    Path.Combine(dir.FullName, "scrcpydex-server.jar"),
                    Path.Combine(dir.FullName, "client", "server", "scrcpydex-server.jar")
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }

                dir = dir.Parent;
            }

            return null;
        }

        /// <summary>
        /// Locates the application workspace root directory.
        /// </summary>
        public static string ResolveAppRootDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "server")) &&
                    (Directory.Exists(Path.Combine(dir.FullName, "client")) || File.Exists(Path.Combine(dir.FullName, "run-scrcpydex.ps1"))))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return AppContext.BaseDirectory;
        }

        private static string? FindInPath(string binaryName)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv)) return null;

            char pathSep = Path.PathSeparator;
            string[] paths = pathEnv.Split(pathSep, StringSplitOptions.RemoveEmptyEntries);

            foreach (string p in paths)
            {
                try
                {
                    string trimmed = p.Trim();
                    if (string.IsNullOrEmpty(trimmed) || !Directory.Exists(trimmed)) continue;

                    foreach (string ext in ExecutableExtensions)
                    {
                        string candidate = Path.Combine(trimmed, binaryName + ext);
                        if (File.Exists(candidate))
                        {
                            return Path.GetFullPath(candidate);
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
