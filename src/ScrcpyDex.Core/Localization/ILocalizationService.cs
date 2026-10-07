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

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace ScrcpyDex.Core.Localization
{
    /// <summary>
    /// Service contract for application internationalization (i18n),
    /// dynamic runtime culture switching, and resource resolution.
    /// </summary>
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>
        /// Retrieves the localized string associated with the specified key,
        /// optionally formatting it with arguments using the current culture.
        /// </summary>
        /// <param name="key">The resource string identifier.</param>
        /// <param name="args">Optional arguments for composite string formatting.</param>
        /// <returns>The localized and formatted string, or the key itself if not found.</returns>
        string GetString(string key, params object[] args);

        /// <summary>
        /// Indexer for convenient string lookup and dynamic UI data-binding.
        /// </summary>
        /// <param name="key">The resource string identifier.</param>
        /// <returns>The localized string.</returns>
        string this[string key] { get; }

        /// <summary>
        /// Gets the currently active culture used for localization.
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Gets the list of supported cultures in the application.
        /// </summary>
        IReadOnlyList<CultureInfo> SupportedCultures { get; }

        /// <summary>
        /// Switches the active culture at runtime without requiring an application restart.
        /// </summary>
        /// <param name="cultureName">The culture name or code (e.g., "en", "pt-BR", "es", "de", "fr", "zh-Hans").</param>
        void SetCulture(string cultureName);

        /// <summary>
        /// Event triggered when culture or localized values change.
        /// </summary>
        new event PropertyChangedEventHandler? PropertyChanged;
    }
}
