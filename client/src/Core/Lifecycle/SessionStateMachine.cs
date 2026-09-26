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
using System.Threading;
using System.Threading.Tasks;

namespace ScrcpyDex.Core.Lifecycle
{
    public interface ISessionStateMachine
    {
        SessionState CurrentState { get; }
        event EventHandler<SessionStateTransitionEventArgs>? StateChanged;

        bool CanFire(SessionTrigger trigger);
        Task<bool> FireAsync(SessionTrigger trigger, string? message = null, CancellationToken ct = default);
        Task EmergencyHaltAsync(string? reason = null);
    }

    public sealed class SessionStateMachine : ISessionStateMachine
    {
        private readonly object _syncRoot = new object();
        private SessionState _currentState = SessionState.Idle;

        public SessionState CurrentState
        {
            get
            {
                lock (_syncRoot) return _currentState;
            }
            private set
            {
                lock (_syncRoot) _currentState = value;
            }
        }

        public event EventHandler<SessionStateTransitionEventArgs>? StateChanged;

        public bool CanFire(SessionTrigger trigger)
        {
            lock (_syncRoot)
            {
                return GetNextState(_currentState, trigger).HasValue;
            }
        }

        public Task<bool> FireAsync(SessionTrigger trigger, string? message = null, CancellationToken ct = default)
        {
            SessionState previous;
            SessionState next;

            lock (_syncRoot)
            {
                previous = _currentState;
                var potentialNext = GetNextState(previous, trigger);
                if (!potentialNext.HasValue)
                {
                    return Task.FromResult(false);
                }
                next = potentialNext.Value;
                _currentState = next;
            }

            StateChanged?.Invoke(this, new SessionStateTransitionEventArgs(previous, next, trigger, message));
            return Task.FromResult(true);
        }

        public Task EmergencyHaltAsync(string? reason = null)
        {
            SessionState previous;
            lock (_syncRoot)
            {
                previous = _currentState;
                _currentState = SessionState.Error;
            }

            StateChanged?.Invoke(this, new SessionStateTransitionEventArgs(previous, SessionState.Error, SessionTrigger.ErrorOccurred, reason ?? "Emergency halt"));
            return Task.CompletedTask;
        }

        private static SessionState? GetNextState(SessionState current, SessionTrigger trigger)
        {
            switch (current)
            {
                case SessionState.Idle:
                    if (trigger == SessionTrigger.TriggerStart) return SessionState.DeviceConnecting;
                    break;

                case SessionState.DeviceConnecting:
                    if (trigger == SessionTrigger.DeviceReady) return SessionState.ServerDeploying;
                    if (trigger == SessionTrigger.DeviceLost || trigger == SessionTrigger.ErrorOccurred) return SessionState.Error;
                    if (trigger == SessionTrigger.UserStop) return SessionState.Teardown;
                    break;

                case SessionState.ServerDeploying:
                    if (trigger == SessionTrigger.ServerDeployed) return SessionState.DisplayNegotiating;
                    if (trigger == SessionTrigger.ErrorOccurred) return SessionState.Error;
                    if (trigger == SessionTrigger.UserStop) return SessionState.Teardown;
                    break;

                case SessionState.DisplayNegotiating:
                    if (trigger == SessionTrigger.StreamingStarted) return SessionState.StreamingActive;
                    if (trigger == SessionTrigger.ErrorOccurred) return SessionState.Error;
                    if (trigger == SessionTrigger.UserStop) return SessionState.Teardown;
                    break;

                case SessionState.StreamingActive:
                    if (trigger == SessionTrigger.UserStop || trigger == SessionTrigger.DeviceLost || trigger == SessionTrigger.ProcessExited) return SessionState.Teardown;
                    if (trigger == SessionTrigger.ErrorOccurred) return SessionState.Error;
                    break;

                case SessionState.Teardown:
                    if (trigger == SessionTrigger.Reset || trigger == SessionTrigger.ProcessExited) return SessionState.Idle;
                    if (trigger == SessionTrigger.ErrorOccurred) return SessionState.Error;
                    break;

                case SessionState.Error:
                    if (trigger == SessionTrigger.Reset) return SessionState.Idle;
                    break;
            }

            return null;
        }
    }
}
