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
using Avalonia.Controls;
using Avalonia.Threading;
using ScrcpyDex.App.ViewModels;

namespace ScrcpyDex.App.Views
{
    /// <summary>
    /// Main interaction window for ScrcpyDeX Avalonia application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private INotifyCollectionChanged? _subscribedLogEntries;
        private ScrollViewer? _terminalScroller;
        private bool _autoScrollPending;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            _terminalScroller = this.FindControl<ScrollViewer>("TerminalScrollViewer");
            if (_terminalScroller != null)
            {
                _terminalScroller.LayoutUpdated += OnTerminalLayoutUpdated;
            }

            if (DataContext is MainWindowViewModel vm)
            {
                vm.SetStorageProvider(StorageProvider);

                if (_subscribedLogEntries != null)
                {
                    _subscribedLogEntries.CollectionChanged -= OnLogEntriesChanged;
                }

                _subscribedLogEntries = vm.LogEntries as INotifyCollectionChanged;
                if (_subscribedLogEntries != null)
                {
                    _subscribedLogEntries.CollectionChanged += OnLogEntriesChanged;
                }
            }
        }

        private void OnTerminalLayoutUpdated(object? sender, EventArgs e)
        {
            if (_autoScrollPending && _terminalScroller != null)
            {
                _autoScrollPending = false;
                _terminalScroller.ScrollToEnd();
            }
        }

        private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                _autoScrollPending = true;
                Dispatcher.UIThread.Post(() =>
                {
                    _terminalScroller?.ScrollToEnd();
                }, DispatcherPriority.Render);
            }
        }
    }
}
