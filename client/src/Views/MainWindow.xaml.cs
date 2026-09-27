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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ScrcpyDex.ViewModels;
using Wpf.Ui.Controls;

namespace ScrcpyDex.WinUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Main application window hosting ScrcpyDeX settings and session controls.
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));   // scrcpy green
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));     // error red
        private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromRgb(255, 235, 59)); // warn yellow
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // default white

        static MainWindow()
        {
            GreenBrush.Freeze();
            RedBrush.Freeze();
            YellowBrush.Freeze();
            WhiteBrush.Freeze();
        }

        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            InitializeComponent();

            Loaded += OnMainWindowLoaded;
            Closed += OnMainWindowClosed;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (LogsRichTextBox.Document == null)
            {
                LogsRichTextBox.Document = new FlowDocument
                {
                    Background = Brushes.Transparent,
                    PagePadding = new Thickness(4, 2, 4, 2)
                };
            }

            RebuildLogsDocument();

            if (ViewModel.DiagnosticLogs is INotifyCollectionChanged observableLogs)
            {
                observableLogs.CollectionChanged += OnDiagnosticLogsCollectionChanged;
            }

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.LogSearchFilter))
            {
                Dispatcher.BeginInvoke(new Action(RebuildLogsDocument));
            }
        }

        private void OnDiagnosticLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    LogsRichTextBox?.Document?.Blocks.Clear();
                    return;
                }

                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    string filter = ViewModel.LogSearchFilter;
                    foreach (var item in e.NewItems)
                    {
                        if (item is string logLine)
                        {
                            if (string.IsNullOrEmpty(filter) || logLine.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                AppendLogLine(logLine);
                            }
                        }
                    }
                }
            }));
        }

        private void RebuildLogsDocument()
        {
            if (LogsRichTextBox?.Document == null) return;

            LogsRichTextBox.Document.Blocks.Clear();
            string filter = ViewModel.LogSearchFilter;

            foreach (var line in ViewModel.DiagnosticLogs)
            {
                if (string.IsNullOrEmpty(filter) || line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AppendLogLine(line);
                }
            }

            LogsRichTextBox.ScrollToEnd();
        }

        private void AppendLogLine(string line)
        {
            if (LogsRichTextBox?.Document == null) return;

            // Colors: scrcpy green, error red, warn yellow, default white
            SolidColorBrush brush = WhiteBrush;
            if (line.Contains("[ERR]", StringComparison.OrdinalIgnoreCase) || line.Contains("[CRIT]", StringComparison.OrdinalIgnoreCase))
            {
                brush = RedBrush;
            }
            else if (line.Contains("[WARN]", StringComparison.OrdinalIgnoreCase))
            {
                brush = YellowBrush;
            }
            else if (line.Contains("[SCRCPY]", StringComparison.OrdinalIgnoreCase))
            {
                brush = GreenBrush;
            }

            var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1), LineHeight = 17 };
            p.Inlines.Add(new Run(line) { Foreground = brush });
            LogsRichTextBox.Document.Blocks.Add(p);

            // Keep buffer bounded to limit memory usage
            if (LogsRichTextBox.Document.Blocks.Count > 1000)
            {
                LogsRichTextBox.Document.Blocks.Remove(LogsRichTextBox.Document.Blocks.FirstBlock);
            }

            LogsRichTextBox.ScrollToEnd();
        }

        private void OnMainWindowClosed(object? sender, EventArgs e)
        {
            ViewModel.Dispose();
        }
    }
}
