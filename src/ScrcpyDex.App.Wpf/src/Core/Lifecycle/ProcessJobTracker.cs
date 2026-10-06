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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ScrcpyDex.Core.Lifecycle
{
    /// <summary>
    /// Safe handle wrapping a Windows Job Object kernel handle.
    /// Guarantees that CloseHandle is invoked during garbage collection or disposal.
    /// </summary>
    public sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeJobHandle() : base(true) { }
        public SafeJobHandle(IntPtr handle) : base(true) => SetHandle(handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    /// <summary>
    /// Manages a Windows Job Object configured with JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE.
    /// When the parent process terminates (clean exit, crash, or Task Manager kill),
    /// the Windows NT kernel atomically terminates all child processes in the job.
    /// </summary>
    public sealed class ProcessJobTracker : IDisposable
    {
        private readonly SafeJobHandle _jobHandle;
        private bool _disposed;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeJobHandle CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(
            SafeJobHandle hJob,
            JobObjectInfoClass JobObjectInformationClass,
            IntPtr lpJobObjectInformation,
            uint cbJobObjectInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(SafeJobHandle hJob, IntPtr hProcess);

        private static readonly Lazy<ProcessJobTracker> _defaultInstance = new(() => new ProcessJobTracker());
        public static ProcessJobTracker Default => _defaultInstance.Value;

        public ProcessJobTracker(string? jobName = null)
        {
            _jobHandle = CreateJobObject(IntPtr.Zero, jobName);
            if (_jobHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create Windows Job Object.");
            }

            var basicInfo = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            };

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = basicInfo
            };

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr pExtendedInfo = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(extendedInfo, pExtendedInfo, false);
                if (!SetInformationJobObject(
                    _jobHandle,
                    JobObjectInfoClass.JobObjectExtendedLimitInformation,
                    pExtendedInfo,
                    (uint)length))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to configure Job Object limits.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pExtendedInfo);
            }
        }

        /// <summary>
        /// Assigns an existing process to the Job Object.
        /// Any children subsequently spawned by this process will automatically inherit job membership.
        /// </summary>
        public void AssignProcess(Process process)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessJobTracker));
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            if (!AssignProcessToJobObject(_jobHandle, process.Handle))
            {
                int error = Marshal.GetLastWin32Error();
                // Error 5 (ERROR_ACCESS_DENIED) happens if the target process has already terminated
                if (error != 5 && !process.HasExited)
                {
                    throw new Win32Exception(error, $"Failed to assign process PID {process.Id} to Job Object.");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _jobHandle?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        private enum JobObjectInfoClass
        {
            JobObjectExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
