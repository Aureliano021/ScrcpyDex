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
using System.Runtime.InteropServices;

namespace ScrcpyDex.Platform.Linux
{
    /// <summary>
    /// POSIX libc P/Invoke bindings for process group management and signal dispatching.
    /// </summary>
    internal static class Libc
    {
        private const string LibcLibrary = "libc";

        public const int SIGHUP = 1;
        public const int SIGINT = 2;
        public const int SIGKILL = 9;
        public const int SIGTERM = 15;

        [DllImport(LibcLibrary, SetLastError = true)]
        public static extern int kill(int pid, int sig);

        [DllImport(LibcLibrary, SetLastError = true)]
        public static extern int getpgid(int pid);

        [DllImport(LibcLibrary, SetLastError = true)]
        public static extern int setpgid(int pid, int pgid);

        [DllImport(LibcLibrary, SetLastError = true)]
        public static extern int getpgrp();

        [DllImport(LibcLibrary, SetLastError = true)]
        public static extern int getpid();
    }
}
