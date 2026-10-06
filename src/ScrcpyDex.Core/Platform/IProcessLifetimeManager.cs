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

namespace ScrcpyDex.Core.Platform
{
    /// <summary>
    /// Manages child process lifetimes to ensure spawned processes (such as scrcpy or adb daemons)
    /// are reliably terminated when the host application exits.
    /// </summary>
    public interface IProcessLifetimeManager : IDisposable
    {
        /// <summary>
        /// Binds the given child process to this lifetime manager.
        /// </summary>
        void AssignProcess(Process process);
    }

    /// <summary>
    /// Fallback no-op lifetime manager when platform-specific process management is not required.
    /// </summary>
    public sealed class NullProcessLifetimeManager : IProcessLifetimeManager
    {
        public static NullProcessLifetimeManager Instance { get; } = new();

        public void AssignProcess(Process process) { }
        public void Dispose() { }
    }
}
