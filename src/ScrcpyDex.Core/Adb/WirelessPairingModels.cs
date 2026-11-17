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

namespace ScrcpyDex.Core.Adb
{
    public enum WirelessPairingState
    {
        Idle = 0,
        WaitingForScan = 1,
        Pairing = 2,
        Connecting = 3,
        Paired = 4,
        Failed = 5,
        Cancelled = 6
    }

    public sealed record WirelessPairingSessionInfo(
        string ServiceName,
        string Password,
        string QrPayload);

    public sealed record MdnsAdbServiceEndpoint(
        string InstanceName,
        string ServiceType,
        string IpAddress,
        int Port);

    public sealed record WirelessTransitionResult(
        bool Succeeded,
        string? TargetSerial,
        string? IpAddress,
        int Port,
        string Message,
        bool IsTls = false);

    public sealed class WirelessPairingStateChangedEventArgs : EventArgs
    {
        public WirelessPairingState State { get; }
        public string? TargetSerial { get; }
        public string? Message { get; }

        public WirelessPairingStateChangedEventArgs(
            WirelessPairingState state,
            string? targetSerial = null,
            string? message = null)
        {
            State = state;
            TargetSerial = targetSerial;
            Message = message;
        }
    }
}
