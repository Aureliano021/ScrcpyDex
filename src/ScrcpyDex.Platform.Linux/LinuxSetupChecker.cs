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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ScrcpyDex.Core.IO;

namespace ScrcpyDex.Platform.Linux
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
    /// Performs Linux system environment diagnostics and hardware acceleration audits.
    /// Evaluates udev rules, group memberships, display server configuration, VA-API drivers, and tool versions.
    /// </summary>
    public sealed class LinuxSetupChecker
    {
        private static readonly string[] UdevRuleCandidatePaths =
        {
            "/etc/udev/rules.d/51-android.rules",
            "/lib/udev/rules.d/51-android.rules",
            "/usr/lib/udev/rules.d/51-android.rules"
        };

        private static readonly string[] DriDriverSearchPaths =
        {
            "/usr/lib/x86_64-linux-gnu/dri",
            "/usr/lib/aarch64-linux-gnu/dri",
            "/usr/lib/arm-linux-gnueabihf/dri",
            "/usr/lib64/dri",
            "/usr/lib/dri"
        };

        public static LinuxSetupChecker Instance { get; } = new();

        /// <summary>
        /// Audits udev rules for Android USB device permissions (Vendor ID 04e8 - Samsung).
        /// </summary>
        public DiagnosticItem CheckUdevRules()
        {
            if (!OperatingSystem.IsLinux())
            {
                return new DiagnosticItem
                {
                    Name = "Udev Rules",
                    Status = DiagnosticStatus.Info,
                    Summary = "Udev checks apply exclusively to Linux hosts.",
                    Details = $"Current OS: {RuntimeInformation.OSDescription}"
                };
            }

            string? existingPath = UdevRuleCandidatePaths.FirstOrDefault(File.Exists);

            if (existingPath == null)
            {
                return new DiagnosticItem
                {
                    Name = "Udev Rules",
                    Status = DiagnosticStatus.Warning,
                    Summary = "Android udev rules file (51-android.rules) not found.",
                    Details = "Without udev rules, ADB may encounter 'no permissions' when accessing USB devices.",
                    Remediation = "Install Android udev rules:\n" +
                                  "  sudo cp packaging/linux/51-android.rules /etc/udev/rules.d/51-android.rules\n" +
                                  "  sudo udevadm control --reload-rules && sudo udevadm trigger"
                };
            }

            try
            {
                string content = File.ReadAllText(existingPath);
                bool hasSamsungVendor = Regex.IsMatch(content, @"04e8", RegexOptions.IgnoreCase);
                bool hasPlugdevOrUaccess = content.Contains("plugdev", StringComparison.OrdinalIgnoreCase) ||
                                          content.Contains("uaccess", StringComparison.OrdinalIgnoreCase);

                if (hasSamsungVendor)
                {
                    return new DiagnosticItem
                    {
                        Name = "Udev Rules",
                        Status = DiagnosticStatus.Pass,
                        Summary = $"Samsung vendor rules confirmed in {existingPath}.",
                        Details = hasPlugdevOrUaccess
                            ? "Permission target: plugdev / uaccess configured."
                            : "Samsung vendor ID 04e8 matched."
                    };
                }
                else
                {
                    return new DiagnosticItem
                    {
                        Name = "Udev Rules",
                        Status = DiagnosticStatus.Warning,
                        Summary = $"Rules found at {existingPath}, but Samsung Vendor ID (04e8) is missing.",
                        Details = "Devices from Samsung may not receive automatic read/write USB node permissions.",
                        Remediation = "Append Samsung rule to /etc/udev/rules.d/51-android.rules:\n" +
                                      "  SUBSYSTEM==\"usb\", ATTR{idVendor}==\"04e8\", MODE=\"0660\", GROUP=\"plugdev\", TAG+=\"uaccess\"\n" +
                                      "Then reload: sudo udevadm control --reload-rules && sudo udevadm trigger"
                    };
                }
            }
            catch (Exception ex)
            {
                return new DiagnosticItem
                {
                    Name = "Udev Rules",
                    Status = DiagnosticStatus.Warning,
                    Summary = $"Rule file found at {existingPath}, but could not be read ({ex.Message}).",
                    Details = ex.Message
                };
            }
        }

        /// <summary>
        /// Audits whether the current user is a member of 'plugdev' or 'adbusers'.
        /// </summary>
        public DiagnosticItem CheckUserGroupMembership()
        {
            if (!OperatingSystem.IsLinux())
            {
                return new DiagnosticItem
                {
                    Name = "User Group Membership",
                    Status = DiagnosticStatus.Info,
                    Summary = "Group membership checks apply exclusively to Linux hosts.",
                    Details = $"Current OS: {RuntimeInformation.OSDescription}"
                };
            }

            string user = Environment.UserName;
            if (string.Equals(user, "root", StringComparison.OrdinalIgnoreCase))
            {
                return new DiagnosticItem
                {
                    Name = "User Group Membership",
                    Status = DiagnosticStatus.Pass,
                    Summary = "Running under root user context (UID 0).",
                    Details = "Root context holds universal device node permissions."
                };
            }

            var matchedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Attempt reading groups via id command
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "id",
                    Arguments = "-Gn",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(1500);

                    foreach (var grp in output.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        matchedGroups.Add(grp);
                    }
                }
            }
            catch { }

            // 2. Fallback: Parse /etc/group
            if (matchedGroups.Count == 0 && File.Exists("/etc/group"))
            {
                try
                {
                    foreach (var line in File.ReadLines("/etc/group"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length >= 4)
                        {
                            string groupName = parts[0];
                            string members = parts[3];
                            var memberList = members.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (memberList.Any(m => string.Equals(m.Trim(), user, StringComparison.OrdinalIgnoreCase)))
                            {
                                matchedGroups.Add(groupName);
                            }
                        }
                    }
                }
                catch { }
            }

            bool inPlugdev = matchedGroups.Contains("plugdev");
            bool inAdbusers = matchedGroups.Contains("adbusers");

            if (inPlugdev || inAdbusers)
            {
                string found = inPlugdev && inAdbusers ? "plugdev, adbusers" : (inPlugdev ? "plugdev" : "adbusers");
                return new DiagnosticItem
                {
                    Name = "User Group Membership",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"User '{user}' belongs to required USB access group: {found}.",
                    Details = "Unprivileged USB access to connected devices is granted."
                };
            }

            return new DiagnosticItem
            {
                Name = "User Group Membership",
                Status = DiagnosticStatus.Warning,
                Summary = $"User '{user}' is not in 'plugdev' or 'adbusers' groups.",
                Details = "Non-root users may encounter ADB permission errors without group membership.",
                Remediation = "Add your user to the plugdev group:\n" +
                              "  sudo usermod -aG plugdev $USER\n" +
                              "(Log out and log back in for changes to take effect)"
            };
        }

        /// <summary>
        /// Audits display server session (X11 vs Wayland).
        /// </summary>
        public DiagnosticItem CheckDisplayServer()
        {
            string? display = Environment.GetEnvironmentVariable("DISPLAY");
            string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            string? sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");

            bool isWayland = !string.IsNullOrWhiteSpace(waylandDisplay) ||
                             string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase);

            if (isWayland)
            {
                return new DiagnosticItem
                {
                    Name = "Display Server",
                    Status = DiagnosticStatus.Warning,
                    Summary = $"Wayland desktop session detected (WAYLAND_DISPLAY={waylandDisplay ?? "unknown"}).",
                    Details = "X11 / XWayland is the certified baseline. Under native Wayland, desktop modifier shortcuts " +
                              "(such as Super/Windows key) and UHID mouse capture may be constrained by compositor security policies.",
                    Remediation = "For full desktop shortcut passthrough and pointer grab, select an X11 (or X.Org) session at login."
                };
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                return new DiagnosticItem
                {
                    Name = "Display Server",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"X11 desktop session active (DISPLAY={display}).",
                    Details = "X11 satisfies the certified ScrcpyDeX deployment baseline."
                };
            }

            return new DiagnosticItem
            {
                Name = "Display Server",
                Status = DiagnosticStatus.Warning,
                Summary = "No active graphical display detected in environment ($DISPLAY is unset).",
                Details = "Running in a headless console or remote shell without X11 forwarding.",
                Remediation = "Run within an active graphical desktop terminal, or export DISPLAY=:0."
            };
        }

        /// <summary>
        /// Audits hardware video acceleration drivers (VA-API).
        /// </summary>
        public DiagnosticItem CheckVaApiDriver()
        {
            if (!OperatingSystem.IsLinux())
            {
                return new DiagnosticItem
                {
                    Name = "VA-API Hardware Video Acceleration",
                    Status = DiagnosticStatus.Info,
                    Summary = "VA-API driver checks apply exclusively to Linux hosts.",
                    Details = $"Current OS: {RuntimeInformation.OSDescription}"
                };
            }

            string? driverEnv = Environment.GetEnvironmentVariable("LIBVA_DRIVER_NAME");
            var foundDrivers = new List<string>();

            foreach (var dir in DriDriverSearchPaths)
            {
                if (!Directory.Exists(dir)) continue;

                try
                {
                    var files = Directory.GetFiles(dir, "*_drv_video.so");
                    foreach (var f in files)
                    {
                        foundDrivers.Add(Path.GetFileName(f));
                    }
                }
                catch { }
            }

            bool drmRenderNodeExists = File.Exists("/dev/dri/renderD128") || Directory.Exists("/dev/dri");

            if (foundDrivers.Count > 0 || !string.IsNullOrWhiteSpace(driverEnv))
            {
                string envNote = !string.IsNullOrWhiteSpace(driverEnv) ? $" ($LIBVA_DRIVER_NAME={driverEnv})" : string.Empty;
                string driversList = foundDrivers.Count > 0 ? string.Join(", ", foundDrivers.Distinct()) : "Driver directory present";

                string archNote = string.Empty;
                if (foundDrivers.Any(d => d.Contains("i965", StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(driverEnv, "i965", StringComparison.OrdinalIgnoreCase))
                {
                    archNote = " (Note: Intel i965 driver supports H.264 hardware decode. HEVC will fall back to CPU decoding.)";
                }

                return new DiagnosticItem
                {
                    Name = "VA-API Hardware Video Acceleration",
                    Status = DiagnosticStatus.Pass,
                    Summary = $"VA-API driver available{envNote}. Drivers found: {driversList}.{archNote}",
                    Details = drmRenderNodeExists
                        ? "Direct Rendering Manager device node (/dev/dri) confirmed."
                        : "DRM device node not detected."
                };
            }

            return new DiagnosticItem
            {
                Name = "VA-API Hardware Video Acceleration",
                Status = DiagnosticStatus.Warning,
                Summary = "No VA-API video acceleration drivers located in standard DRI directories.",
                Details = "Streaming will fall back to CPU software video decoding.",
                Remediation = "Install VA-API drivers:\n" +
                              "  sudo apt install va-driver-all i965-va-driver intel-media-va-driver vainfo mesa-va-drivers"
            };
        }

        /// <summary>
        /// Audits scrcpy binary and validates version >= 2.4.0.
        /// </summary>
        public DiagnosticItem CheckScrcpyVersion()
        {
            if (!ScrcpyPathResolver.TryResolveScrcpyExecutable(out string path))
            {
                return new DiagnosticItem
                {
                    Name = "scrcpy Executable",
                    Status = DiagnosticStatus.Error,
                    Summary = "scrcpy binary could not be located in system PATH or standard directories.",
                    Details = "ScrcpyDeX depends on the scrcpy streaming engine.",
                    Remediation = "Install official scrcpy (version >= 2.4):\n" +
                                  "  Download official release or deploy to /usr/local/bin/scrcpy."
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
                Remediation = "The default Ubuntu package (scrcpy 1.25) is incompatible.\n" +
                              "Install official upstream scrcpy >= 2.4 or deploy official binary to /usr/local/bin/scrcpy."
            };
        }

        /// <summary>
        /// Audits Android Debug Bridge (ADB) installation.
        /// </summary>
        public DiagnosticItem CheckAdbInstallation()
        {
            if (ScrcpyPathResolver.TryResolveAdbExecutable(out string path))
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
                Remediation = "Install ADB:\n" +
                              "  sudo apt install adb (or install Android platform-tools)."
            };
        }

        /// <summary>
        /// Audits scrcpydex-server.jar availability.
        /// </summary>
        public DiagnosticItem CheckServerJarInstallation()
        {
            if (ScrcpyPathResolver.TryResolveServerJar(out string path) && File.Exists(path))
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
                Remediation = "Verify that scrcpydex-server.jar is present in the server directory or /opt/scrcpydex/server/."
            };
        }

        /// <summary>
        /// Executes all diagnostic audits and compiles a consolidated report.
        /// </summary>
        public DiagnosticReport RunAllChecks()
        {
            var report = new DiagnosticReport();
            report.Items.Add(CheckScrcpyVersion());
            report.Items.Add(CheckAdbInstallation());
            report.Items.Add(CheckServerJarInstallation());
            report.Items.Add(CheckUdevRules());
            report.Items.Add(CheckUserGroupMembership());
            report.Items.Add(CheckDisplayServer());
            report.Items.Add(CheckVaApiDriver());
            return report;
        }
    }
}
