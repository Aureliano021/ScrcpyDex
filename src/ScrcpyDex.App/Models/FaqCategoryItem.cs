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
    /// Represents a selectable category filter pill in the FAQ drawer.
    /// </summary>
    public partial class FaqCategoryItem : ObservableObject
    {
        public string Id { get; init; } = string.Empty;
        public string NameKey { get; init; } = string.Empty;
        public int Count { get; init; }

        public string DisplayName => LocalizationManager.Instance[NameKey];
        public string HeaderText => $"{DisplayName} ({Count})";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
        [NotifyPropertyChangedFor(nameof(BorderBrushColor))]
        [NotifyPropertyChangedFor(nameof(ForegroundBrush))]
        private bool _isSelected;

        public string BackgroundBrush => IsSelected ? "#2563EB" : "#161F2E";
        public string BorderBrushColor => IsSelected ? "#3B82F6" : "#1E293B";
        public string ForegroundBrush => IsSelected ? "#FFFFFF" : "#94A3B8";

        public void RefreshLocalizedStrings()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(HeaderText));
        }
    }
}
