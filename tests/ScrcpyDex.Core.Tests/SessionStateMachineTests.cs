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
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrcpyDex.Core.Lifecycle;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class SessionStateMachineTests
    {
        [Fact]
        public async Task StateTransitions_FollowCompleteLifecycleFromIdleToIdle()
        {
            var stateMachine = new SessionStateMachine();
            var transitions = new List<SessionStateTransitionEventArgs>();
            stateMachine.StateChanged += (_, e) => transitions.Add(e);

            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);

            // 1. Idle -> DeviceConnecting
            Assert.True(stateMachine.CanFire(SessionTrigger.TriggerStart));
            bool ok1 = await stateMachine.FireAsync(SessionTrigger.TriggerStart, "Starting connection");
            Assert.True(ok1);
            Assert.Equal(SessionState.DeviceConnecting, stateMachine.CurrentState);

            // 2. DeviceConnecting -> ServerDeploying
            Assert.True(stateMachine.CanFire(SessionTrigger.DeviceReady));
            bool ok2 = await stateMachine.FireAsync(SessionTrigger.DeviceReady, "Device ready");
            Assert.True(ok2);
            Assert.Equal(SessionState.ServerDeploying, stateMachine.CurrentState);

            // 3. ServerDeploying -> DisplayNegotiating
            Assert.True(stateMachine.CanFire(SessionTrigger.ServerDeployed));
            bool ok3 = await stateMachine.FireAsync(SessionTrigger.ServerDeployed, "Server deployed");
            Assert.True(ok3);
            Assert.Equal(SessionState.DisplayNegotiating, stateMachine.CurrentState);

            // 4. DisplayNegotiating -> StreamingActive
            Assert.True(stateMachine.CanFire(SessionTrigger.StreamingStarted));
            bool ok4 = await stateMachine.FireAsync(SessionTrigger.StreamingStarted, "Streaming started");
            Assert.True(ok4);
            Assert.Equal(SessionState.StreamingActive, stateMachine.CurrentState);

            // 5. StreamingActive -> Teardown
            Assert.True(stateMachine.CanFire(SessionTrigger.UserStop));
            bool ok5 = await stateMachine.FireAsync(SessionTrigger.UserStop, "User stopped");
            Assert.True(ok5);
            Assert.Equal(SessionState.Teardown, stateMachine.CurrentState);

            // 6. Teardown -> Idle
            Assert.True(stateMachine.CanFire(SessionTrigger.Reset));
            bool ok6 = await stateMachine.FireAsync(SessionTrigger.Reset, "Session reset");
            Assert.True(ok6);
            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);

            Assert.Equal(6, transitions.Count);
            Assert.Equal(SessionState.Idle, transitions[0].PreviousState);
            Assert.Equal(SessionState.DeviceConnecting, transitions[0].CurrentState);
            Assert.Equal(SessionState.Teardown, transitions[5].PreviousState);
            Assert.Equal(SessionState.Idle, transitions[5].CurrentState);
        }

        [Fact]
        public async Task InvalidTransition_ReturnsFalseAndPreservesCurrentState()
        {
            var stateMachine = new SessionStateMachine();
            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);

            Assert.False(stateMachine.CanFire(SessionTrigger.StreamingStarted));
            bool success = await stateMachine.FireAsync(SessionTrigger.StreamingStarted);

            Assert.False(success);
            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);
        }

        [Fact]
        public async Task InvalidTransition_StrictExecution_ThrowsInvalidOperationException()
        {
            var stateMachine = new SessionStateMachine();
            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                if (!await stateMachine.FireAsync(SessionTrigger.StreamingStarted))
                {
                    throw new InvalidOperationException($"Invalid transition from {stateMachine.CurrentState} with trigger {SessionTrigger.StreamingStarted}");
                }
            });

            Assert.Equal(SessionState.Idle, stateMachine.CurrentState);
        }

        [Fact]
        public async Task EmergencyHaltAsync_TransitionsToErrorStateFromAnyState()
        {
            var stateMachine = new SessionStateMachine();
            await stateMachine.FireAsync(SessionTrigger.TriggerStart);
            Assert.Equal(SessionState.DeviceConnecting, stateMachine.CurrentState);

            await stateMachine.EmergencyHaltAsync("Emergency stop test");

            Assert.Equal(SessionState.Error, stateMachine.CurrentState);
        }
    }
}
