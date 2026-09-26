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

namespace ScrcpyDex.Core.IO
{
    public readonly struct ProcessLogEntry : IEquatable<ProcessLogEntry>
    {
        public DateTime Timestamp { get; }
        public string Text { get; }
        public bool IsError { get; }

        public ProcessLogEntry(DateTime timestamp, string text, bool isError)
        {
            Timestamp = timestamp;
            Text = text ?? string.Empty;
            IsError = isError;
        }

        public override string ToString() => $"[{Timestamp:HH:mm:ss.fff}] {(IsError ? "[ERR] " : "[OUT] ")}{Text}";

        public bool Equals(ProcessLogEntry other) => Timestamp == other.Timestamp && Text == other.Text && IsError == other.IsError;
        public override bool Equals(object? obj) => obj is ProcessLogEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Timestamp, Text, IsError);

        public static bool operator ==(ProcessLogEntry left, ProcessLogEntry right) => left.Equals(right);
        public static bool operator !=(ProcessLogEntry left, ProcessLogEntry right) => !left.Equals(right);
    }
}
