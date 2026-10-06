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

using System.Linq;
using ScrcpyDex.Core.Collections;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class BoundedRingBufferTests
    {
        [Fact]
        public void Push_ExceedingMaxCapacity_DropsOldestElements()
        {
            const int maxCapacity = 3;
            var buffer = new BoundedRingBuffer<int>(maxCapacity);

            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            Assert.Equal(3, buffer.Count);
            Assert.Equal(new[] { 1, 2, 3 }, buffer.ToArray());

            buffer.Push(4);

            Assert.Equal(3, buffer.Count);
            Assert.Equal(new[] { 2, 3, 4 }, buffer.ToArray());

            buffer.Push(5);

            Assert.Equal(3, buffer.Count);
            Assert.Equal(new[] { 3, 4, 5 }, buffer.ToArray());
        }

        [Fact]
        public void ClearAll_RemovesAllElementsFromBuffer()
        {
            var buffer = new BoundedRingBuffer<string>(maxCapacity: 5);
            buffer.Push("log1");
            buffer.Push("log2");
            buffer.Push("log3");

            Assert.Equal(3, buffer.Count);

            buffer.ClearAll();

            Assert.Empty(buffer);
        }

        [Fact]
        public void PushAndClear_WithNullDispatcherAndSyncContext_OperatesSynchronously()
        {
            var buffer = new BoundedRingBuffer<string>(
                maxCapacity: 10,
                dispatcherInvoke: null,
                syncContext: null);

            buffer.Push("itemA");
            buffer.Push("itemB");

            // Asserts immediate synchronous update on the executing thread
            Assert.Equal(2, buffer.Count);
            Assert.Equal("itemA", buffer[0]);
            Assert.Equal("itemB", buffer[1]);

            buffer.ClearAll();

            Assert.Empty(buffer);
        }
    }
}
