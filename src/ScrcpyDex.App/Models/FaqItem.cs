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

using CommunityToolkit.Mvvm.ComponentModel;
using ScrcpyDex.Core.Localization;

namespace ScrcpyDex.App.Models
{
    /// <summary>
    /// Represents an interactive troubleshooting FAQ entry.
    /// Provides dynamic localized strings, category metadata, and optional shell command for quick copy.
    /// </summary>
    public partial class FaqItem : ObservableObject
    {
        public int Id { get; init; }
        public string CategoryId { get; init; } = string.Empty;
        public string TitleKey { get; init; } = string.Empty;
        public string SymptomKey { get; init; } = string.Empty;
        public string CauseKey { get; init; } = string.Empty;
        public string SolutionKey { get; init; } = string.Empty;
        public string? CommandCode { get; init; }

        public string Title => LocalizationManager.Instance[TitleKey];
        public string Symptom => LocalizationManager.Instance[SymptomKey];
        public string Cause => LocalizationManager.Instance[CauseKey];
        public string Solution => LocalizationManager.Instance[SolutionKey];
        public string CategoryName => LocalizationManager.Instance[$"Faq_Category_{CategoryId}"];

        public bool HasCommand => !string.IsNullOrWhiteSpace(CommandCode);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExpandIcon))]
        private bool _isExpanded;

        public string ExpandIcon => IsExpanded ? "▴" : "▾";

        public void RefreshLocalizedStrings()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Symptom));
            OnPropertyChanged(nameof(Cause));
            OnPropertyChanged(nameof(Solution));
            OnPropertyChanged(nameof(CategoryName));
        }
    }
}
