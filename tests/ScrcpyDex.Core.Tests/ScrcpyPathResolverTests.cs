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
using ScrcpyDex.Core.IO;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class ScrcpyPathResolverTests
    {
        [Theory]
        [InlineData("scrcpy 2.4 <https://github.com/Genymobile/scrcpy>", "2.4")]
        [InlineData("scrcpy 5.0", "5.0")]
        [InlineData("scrcpy v3.1", "3.1")]
        [InlineData("scrcpy 2.4.1", "2.4.1")]
        public void ParseScrcpyVersion_WithValidVersionStrings_ReturnsExpectedVersion(string input, string expectedVersion)
        {
            Version? result = ScrcpyPathResolver.ParseScrcpyVersion(input);

            Assert.NotNull(result);
            Assert.Equal(new Version(expectedVersion), result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a version")]
        [InlineData("scrcpy version unknown")]
        [InlineData("scrcpy")]
        public void ParseScrcpyVersion_WithInvalidOrEmptyStrings_ReturnsNull(string? input)
        {
            Version? result = ScrcpyPathResolver.ParseScrcpyVersion(input);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("2.4", true)]
        [InlineData("2.4.1", true)]
        [InlineData("3.0", true)]
        [InlineData("5.0", true)]
        [InlineData("2.3.9", false)]
        [InlineData("2.3", false)]
        [InlineData("1.25", false)]
        public void IsScrcpyVersionSupported_EvaluatesMinimumVersionCorrectly(string versionString, bool expected)
        {
            var version = new Version(versionString);
            bool isSupported = ScrcpyPathResolver.IsScrcpyVersionSupported(version);

            Assert.Equal(expected, isSupported);
        }

        [Fact]
        public void IsScrcpyVersionSupported_WithNullVersion_ReturnsFalse()
        {
            bool isSupported = ScrcpyPathResolver.IsScrcpyVersionSupported(null);

            Assert.False(isSupported);
        }

        [Fact]
        public void TryResolveAdbExecutable_WithSiblingAdbNextToScrcpy_ResolvesSiblingAdb()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ScrcpyDeX_Test_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                string scrcpyBin = OperatingSystem.IsWindows() ? "scrcpy.exe" : "scrcpy";
                string adbBin = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
                string fakeScrcpy = System.IO.Path.Combine(tempDir, scrcpyBin);
                string fakeAdb = System.IO.Path.Combine(tempDir, adbBin);

                System.IO.File.WriteAllText(fakeScrcpy, "dummy");
                System.IO.File.WriteAllText(fakeAdb, "dummy");

                bool found = ScrcpyPathResolver.TryResolveAdbExecutable(out string resolvedAdb, customPath: null, scrcpyPath: fakeScrcpy);

                Assert.True(found);
                Assert.Equal(System.IO.Path.GetFullPath(fakeAdb), resolvedAdb);
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void TryResolveScrcpyExecutable_WithCustomDirectoryPath_ResolvesScrcpyInsideDirectory()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ScrcpyDeX_Test_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                string scrcpyBin = OperatingSystem.IsWindows() ? "scrcpy.exe" : "scrcpy";
                string fakeScrcpy = System.IO.Path.Combine(tempDir, scrcpyBin);
                System.IO.File.WriteAllText(fakeScrcpy, "dummy");

                bool found = ScrcpyPathResolver.TryResolveScrcpyExecutable(out string resolvedScrcpy, customPath: tempDir);

                Assert.True(found);
                Assert.Equal(System.IO.Path.GetFullPath(fakeScrcpy), resolvedScrcpy);
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
