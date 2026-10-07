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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Platform;

namespace ScrcpyDex.Platform.Linux
{
    /// <summary>
    /// Tracks a spawned process along with its assigned POSIX process group ID.
    /// </summary>
    internal sealed class TrackedProcessEntry
    {
        public Process Process { get; }
        public int Pid { get; }
        public int Pgid { get; }

        public TrackedProcessEntry(Process process, int pid, int pgid)
        {
            Process = process;
            Pid = pid;
            Pgid = pgid;
        }
    }

    /// <summary>
    /// Linux/POSIX implementation of <see cref="IProcessLifetimeManager"/>.
    /// Enforces process tree containment using POSIX process groups (setpgid/getpgid) and signal delivery (SIGTERM/SIGKILL).
    /// Listens for POSIX signals (SIGINT, SIGTERM, SIGHUP) and ProcessExit to ensure children are cleanly terminated.
    /// </summary>
    public sealed class LinuxProcessLifetimeManager : IProcessLifetimeManager
    {
        private readonly ConcurrentDictionary<int, TrackedProcessEntry> _trackedProcesses = new();
        private readonly List<Func<Task>> _cleanupActions = new();
        private readonly object _cleanupLock = new();

        private PosixSignalRegistration? _sigIntReg;
        private PosixSignalRegistration? _sigTermReg;
        private PosixSignalRegistration? _sigHupReg;

        private int _isTerminating;
        private bool _isDisposed;

        public static LinuxProcessLifetimeManager Default { get; } = new();

        /// <summary>
        /// Event fired when a POSIX termination signal or ProcessExit is intercepted.
        /// </summary>
        public event EventHandler? ShutdownRequested;

        public LinuxProcessLifetimeManager()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            RegisterPosixSignals();
        }

        private void RegisterPosixSignals()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            {
                return;
            }

            try
            {
                _sigIntReg = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnPosixSignalReceived);
            }
            catch (PlatformNotSupportedException) { }
            catch (Exception) { }

            try
            {
                _sigTermReg = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnPosixSignalReceived);
            }
            catch (PlatformNotSupportedException) { }
            catch (Exception) { }

            try
            {
                _sigHupReg = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnPosixSignalReceived);
            }
            catch (PlatformNotSupportedException) { }
            catch (Exception) { }
        }

        /// <summary>
        /// Registers a pre-termination cleanup action (such as unregistering DeX display sessions via ADB)
        /// executed prior to process group signaling.
        /// </summary>
        public void RegisterCleanupAction(Func<Task> action)
        {
            if (action == null) return;
            lock (_cleanupLock)
            {
                _cleanupActions.Add(action);
            }
        }

        /// <summary>
        /// Registers a synchronous pre-termination cleanup action.
        /// </summary>
        public void RegisterCleanupAction(Action action)
        {
            if (action == null) return;
            RegisterCleanupAction(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Binds the given child process to this lifetime manager.
        /// Isolates the process in its own POSIX process group when running on Linux.
        /// </summary>
        public void AssignProcess(Process process)
        {
            if (process == null || _isDisposed || Volatile.Read(ref _isTerminating) == 1)
            {
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                int pid = process.Id;
                int pgid = pid;

                if (OperatingSystem.IsLinux())
                {
                    try
                    {
                        // Attempt to place process in its own process group
                        int setRes = Libc.setpgid(pid, pid);
                        int fetchedPgid = Libc.getpgid(pid);
                        if (fetchedPgid > 0)
                        {
                            pgid = fetchedPgid;
                        }
                    }
                    catch
                    {
                        // Fallback to PID as default PGID
                        pgid = pid;
                    }
                }

                var entry = new TrackedProcessEntry(process, pid, pgid);
                _trackedProcesses.TryAdd(pid, entry);

                try
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        _trackedProcesses.TryRemove(pid, out _);
                    };
                }
                catch { }
            }
            catch
            {
                // Process may have already exited before assignment
            }
        }

        private void OnPosixSignalReceived(PosixSignalContext ctx)
        {
            ctx.Cancel = true;
            ShutdownRequested?.Invoke(this, EventArgs.Empty);
            TerminateAllProcesses(gracePeriodMs: 2000);
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            ShutdownRequested?.Invoke(this, EventArgs.Empty);
            TerminateAllProcesses(gracePeriodMs: 2000);
        }

        /// <summary>
        /// Terminates all tracked process groups. Sends SIGTERM first, waits up to <paramref name="gracePeriodMs"/>,
        /// and escalates to SIGKILL if processes remain active.
        /// </summary>
        public void TerminateAllProcesses(int gracePeriodMs = 2000)
        {
            if (Interlocked.Exchange(ref _isTerminating, 1) != 0)
            {
                return;
            }

            // Execute registered pre-termination cleanup actions
            ExecuteCleanupActions();

            var entries = new List<TrackedProcessEntry>(_trackedProcesses.Values);
            if (entries.Count == 0)
            {
                return;
            }

            int hostPgid = 0;
            if (OperatingSystem.IsLinux())
            {
                try
                {
                    hostPgid = Libc.getpgrp();
                }
                catch { }
            }

            // Phase 1: Send SIGTERM to process groups (or individual processes)
            foreach (var entry in entries)
            {
                try
                {
                    if (entry.Process.HasExited) continue;

                    if (OperatingSystem.IsLinux())
                    {
                        // Send SIGTERM to process group if isolated, avoiding signaling host's own group
                        if (entry.Pgid > 1 && entry.Pgid != hostPgid)
                        {
                            Libc.kill(-entry.Pgid, Libc.SIGTERM);
                        }
                        else
                        {
                            Libc.kill(entry.Pid, Libc.SIGTERM);
                        }
                    }

                    // Also signal via managed process tree kill if non-Linux
                    if (!OperatingSystem.IsLinux() && !entry.Process.HasExited)
                    {
                        entry.Process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore transient signaling exceptions during teardown
                }
            }

            // Phase 2: Wait within grace period (up to gracePeriodMs)
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < gracePeriodMs)
            {
                bool anyAlive = false;
                foreach (var entry in entries)
                {
                    try
                    {
                        if (!entry.Process.HasExited)
                        {
                            anyAlive = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Process exit inquiry exception indicates termination
                    }
                }

                if (!anyAlive)
                {
                    break;
                }

                Thread.Sleep(50);
            }

            // Phase 3: Force kill with SIGKILL if any processes survived grace period
            foreach (var entry in entries)
            {
                try
                {
                    if (entry.Process.HasExited) continue;

                    if (OperatingSystem.IsLinux())
                    {
                        if (entry.Pgid > 1 && entry.Pgid != hostPgid)
                        {
                            Libc.kill(-entry.Pgid, Libc.SIGKILL);
                        }
                        else
                        {
                            Libc.kill(entry.Pid, Libc.SIGKILL);
                        }
                    }

                    try
                    {
                        if (!entry.Process.HasExited)
                        {
                            entry.Process.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                }
                catch
                {
                    // Ignore exceptions during forced kill
                }
            }

            _trackedProcesses.Clear();
        }

        private void ExecuteCleanupActions()
        {
            List<Func<Task>> actionsCopy;
            lock (_cleanupLock)
            {
                actionsCopy = new List<Func<Task>>(_cleanupActions);
                _cleanupActions.Clear();
            }

            if (actionsCopy.Count == 0) return;

            foreach (var action in actionsCopy)
            {
                try
                {
                    var task = action();
                    task.Wait(TimeSpan.FromMilliseconds(1500));
                }
                catch
                {
                    // Best effort cleanup during shutdown
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            }
            catch { }

            _sigIntReg?.Dispose();
            _sigIntReg = null;

            _sigTermReg?.Dispose();
            _sigTermReg = null;

            _sigHupReg?.Dispose();
            _sigHupReg = null;

            TerminateAllProcesses(gracePeriodMs: 2000);
        }
    }
}
