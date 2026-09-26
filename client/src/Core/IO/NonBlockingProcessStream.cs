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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ScrcpyDex.Core.IO
{
    /// <summary>
    /// Connects to a running Process's StandardOutput and StandardError streams,
    /// draining both concurrently via System.Threading.Channels.
    /// Completely prevents the classic Windows pipe buffer deadlock where child processes
    /// block indefinitely on unconsumed stderr while parent blocks on stdout.
    /// </summary>
    public sealed class NonBlockingProcessStream : IAsyncDisposable
    {
        private readonly Channel<ProcessLogEntry> _channel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _stdoutTask;
        private Task? _stderrTask;
        private bool _disposed;

        public ChannelReader<ProcessLogEntry> Reader => _channel.Reader;

        public event Action<ProcessLogEntry>? EntryReceived;

        public NonBlockingProcessStream(int boundedCapacity = 2048)
        {
            var options = new BoundedChannelOptions(boundedCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = false,
                SingleReader = true
            };
            _channel = Channel.CreateBounded<ProcessLogEntry>(options);
        }

        /// <summary>
        /// Binds the asynchronous readers to the process's standard output and error streams.
        /// The process must have been started with RedirectStandardOutput = true and RedirectStandardError = true.
        /// </summary>
        public void Bind(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            _stdoutTask = Task.Run(() => PumpStreamAsync(process.StandardOutput, false, _cts.Token));
            _stderrTask = Task.Run(() => PumpStreamAsync(process.StandardError, true, _cts.Token));
        }

        private async Task PumpStreamAsync(StreamReader reader, bool isError, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && !reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line != null)
                    {
                        var entry = new ProcessLogEntry(DateTime.UtcNow, line, isError);
                        _channel.Writer.TryWrite(entry);
                        EntryReceived?.Invoke(entry);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                var entry = new ProcessLogEntry(DateTime.UtcNow, $"[Stream error: {ex.Message}]", true);
                _channel.Writer.TryWrite(entry);
                EntryReceived?.Invoke(entry);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            _channel.Writer.TryComplete();

            if (_stdoutTask != null && _stderrTask != null)
            {
                try
                {
                    await Task.WhenAll(_stdoutTask, _stderrTask).ConfigureAwait(false);
                }
                catch { }
            }

            _cts.Dispose();
        }
    }
}
