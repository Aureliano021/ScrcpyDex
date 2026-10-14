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
using System.Globalization;
using System.IO;
using System.Linq;
using ScrcpyDex.Core.Localization;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class LocalizationTests
    {
        private static string ResolveLocalizationDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "src", "ScrcpyDex.Core", "Localization");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                string candidateNested = Path.Combine(dir.FullName, "ScrcpyDex", "src", "ScrcpyDex.Core", "Localization");
                if (Directory.Exists(candidateNested))
                {
                    return candidateNested;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate src/ScrcpyDex.Core/Localization directory from test runner.");
        }

        [Fact]
        public void ValidateDirectory_AllLocalizedResxFilesHaveFullParityWithMaster()
        {
            string locDir = ResolveLocalizationDirectory();
            var results = LocalizationValidator.ValidateDirectory(locDir);

            Assert.NotEmpty(results);
            Assert.True(results.Count >= 5, $"Expected at least 5 localized resource files, found {results.Count}.");

            var expectedCultures = new[] { "de", "es", "fr", "pt-BR", "zh-Hans" };
            foreach (var culture in expectedCultures)
            {
                var result = results.FirstOrDefault(r => string.Equals(r.TargetCulture, culture, StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(result);
                Assert.True(result.IsValid, $"Validation failed for culture {culture}: {result.GetSummary()}");
                Assert.Empty(result.MissingKeys);
                Assert.Empty(result.OrphanedKeys);
                Assert.Empty(result.PlaceholderMismatches);
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("pt-BR")]
        [InlineData("es")]
        [InlineData("de")]
        [InlineData("fr")]
        [InlineData("zh-Hans")]
        public void LocalizationManager_RuntimeCultureSwitching_ReturnsLocalizedStrings(string cultureName)
        {
            var manager = LocalizationManager.Instance;
            manager.SetCulture(cultureName);

            Assert.Equal(cultureName, manager.CurrentCulture.Name);

            string title = manager.GetString("App_Title");
            Assert.False(string.IsNullOrWhiteSpace(title));

            string refresh = manager["Action_Refresh"];
            Assert.False(string.IsNullOrWhiteSpace(refresh));

            string mainWindowTitle = manager["MainWindow_Title"];
            Assert.False(string.IsNullOrWhiteSpace(mainWindowTitle));

            string displayHeader = manager["Display_CardHeader"];
            Assert.False(string.IsNullOrWhiteSpace(displayHeader));

            string quickHeader = manager["QuickOptions_Header"];
            Assert.False(string.IsNullOrWhiteSpace(quickHeader));

            string telemetryBattery = manager["Telemetry_Battery"];
            Assert.False(string.IsNullOrWhiteSpace(telemetryBattery));
        }

        [Fact]
        public void LocalizationManager_FormatString_FormatsParametersCorrectly()
        {
            var manager = LocalizationManager.Instance;
            manager.SetCulture("en");

            string formatted = manager.GetString("DeviceSelector_Connected", "Galaxy S23");
            Assert.Equal("Galaxy S23 Connected", formatted);
        }

        [Fact]
        public void LocalizationManager_MissingKey_FallsBackToKeyName()
        {
            var manager = LocalizationManager.Instance;
            string missing = manager.GetString("NonExistent_Key_For_Test_12345");
            Assert.Equal("NonExistent_Key_For_Test_12345", missing);
        }

        [Theory]
        [InlineData("en", "Language")]
        [InlineData("pt-BR", "Idioma")]
        [InlineData("es", "Idioma")]
        [InlineData("de", "Sprache")]
        [InlineData("fr", "Langue")]
        [InlineData("zh-Hans", "语言")]
        public void LocalizationManager_GeneralLanguage_ResolvesCorrectlyAcrossCultures(string cultureName, string expectedLanguageText)
        {
            var manager = LocalizationManager.Instance;
            manager.SetCulture(cultureName);

            string languageText = manager["General_Language"];
            Assert.Equal(expectedLanguageText, languageText);

            string descText = manager["General_Language_Desc"];
            Assert.False(string.IsNullOrWhiteSpace(descText));
        }
    }
}
