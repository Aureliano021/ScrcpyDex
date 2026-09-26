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
using System.Windows;
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
            if (ViewModel.DiagnosticLogs is INotifyCollectionChanged observableLogs)
            {
                observableLogs.CollectionChanged += OnDiagnosticLogsCollectionChanged;
            }
        }

        private void OnDiagnosticLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel.IsAutoScrollEnabled && LogsListBox != null && LogsListBox.Items.Count > 0)
            {
                // Auto-scroll virtualized log viewer to bottom entry
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (LogsListBox.Items.Count > 0)
                        {
                            LogsListBox.ScrollIntoView(LogsListBox.Items[LogsListBox.Items.Count - 1]);
                        }
                    }
                    catch { }
                }));
            }
        }

        private void OnMainWindowClosed(object? sender, EventArgs e)
        {
            ViewModel.Dispose();
        }
    }
}
