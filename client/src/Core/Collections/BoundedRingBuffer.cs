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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace ScrcpyDex.Core.Collections
{
    /// <summary>
    /// Fixed-capacity observable circular collection for log entries.
    /// Drops the oldest entries when capacity is exceeded to prevent excessive memory usage.
    /// </summary>
    public sealed class BoundedRingBuffer<T> : ObservableCollection<T>
    {
        private readonly int _maxCapacity;
        private readonly object _syncLock = new object();
        private readonly Dispatcher? _dispatcher;

        public BoundedRingBuffer(int maxCapacity = 500, Dispatcher? dispatcher = null)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity must be positive.");
            _maxCapacity = maxCapacity;
            _dispatcher = dispatcher;
        }

        public void Push(T item)
        {
            if (_dispatcher != null && !_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(() => PushInternal(item)));
            }
            else
            {
                PushInternal(item);
            }
        }

        private void PushInternal(T item)
        {
            lock (_syncLock)
            {
                if (Count >= _maxCapacity)
                {
                    RemoveAt(0);
                }
                Add(item);
            }
        }

        public void ClearAll()
        {
            if (_dispatcher != null && !_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(ClearAllInternal));
            }
            else
            {
                ClearAllInternal();
            }
        }

        private void ClearAllInternal()
        {
            lock (_syncLock)
            {
                Clear();
            }
        }
    }
}
