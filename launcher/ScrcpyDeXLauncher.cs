using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ScrcpyDeX
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptPath = Path.Combine(baseDir, "client", "ScrcpyDeX-WinUI.ps1");

            if (!File.Exists(scriptPath))
            {
                string altPath = Path.Combine(baseDir, "..", "client", "ScrcpyDeX-WinUI.ps1");
                if (File.Exists(altPath))
                {
                    scriptPath = Path.GetFullPath(altPath);
                }
            }

            if (!File.Exists(scriptPath))
            {
                MessageBox.Show(
                    "Could not locate ScrcpyDeX-WinUI.ps1 in:\n" + scriptPath,
                    "ScrcpyDeX — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Determine if pwsh.exe is installed, else use powershell.exe
            string shellExe = "pwsh.exe";
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            bool pwshFound = false;

            // Check standard WinGet / Program Files pwsh paths
            string[] directPwshCandidates = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "pwsh.exe")
            };
            foreach (string candidate in directPwshCandidates)
            {
                if (File.Exists(candidate))
                {
                    shellExe = candidate;
                    pwshFound = true;
                    break;
                }
            }

            if (!pwshFound)
            {
                foreach (string p in pathEnv.Split(';'))
                {
                    try
                    {
                        string trimmed = p.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && File.Exists(Path.Combine(trimmed, "pwsh.exe")))
                        {
                            shellExe = "pwsh.exe";
                            pwshFound = true;
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (!pwshFound)
            {
                shellExe = "powershell.exe";
            }

            try
            {
                string passedArgs = (args != null && args.Length > 0) ? (" " + string.Join(" ", args)) : "";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = shellExe;
                psi.Arguments = "-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File \"" + scriptPath + "\"" + passedArgs;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to launch ScrcpyDeX Control Center:\n\n" + ex.Message,
                    "ScrcpyDeX — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
