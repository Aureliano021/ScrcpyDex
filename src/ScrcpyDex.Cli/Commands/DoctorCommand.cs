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
using ScrcpyDex.Cli.Utils;
using ScrcpyDex.Platform.Linux;

namespace ScrcpyDex.Cli.Commands
{
    public static class DoctorCommand
    {
        public static int Execute()
        {
            AnsiConsole.Header("=== ScrcpyDeX Environment Diagnostic Audit ===");
            Console.WriteLine();

            var report = LinuxSetupChecker.Instance.RunAllChecks();

            var tableRows = new List<string[]>();
            var headers = new[] { "STATUS", "SUBSYSTEM / CHECK", "SUMMARY" };

            foreach (var item in report.Items)
            {
                string statusText = item.Status switch
                {
                    DiagnosticStatus.Pass => AnsiConsole.Colorize("[PASS]", AnsiConsole.Green),
                    DiagnosticStatus.Warning => AnsiConsole.Colorize("[WARN]", AnsiConsole.Yellow),
                    DiagnosticStatus.Error => AnsiConsole.Colorize("[FAIL]", AnsiConsole.Red),
                    DiagnosticStatus.Info => AnsiConsole.Colorize("[INFO]", AnsiConsole.Cyan),
                    _ => "[????]"
                };

                tableRows.Add(new[] { statusText, item.Name, item.Summary });
            }

            AnsiConsole.PrintTable(headers, tableRows);
            Console.WriteLine();

            // Print remediation notes if any warnings or errors were encountered
            foreach (var item in report.Items)
            {
                if (item.Status == DiagnosticStatus.Error || item.Status == DiagnosticStatus.Warning)
                {
                    string tag = item.Status == DiagnosticStatus.Error
                        ? AnsiConsole.Colorize("[REMEDIATION REQUIRED]", AnsiConsole.Red)
                        : AnsiConsole.Colorize("[RECOMMENDED ACTION]", AnsiConsole.Yellow);

                    Console.WriteLine($"{tag} {AnsiConsole.Colorize(item.Name, AnsiConsole.Bold)}:");
                    if (!string.IsNullOrWhiteSpace(item.Details))
                    {
                        Console.WriteLine($"  Details: {item.Details}");
                    }
                    if (!string.IsNullOrWhiteSpace(item.Remediation))
                    {
                        Console.WriteLine($"  Remediation:\n{item.Remediation}");
                    }
                    Console.WriteLine();
                }
            }

            string resultSummary = $"Audit Summary: {report.PassCount} passed, {report.WarningCount} warnings, {report.ErrorCount} errors.";
            if (report.HasErrors)
            {
                AnsiConsole.Error(resultSummary);
                return 1;
            }
            else if (report.HasWarnings)
            {
                AnsiConsole.Warning(resultSummary);
                return 0;
            }
            else
            {
                AnsiConsole.Success(resultSummary);
                return 0;
            }
        }
    }
}
