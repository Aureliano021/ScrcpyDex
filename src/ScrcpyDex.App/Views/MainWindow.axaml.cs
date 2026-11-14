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
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScrcpyDex.App.ViewModels;

namespace ScrcpyDex.App.Views
{
    /// <summary>
    /// Compact main interaction window for ScrcpyDeX.
    /// Hosts the primary connected-device view, in-window Settings/FAQ overlays, and manages the dedicated ConsoleWindow.
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConsoleWindow? _consoleWindow;
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (!e.Handled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void OnMinimizeButtonClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (DataContext is MainWindowViewModel vm)
            {
                _viewModel = vm;
                vm.SetStorageProvider(StorageProvider);
                vm.OpenConsoleWindowRequested = OpenOrActivateConsoleWindow;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.OpenConsoleWindowRequested = null;
            }

            if (_consoleWindow is { IsVisible: true })
            {
                _consoleWindow.Close();
                _consoleWindow = null;
            }

            base.OnClosed(e);
        }

        public void OpenOrActivateConsoleWindow()
        {
            if (_viewModel == null) return;

            if (_consoleWindow is { IsVisible: true })
            {
                if (_consoleWindow.WindowState == WindowState.Minimized)
                {
                    _consoleWindow.WindowState = WindowState.Normal;
                }
                _consoleWindow.Activate();
                return;
            }

            _consoleWindow = new ConsoleWindow(_viewModel);
            _consoleWindow.Closed += (_, _) => _consoleWindow = null;
            _consoleWindow.Show();
        }
    }
}
