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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ScrcpyDex.Core.Localization
{
    /// <summary>
    /// Programmatic audit utility to verify that localized RESX resource files
    /// maintain 1:1 key parity with the master Strings.resx and that composite
    /// format placeholders ({0}, {1}, etc.) match identically.
    /// </summary>
    public static class LocalizationValidator
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Parses a RESX XML file and extracts all key-value entries.
        /// </summary>
        /// <param name="filePath">Path to the .resx file on disk.</param>
        /// <returns>A dictionary containing resource key names and their values.</returns>
        public static Dictionary<string, string> ParseResx(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"RESX resource file not found: {filePath}", filePath);
            }

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            return ParseResxXml(content);
        }

        /// <summary>
        /// Parses raw XML content of a RESX file into key-value entries.
        /// </summary>
        /// <param name="xmlContent">Raw RESX XML string content.</param>
        /// <returns>A dictionary containing resource key names and their values.</returns>
        public static Dictionary<string, string> ParseResxXml(string xmlContent)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            var doc = XDocument.Parse(xmlContent);

            var dataElements = doc.Root?.Elements("data") ?? Enumerable.Empty<XElement>();
            foreach (var element in dataElements)
            {
                var nameAttr = element.Attribute("name");
                if (nameAttr == null || string.IsNullOrWhiteSpace(nameAttr.Value))
                {
                    continue;
                }

                string key = nameAttr.Value.Trim();
                string value = element.Element("value")?.Value ?? string.Empty;
                entries[key] = value;
            }

            return entries;
        }

        /// <summary>
        /// Validates a single target RESX file against the master RESX file.
        /// </summary>
        /// <param name="masterResxFilePath">Path to the invariant master Strings.resx file.</param>
        /// <param name="targetResxFilePath">Path to the localized Strings.&lt;culture&gt;.resx file.</param>
        /// <returns>A validation result detailing missing keys, orphaned keys, and placeholder mismatches.</returns>
        public static LocalizationValidationResult ValidateResxFile(string masterResxFilePath, string targetResxFilePath)
        {
            string targetCulture = ExtractCultureName(targetResxFilePath);
            var masterEntries = ParseResx(masterResxFilePath);
            var targetEntries = ParseResx(targetResxFilePath);

            return ValidateEntries(targetCulture, masterEntries, targetEntries);
        }

        /// <summary>
        /// Validates in-memory target dictionary entries against master entries.
        /// </summary>
        /// <param name="targetCulture">The culture identifier of the target entries.</param>
        /// <param name="masterEntries">The invariant master key-value pairs.</param>
        /// <param name="targetEntries">The target localized key-value pairs.</param>
        /// <returns>A validation result detailing parity and placeholder conformity.</returns>
        public static LocalizationValidationResult ValidateEntries(
            string targetCulture,
            IReadOnlyDictionary<string, string> masterEntries,
            IReadOnlyDictionary<string, string> targetEntries)
        {
            var missingKeys = new List<string>();
            var orphanedKeys = new List<string>();
            var placeholderMismatches = new List<PlaceholderMismatch>();

            // 1. Audit missing keys (exist in master but not target)
            foreach (var masterKey in masterEntries.Keys)
            {
                if (!targetEntries.ContainsKey(masterKey))
                {
                    missingKeys.Add(masterKey);
                }
            }

            // 2. Audit orphaned keys (exist in target but not master)
            foreach (var targetKey in targetEntries.Keys)
            {
                if (!masterEntries.ContainsKey(targetKey))
                {
                    orphanedKeys.Add(targetKey);
                }
            }

            // 3. Audit format string placeholders on common keys
            foreach (var pair in masterEntries)
            {
                string key = pair.Key;
                if (!targetEntries.TryGetValue(key, out var targetValue))
                {
                    continue;
                }

                string masterValue = pair.Value;
                var masterPlaceholders = ExtractPlaceholders(masterValue);
                var targetPlaceholders = ExtractPlaceholders(targetValue);

                if (!masterPlaceholders.SequenceEqual(targetPlaceholders))
                {
                    placeholderMismatches.Add(new PlaceholderMismatch(
                        key,
                        masterPlaceholders,
                        targetPlaceholders));
                }
            }

            return new LocalizationValidationResult(
                targetCulture,
                missingKeys.AsReadOnly(),
                orphanedKeys.AsReadOnly(),
                placeholderMismatches.AsReadOnly());
        }

        /// <summary>
        /// Audits all localized RESX files found within a directory against the master Strings.resx.
        /// </summary>
        /// <param name="directoryPath">Directory containing RESX files.</param>
        /// <param name="masterFileName">Name of master file, defaults to "Strings.resx".</param>
        /// <returns>A collection of validation results for each detected localized resource file.</returns>
        public static IReadOnlyList<LocalizationValidationResult> ValidateDirectory(
            string directoryPath,
            string masterFileName = "Strings.resx")
        {
            string masterPath = Path.Combine(directoryPath, masterFileName);
            if (!File.Exists(masterPath))
            {
                throw new FileNotFoundException($"Master RESX file not found: {masterPath}", masterPath);
            }

            var masterEntries = ParseResx(masterPath);
            var results = new List<LocalizationValidationResult>();

            var resxFiles = Directory.GetFiles(directoryPath, "Strings.*.resx");
            foreach (var file in resxFiles.OrderBy(f => f))
            {
                string cultureName = ExtractCultureName(file);
                var targetEntries = ParseResx(file);
                results.Add(ValidateEntries(cultureName, masterEntries, targetEntries));
            }

            return results.AsReadOnly();
        }

        private static List<int> ExtractPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<int>();
            }

            var matches = PlaceholderRegex.Matches(text);
            var indices = new List<int>();

            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int idx))
                {
                    indices.Add(idx);
                }
            }

            return indices.Distinct().OrderBy(i => i).ToList();
        }

        private static string ExtractCultureName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            int firstDot = fileName.IndexOf('.');
            if (firstDot >= 0 && firstDot < fileName.Length - 1)
            {
                return fileName.Substring(firstDot + 1);
            }

            return Path.GetFileName(filePath);
        }
    }

    /// <summary>
    /// Result record containing audit outcome for a localized RESX file.
    /// </summary>
    public sealed class LocalizationValidationResult
    {
        public string TargetCulture { get; }
        public IReadOnlyList<string> MissingKeys { get; }
        public IReadOnlyList<string> OrphanedKeys { get; }
        public IReadOnlyList<PlaceholderMismatch> PlaceholderMismatches { get; }

        public bool IsValid =>
            MissingKeys.Count == 0 &&
            OrphanedKeys.Count == 0 &&
            PlaceholderMismatches.Count == 0;

        public LocalizationValidationResult(
            string targetCulture,
            IReadOnlyList<string> missingKeys,
            IReadOnlyList<string> orphanedKeys,
            IReadOnlyList<PlaceholderMismatch> placeholderMismatches)
        {
            TargetCulture = targetCulture;
            MissingKeys = missingKeys;
            OrphanedKeys = orphanedKeys;
            PlaceholderMismatches = placeholderMismatches;
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Culture: {TargetCulture} | Valid: {IsValid}");
            sb.AppendLine($"  Missing Keys: {MissingKeys.Count}");
            foreach (var key in MissingKeys)
            {
                sb.AppendLine($"    - Missing: {key}");
            }

            sb.AppendLine($"  Orphaned Keys: {OrphanedKeys.Count}");
            foreach (var key in OrphanedKeys)
            {
                sb.AppendLine($"    - Orphaned: {key}");
            }

            sb.AppendLine($"  Placeholder Mismatches: {PlaceholderMismatches.Count}");
            foreach (var mismatch in PlaceholderMismatches)
            {
                sb.AppendLine($"    - Key: {mismatch.Key} | Master: [{string.Join(", ", mismatch.MasterPlaceholders)}] vs Target: [{string.Join(", ", mismatch.TargetPlaceholders)}]");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Details regarding a format string placeholder mismatch between master and localized strings.
    /// </summary>
    public sealed class PlaceholderMismatch
    {
        public string Key { get; }
        public IReadOnlyList<int> MasterPlaceholders { get; }
        public IReadOnlyList<int> TargetPlaceholders { get; }

        public PlaceholderMismatch(string key, IReadOnlyList<int> masterPlaceholders, IReadOnlyList<int> targetPlaceholders)
        {
            Key = key;
            MasterPlaceholders = masterPlaceholders;
            TargetPlaceholders = targetPlaceholders;
        }
    }
}
