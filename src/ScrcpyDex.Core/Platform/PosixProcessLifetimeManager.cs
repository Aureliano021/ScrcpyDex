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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ScrcpyDex.Core.Platform
{
    /// <summary>
    /// POSIX process lifetime manager that tracks spawned child processes
    /// and ensures orderly termination when the parent application exits.
    /// </summary>
    public sealed class PosixProcessLifetimeManager : IProcessLifetimeManager
    {
        private readonly ConcurrentDictionary<int, Process> _trackedProcesses = new();
        private bool _isDisposed;

        public static PosixProcessLifetimeManager Default { get; } = new();

        public PosixProcessLifetimeManager()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public void AssignProcess(Process process)
        {
            if (process == null || _isDisposed) return;

            try
            {
                if (!process.HasExited)
                {
                    _trackedProcesses.TryAdd(process.Id, process);
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        try { _trackedProcesses.TryRemove(process.Id, out _); } catch { }
                    };
                }
            }
            catch
            {
                // Process may have already exited
            }
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            KillTrackedProcesses();
        }

        private void KillTrackedProcesses()
        {
            foreach (var kvp in _trackedProcesses)
            {
                try
                {
                    var proc = kvp.Value;
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore exceptions during teardown
                }
            }
            _trackedProcesses.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            KillTrackedProcesses();
        }
    }
}
