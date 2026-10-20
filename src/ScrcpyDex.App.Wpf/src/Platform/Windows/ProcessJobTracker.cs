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
using System.Diagnostics;
using ScrcpyDex.Core.Platform;
using ScrcpyDex.Core.Platform.Windows;

namespace ScrcpyDex.Core.Lifecycle
{
    /// <summary>
    /// Legacy wrapper forwarding to WindowsJobObjectLifetimeManager in ScrcpyDex.Core.
    /// Retained until WPF retirement in PR 4.
    /// </summary>
    public sealed class ProcessJobTracker : IProcessLifetimeManager
    {
        private readonly WindowsJobObjectLifetimeManager _inner;

        private static readonly Lazy<ProcessJobTracker> _defaultInstance = new(() => new ProcessJobTracker());
        public static ProcessJobTracker Default => _defaultInstance.Value;

        public ProcessJobTracker()
        {
            _inner = WindowsJobObjectLifetimeManager.Default;
        }

        public void AssignProcess(Process process) => _inner.AssignProcess(process);

        public void Dispose()
        {
            // Lifecycle managed by inner singleton or process teardown
        }
    }
}
