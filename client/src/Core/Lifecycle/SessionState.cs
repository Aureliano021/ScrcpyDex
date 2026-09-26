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

namespace ScrcpyDex.Core.Lifecycle
{
    public enum SessionState
    {
        Idle,
        DeviceConnecting,
        ServerDeploying,
        DisplayNegotiating,
        StreamingActive,
        Teardown,
        Error
    }

    public enum SessionTrigger
    {
        TriggerStart,
        DeviceReady,
        ServerDeployed,
        StreamingStarted,
        UserStop,
        DeviceLost,
        ProcessExited,
        ErrorOccurred,
        Reset
    }

    public sealed class SessionStateTransitionEventArgs : EventArgs
    {
        public SessionState PreviousState { get; }
        public SessionState CurrentState { get; }
        public SessionTrigger Trigger { get; }
        public string? Message { get; }

        public SessionStateTransitionEventArgs(SessionState previous, SessionState current, SessionTrigger trigger, string? message = null)
        {
            PreviousState = previous;
            CurrentState = current;
            Trigger = trigger;
            Message = message;
        }
    }
}
