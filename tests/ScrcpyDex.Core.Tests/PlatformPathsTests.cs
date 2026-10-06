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
using ScrcpyDex.Core.Platform;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class PlatformPathsTests
    {
        [Fact]
        public void Instance_ReturnsNonNullSingleton()
        {
            var paths = DefaultPlatformPaths.Instance;

            Assert.NotNull(paths);
        }

        [Fact]
        public void GetConfigDirectory_ReturnsNonEmptyPath()
        {
            string configDir = DefaultPlatformPaths.Instance.GetConfigDirectory();

            Assert.False(string.IsNullOrWhiteSpace(configDir));
            Assert.Contains(OperatingSystem.IsWindows() ? "ScrcpyDeX" : "scrcpydex", configDir, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetLogDirectory_ReturnsNonEmptyPath()
        {
            string logDir = DefaultPlatformPaths.Instance.GetLogDirectory();

            Assert.False(string.IsNullOrWhiteSpace(logDir));
            Assert.Contains(OperatingSystem.IsWindows() ? "Logs" : "logs", logDir, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetCacheDirectory_ReturnsNonEmptyPath()
        {
            string cacheDir = DefaultPlatformPaths.Instance.GetCacheDirectory();

            Assert.False(string.IsNullOrWhiteSpace(cacheDir));
            Assert.Contains(OperatingSystem.IsWindows() ? "Cache" : "scrcpydex", cacheDir, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Directories_AreWellFormedPathStrings()
        {
            var paths = DefaultPlatformPaths.Instance;

            string configDir = paths.GetConfigDirectory();
            string logDir = paths.GetLogDirectory();
            string cacheDir = paths.GetCacheDirectory();

            Assert.True(Path.IsPathRooted(configDir));
            Assert.True(Path.IsPathRooted(logDir));
            Assert.True(Path.IsPathRooted(cacheDir));
        }
    }
}
