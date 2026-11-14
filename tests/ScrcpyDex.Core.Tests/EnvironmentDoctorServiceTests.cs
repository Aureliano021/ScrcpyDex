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
using ScrcpyDex.Core.Diagnostics;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class EnvironmentDoctorServiceTests
    {
        [Fact]
        public void DiagnosticReport_ComputesStatusCountsAccurately()
        {
            var report = new DiagnosticReport
            {
                Items =
                {
                    new DiagnosticItem { Name = "A", Status = DiagnosticStatus.Pass },
                    new DiagnosticItem { Name = "B", Status = DiagnosticStatus.Warning },
                    new DiagnosticItem { Name = "C", Status = DiagnosticStatus.Error },
                    new DiagnosticItem { Name = "D", Status = DiagnosticStatus.Pass }
                }
            };

            Assert.True(report.HasErrors);
            Assert.True(report.HasWarnings);
            Assert.Equal(2, report.PassCount);
            Assert.Equal(1, report.WarningCount);
            Assert.Equal(1, report.ErrorCount);
        }

        [Fact]
        public void CheckAdbInstallation_WithExistingCustomFile_ReturnsPass()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ScrcpyDeX_DoctorTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string adbName = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
                string fakeAdb = Path.Combine(tempDir, adbName);
                File.WriteAllText(fakeAdb, "dummy");

                DiagnosticItem item = EnvironmentDoctorService.Instance.CheckAdbInstallation(customAdbPath: fakeAdb);

                Assert.Equal(DiagnosticStatus.Pass, item.Status);
                Assert.Contains(Path.GetFullPath(fakeAdb), item.Summary);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void RunCrossPlatformChecks_ReturnsExpectedThreeCoreChecks()
        {
            DiagnosticReport report = EnvironmentDoctorService.Instance.RunCrossPlatformChecks();

            Assert.Equal(3, report.Items.Count);
            Assert.Equal("scrcpy Executable", report.Items[0].Name);
            Assert.Equal("Android Debug Bridge (ADB)", report.Items[1].Name);
            Assert.Equal("ScrcpyDeX Server JAR", report.Items[2].Name);
            Assert.Equal(report.ErrorCount > 0, report.HasErrors);
            Assert.Equal(report.WarningCount > 0, report.HasWarnings);
        }
    }
}
