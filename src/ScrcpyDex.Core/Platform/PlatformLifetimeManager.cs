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

namespace ScrcpyDex.Core.Platform
{
    /// <summary>
    /// Provides access to the active platform-specific process lifetime manager.
    /// </summary>
    public static class PlatformLifetimeManager
    {
        private static IProcessLifetimeManager? _default;

        public static IProcessLifetimeManager Default
        {
            get
            {
                if (_default != null) return _default;

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    _default = PosixProcessLifetimeManager.Default;
                }
                else
                {
                    _default = NullProcessLifetimeManager.Instance;
                }
                return _default;
            }
            set => _default = value;
        }
    }
}
