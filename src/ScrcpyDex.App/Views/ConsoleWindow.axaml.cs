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
    /// Dedicated diagnostic console window for real-time scrcpy and ADB output streams.
    /// </summary>
    public partial class ConsoleWindow : Window
    {
        private static readonly IBrush TimestampBrush = new SolidColorBrush(Color.Parse("#86868B"));
        private static readonly IBrush StandardTextBrush = new SolidColorBrush(Color.Parse("#D0D0D7"));
        private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#FFAA00"));
        private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF453A"));

        private INotifyCollectionChanged? _subscribedLogEntries;
        private ScrollViewer? _terminalScroller;
        private SelectableTextBlock? _terminalTextBlock;
        private MainWindowViewModel? _viewModel;
        private int _scrollRequestsPending;

        public ConsoleWindow()
        {
            InitializeComponent();
        }

        public ConsoleWindow(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
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

        protected override void OnClosed(EventArgs e)
        {
            if (_subscribedLogEntries != null)
            {
                _subscribedLogEntries.CollectionChanged -= OnLogEntriesChanged;
                _subscribedLogEntries = null;
            }

            if (_terminalScroller != null)
            {
                _terminalScroller.LayoutUpdated -= OnTerminalLayoutUpdated;
            }

            base.OnClosed(e);
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
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => OnLogEntriesChanged(sender, e));
                return;
            }

            if (_terminalTextBlock == null) return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _terminalTextBlock.Inlines?.Clear();
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                var inlines = _terminalTextBlock.Inlines ??= new InlineCollection();
                foreach (ProcessLogEntry entry in e.NewItems)
                {
                    AppendLogEntry(inlines, entry);
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

            var inlines = _terminalTextBlock.Inlines ??= new InlineCollection();
            inlines.Clear();
            foreach (var entry in _viewModel.LogEntries)
            {
                AppendLogEntry(inlines, entry);
            }
            RequestTerminalScroll();
        }

        private static void AppendLogEntry(InlineCollection inlines, ProcessLogEntry entry)
        {
            inlines.Add(new Run($"[{entry.Timestamp:HH:mm:ss}] ")
            {
                Foreground = TimestampBrush
            });

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

            var textBrush = entry.IsError ? ErrorBrush : (entry.IsWarning ? WarningBrush : StandardTextBrush);
            inlines.Add(new Run(entry.Text)
            {
                Foreground = textBrush
            });

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
