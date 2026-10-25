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
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using ScrcpyDex.App.ViewModels;
using ScrcpyDex.Core.IO;

namespace ScrcpyDex.App.Views
{
    /// <summary>
    /// Main interaction window for ScrcpyDeX Avalonia application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly IBrush TimestampBrush = new SolidColorBrush(Color.Parse("#64748B"));
        private static readonly IBrush StandardTextBrush = new SolidColorBrush(Color.Parse("#E2E8F0"));
        private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#FBBF24"));
        private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FB7185"));

        private INotifyCollectionChanged? _subscribedLogEntries;
        private ScrollViewer? _terminalScroller;
        private SelectableTextBlock? _terminalTextBlock;
        private MainWindowViewModel? _viewModel;
        private int _scrollRequestsPending;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            _terminalScroller = this.FindControl<ScrollViewer>("TerminalScrollViewer");
            _terminalTextBlock = this.FindControl<SelectableTextBlock>("TerminalTextBlock");

            if (_terminalScroller != null)
            {
                _terminalScroller.LayoutUpdated += OnTerminalLayoutUpdated;
            }

            if (DataContext is MainWindowViewModel vm)
            {
                _viewModel = vm;
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

                RenderAllLogs();
            }
        }

        private void OnTerminalLayoutUpdated(object? sender, EventArgs e)
        {
            if (_scrollRequestsPending > 0 && _terminalScroller != null)
            {
                _scrollRequestsPending--;
                _terminalScroller.ScrollToEnd();
            }
        }

        private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_terminalTextBlock == null) return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _terminalTextBlock.Inlines?.Clear();
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                if (_terminalTextBlock.Inlines == null)
                {
                    _terminalTextBlock.Inlines = new InlineCollection();
                }

                foreach (ProcessLogEntry entry in e.NewItems)
                {
                    AppendLogEntry(_terminalTextBlock.Inlines, entry);
                }
                RequestTerminalScroll();
            }
            else
            {
                RenderAllLogs();
            }
        }

        private void RenderAllLogs()
        {
            if (_terminalTextBlock == null || _viewModel == null) return;

            var inlines = new InlineCollection();
            foreach (var entry in _viewModel.LogEntries)
            {
                AppendLogEntry(inlines, entry);
            }
            _terminalTextBlock.Inlines = inlines;
            RequestTerminalScroll();
        }

        private static void AppendLogEntry(InlineCollection inlines, ProcessLogEntry entry)
        {
            // Timestamp
            inlines.Add(new Run($"[{entry.Timestamp:HH:mm:ss}] ")
            {
                Foreground = TimestampBrush
            });

            // Badge / Severity prefix
            if (entry.IsError)
            {
                inlines.Add(new Run("[ERR] ")
                {
                    Foreground = ErrorBrush,
                    FontWeight = FontWeight.Bold
                });
            }
            else if (entry.IsWarning)
            {
                inlines.Add(new Run("[WARN] ")
                {
                    Foreground = WarningBrush,
                    FontWeight = FontWeight.Bold
                });
            }

            // Message text
            var textBrush = entry.IsError ? ErrorBrush : (entry.IsWarning ? WarningBrush : StandardTextBrush);
            inlines.Add(new Run(entry.Text)
            {
                Foreground = textBrush
            });

            // Newline
            inlines.Add(new LineBreak());
        }

        private void RequestTerminalScroll()
        {
            if (_terminalScroller == null) return;

            _scrollRequestsPending = 2;
            Dispatcher.UIThread.Post(() =>
            {
                _terminalScroller.ScrollToEnd();
            }, DispatcherPriority.Loaded);
        }
    }
}
