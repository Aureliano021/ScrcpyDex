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

namespace ScrcpyDex.Core.Platform
{
    /// <summary>
    /// Provides platform-specific storage and configuration paths.
    /// </summary>
    public interface IPlatformPaths
    {
        string GetConfigDirectory();
        string GetLogDirectory();
        string GetCacheDirectory();
    }

    /// <summary>
    /// Default cross-platform implementation using XDG standards on Linux/macOS
    /// and standard AppData locations on Windows.
    /// </summary>
    public class DefaultPlatformPaths : IPlatformPaths
    {
        public static DefaultPlatformPaths Instance { get; } = new();

        public virtual string GetConfigDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "ScrcpyDeX");
            }

            // Linux / macOS XDG specification
            string? xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdgConfig))
            {
                return Path.Combine(xdgConfig, "scrcpydex");
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "scrcpydex");
        }

        public virtual string GetLogDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "ScrcpyDeX", "Logs");
            }

            string? xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (!string.IsNullOrWhiteSpace(xdgState))
            {
                return Path.Combine(xdgState, "scrcpydex", "logs");
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "state", "scrcpydex", "logs");
        }

        public virtual string GetCacheDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "ScrcpyDeX", "Cache");
            }

            string? xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrWhiteSpace(xdgCache))
            {
                return Path.Combine(xdgCache, "scrcpydex");
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".cache", "scrcpydex");
        }
    }
}
