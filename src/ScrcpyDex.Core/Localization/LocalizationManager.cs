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
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ScrcpyDex.Core.Localization
{
    /// <summary>
    /// Thread-safe localization manager providing runtime culture switching,
    /// hierarchical fallback resolution, and notification support for UI bindings.
    /// </summary>
    public sealed class LocalizationManager : ILocalizationService
    {
        private static readonly Lazy<LocalizationManager> _instance =
            new Lazy<LocalizationManager>(() => new LocalizationManager());

        /// <summary>
        /// Gets the singleton instance of <see cref="LocalizationManager"/>.
        /// </summary>
        public static LocalizationManager Instance => _instance.Value;

        private readonly object _syncLock = new object();
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        public IReadOnlyList<CultureInfo> SupportedCultures { get; }

        /// <inheritdoc />
        public CultureInfo CurrentCulture
        {
            get
            {
                lock (_syncLock)
                {
                    return _currentCulture;
                }
            }
            private set
            {
                lock (_syncLock)
                {
                    _currentCulture = value;
                }
            }
        }

        /// <inheritdoc />
        public string this[string key] => GetString(key);

        /// <summary>
        /// Initializes a new instance of <see cref="LocalizationManager"/> with default configuration.
        /// </summary>
        public LocalizationManager() : this(new ResourceManager("ScrcpyDex.Core.Localization.Strings", typeof(LocalizationManager).Assembly))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalizationManager"/> with a custom ResourceManager.
        /// </summary>
        /// <param name="resourceManager">The resource manager to resolve string resources.</param>
        public LocalizationManager(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

            SupportedCultures = new List<CultureInfo>
            {
                CultureInfo.GetCultureInfo("en"),
                CultureInfo.GetCultureInfo("pt-BR"),
                CultureInfo.GetCultureInfo("es"),
                CultureInfo.GetCultureInfo("de"),
                CultureInfo.GetCultureInfo("fr"),
                CultureInfo.GetCultureInfo("zh-Hans")
            }.AsReadOnly();

            // Default to English
            _currentCulture = CultureInfo.GetCultureInfo("en");
        }

        /// <inheritdoc />
        public void SetCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                cultureName = "en";
            }

            string normalized = NormalizeCultureName(cultureName);

            CultureInfo resolvedCulture;
            try
            {
                resolvedCulture = CultureInfo.GetCultureInfo(normalized);
            }
            catch (CultureNotFoundException)
            {
                resolvedCulture = CultureInfo.GetCultureInfo("en");
            }

            lock (_syncLock)
            {
                if (Equals(_currentCulture, resolvedCulture))
                {
                    return;
                }

                _currentCulture = resolvedCulture;
                CultureInfo.CurrentCulture = resolvedCulture;
                CultureInfo.CurrentUICulture = resolvedCulture;
                CultureInfo.DefaultThreadCurrentCulture = resolvedCulture;
                CultureInfo.DefaultThreadCurrentUICulture = resolvedCulture;
            }

            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged("Item[]");
            OnPropertyChanged(string.Empty);
        }

        /// <inheritdoc />
        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            CultureInfo culture = CurrentCulture;
            string? localized = null;

            try
            {
                // 1. Requested specific culture
                localized = _resourceManager.GetString(key, culture);

                // 2. Language family / parent culture
                if (string.IsNullOrEmpty(localized) && !Equals(culture.Parent, CultureInfo.InvariantCulture))
                {
                    localized = _resourceManager.GetString(key, culture.Parent);
                }

                // 3. Invariant master Strings.resx
                if (string.IsNullOrEmpty(localized))
                {
                    localized = _resourceManager.GetString(key, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                localized = null;
            }

            if (string.IsNullOrEmpty(localized))
            {
                localized = key;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(culture, localized, args);
                }
                catch (FormatException)
                {
                    return localized;
                }
            }

            return localized;
        }

        private static string NormalizeCultureName(string cultureName)
        {
            string trimmed = cultureName.Trim();
            if (trimmed.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("zh-SG", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hans";
            }

            if (trimmed.Equals("pt", StringComparison.OrdinalIgnoreCase))
            {
                return "pt-BR";
            }

            return trimmed;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
