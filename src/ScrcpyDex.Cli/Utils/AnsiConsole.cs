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

namespace ScrcpyDex.Cli.Utils
{
    /// <summary>
    /// Utility for ANSI-colored terminal output and tabular formatting.
    /// </summary>
    public static class AnsiConsole
    {
        private static readonly bool ColorsSupported = !Console.IsOutputRedirected;

        public const string Reset = "\u001b[0m";
        public const string Bold = "\u001b[1m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";
        public const string Gray = "\u001b[90m";

        public static string Colorize(string text, string color)
        {
            return ColorsSupported ? $"{color}{text}{Reset}" : text;
        }

        public static void Info(string message)
        {
            Console.WriteLine($"{Colorize("[INFO]", Cyan)} {message}");
        }

        public static void Success(string message)
        {
            Console.WriteLine($"{Colorize("[OK]", Green)} {message}");
        }

        public static void Warning(string message)
        {
            Console.WriteLine($"{Colorize("[WARN]", Yellow)} {message}");
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine($"{Colorize("[ERROR]", Red)} {message}");
        }

        public static void Header(string message)
        {
            Console.WriteLine($"{Colorize(message, Bold + Cyan)}");
        }

        public static void PrintTable(string[] headers, List<string[]> rows)
        {
            if (headers.Length == 0 || rows.Count == 0) return;

            int cols = headers.Length;
            int[] colWidths = new int[cols];

            for (int i = 0; i < cols; i++)
            {
                colWidths[i] = headers[i].Length;
            }

            foreach (var row in rows)
            {
                for (int i = 0; i < cols && i < row.Length; i++)
                {
                    if (row[i].Length > colWidths[i])
                    {
                        colWidths[i] = row[i].Length;
                    }
                }
            }

            // Print header
            var headerLine = new List<string>();
            var separatorLine = new List<string>();
            for (int i = 0; i < cols; i++)
            {
                headerLine.Add(headers[i].PadRight(colWidths[i]));
                separatorLine.Add(new string('-', colWidths[i]));
            }

            Console.WriteLine(Colorize(string.Join("  ", headerLine), Bold));
            Console.WriteLine(Colorize(string.Join("  ", separatorLine), Gray));

            // Print rows
            foreach (var row in rows)
            {
                var rowLine = new List<string>();
                for (int i = 0; i < cols; i++)
                {
                    string val = i < row.Length ? row[i] : string.Empty;
                    rowLine.Add(val.PadRight(colWidths[i]));
                }
                Console.WriteLine(string.Join("  ", rowLine));
            }
        }
    }
}
