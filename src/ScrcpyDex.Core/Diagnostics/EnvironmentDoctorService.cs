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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ScrcpyDex.Core.IO;

namespace ScrcpyDex.Core.Diagnostics
{
    public enum DiagnosticStatus
    {
        Pass,
        Warning,
        Error,
        Info
    }

    public sealed class DiagnosticItem
    {
        public string Name { get; set; } = string.Empty;
        public DiagnosticStatus Status { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? Remediation { get; set; }
    }

    public sealed class DiagnosticReport
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string OsDescription { get; set; } = RuntimeInformation.OSDescription;
        public List<DiagnosticItem> Items { get; set; } = new();

        public bool HasErrors => Items.Any(i => i.Status == DiagnosticStatus.Error);
        public bool HasWarnings => Items.Any(i => i.Status == DiagnosticStatus.Warning);
        public int PassCount => Items.Count(i => i.Status == DiagnosticStatus.Pass);
        public int WarningCount => Items.Count(i => i.Status == DiagnosticStatus.Warning);
        public int ErrorCount => Items.Count(i => i.Status == DiagnosticStatus.Error);
    }

    /// <summary>
    /// Cross-platform environment diagnostic service for auditing scrcpy, adb, and server JAR readiness.
    /// Shared between the desktop GUI and CLI doctor command.
    /// </summary>
    public sealed class EnvironmentDoctorService
    {
        public static EnvironmentDoctorService Instance { get; } = new();

        public DiagnosticItem CheckScrcpyVersion(string? customScrcpyPath = null)
        {
            if (!ScrcpyPathResolver.TryResolveScrcpyExecutable(out string path, customScrcpyPath))
            {
                return new DiagnosticItem
                {
                    Name = "scrcpy Executable",
                    Status = DiagnosticStatus.Error,
                    Summary = "scrcpy binary could not be located in system PATH or standard directories.",
                    Details = "ScrcpyDeX depends on the scrcpy streaming engine.",
                    Remediation = OperatingSystem.IsWindows()
                        ? "Install official scrcpy (>= 2.4) or select scrcpy.exe using the Scrcpy path picker."
                        : "Install official scrcpy (version >= 2.4):\n  Download official release or deploy to /usr/local/bin/scrcpy."
                };
            }

            var ver = ScrcpyPathResolver.QueryScrcpyVersion(path);
            if (ver == null)
            {
                return new DiagnosticItem
                {
                    Name = "scrcpy Executable",
                    Status = DiagnosticStatus.Warning,
                    Summary = $"scrcpy located at '{path}', but version output could not be parsed.",
                    Details = "Could not confirm whether scrcpy satisfies >= 2.4.0 version requirement."
                };
            }

            if (ver >= new Version(2, 4))
            {
                return new DiagnosticItem
                {
                    Name = "scrcpy Executable",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"scrcpy version {ver} satisfies requirement (>= 2.4.0).",
                    Details = $"Resolved path: {path}"
                };
            }

            return new DiagnosticItem
            {
                Name = "scrcpy Executable",
                Status = DiagnosticStatus.Error,
                Summary = $"scrcpy version {ver} is incompatible (required: >= 2.4.0).",
                Details = $"Binary located at '{path}'. Upstream version < 2.4 lacks --mouse=uhid and --display-id flags.",
                Remediation = "Install official upstream scrcpy >= 2.4."
            };
        }

        public DiagnosticItem CheckAdbInstallation(string? customAdbPath = null, string? customScrcpyPath = null)
        {
            string? effectiveScrcpyPath = customScrcpyPath;
            if (string.IsNullOrWhiteSpace(effectiveScrcpyPath))
            {
                ScrcpyPathResolver.TryResolveScrcpyExecutable(out string resolvedScrcpy);
                effectiveScrcpyPath = resolvedScrcpy;
            }

            if (ScrcpyPathResolver.TryResolveAdbExecutable(out string path, customAdbPath, effectiveScrcpyPath))
            {
                return new DiagnosticItem
                {
                    Name = "Android Debug Bridge (ADB)",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"adb located at '{path}'.",
                    Details = "Host-device communication channel ready."
                };
            }

            return new DiagnosticItem
            {
                Name = "Android Debug Bridge (ADB)",
                Status = DiagnosticStatus.Error,
                Summary = "adb binary could not be resolved.",
                Details = "Required for device enumeration and DeX server deployment.",
                Remediation = OperatingSystem.IsWindows()
                    ? "Ensure adb.exe is in PATH, alongside scrcpy.exe, or install Android platform-tools."
                    : "Install ADB:\n  sudo apt install adb (or install Android platform-tools)."
            };
        }

        public DiagnosticItem CheckServerJarInstallation(string? customServerJarPath = null)
        {
            if (ScrcpyPathResolver.TryResolveServerJar(out string path, customServerJarPath) && File.Exists(path))
            {
                var fi = new FileInfo(path);
                return new DiagnosticItem
                {
                    Name = "ScrcpyDeX Server JAR",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"Server JAR located at '{path}'.",
                    Details = $"Artifact size: {fi.Length} bytes."
                };
            }

            return new DiagnosticItem
            {
                Name = "ScrcpyDeX Server JAR",
                Status = DiagnosticStatus.Error,
                Summary = "scrcpydex-server.jar could not be located or extracted.",
                Details = "Required for on-device DeX reflection activation.",
                Remediation = "Verify that scrcpydex-server.jar is present in the server directory or application bundle."
            };
        }

        public DiagnosticReport RunCrossPlatformChecks(
            string? customScrcpyPath = null,
            string? customAdbPath = null,
            string? customServerJarPath = null)
        {
            var report = new DiagnosticReport();
            report.Items.Add(CheckScrcpyVersion(customScrcpyPath));
            report.Items.Add(CheckAdbInstallation(customAdbPath, customScrcpyPath));
            report.Items.Add(CheckServerJarInstallation(customServerJarPath));
            return report;
        }
    }
}
