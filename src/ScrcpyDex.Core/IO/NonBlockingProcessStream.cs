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
    /// Reads standard output and standard error streams asynchronously using Channels
    /// to avoid pipe buffer deadlocks and classifies lines by severity.
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

        /// <summary>
        /// Classifies a stream line into an appropriate ProcessLogLevel (Info, Warning, Error)
        /// based on standard scrcpy/adb prefixes and message content.
        /// </summary>
        public static ProcessLogLevel ClassifyLogLevel(string? text, bool isStderr)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ProcessLogLevel.Info;
            }

            ReadOnlySpan<char> span = text.AsSpan().Trim();

            // 1. Critical errors and exceptions
            if (span.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] ERROR:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("FATAL:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] FATAL:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("CRITICAL:", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("Exception in thread", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("java.lang.", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("Execution error:", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("Could not open video stream", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Failed to start", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("failed to connect to", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessLogLevel.Error;
            }

            // 2. Benign informational messages (even when emitted to stderr by scrcpy/adb)
            if (span.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] INFO:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("DEBUG:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] DEBUG:", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("file pushed", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("files pushed", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("* daemon", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("daemon started successfully", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("daemon not running", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Renderer:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Device:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Display: ", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Texture: ", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("Initial texture", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("MB/s (", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("KB/s (", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("bytes in", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessLogLevel.Info;
            }

            // 3. Warnings and alerts
            if (span.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] WARN:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("[server] WARNING:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Notice:", StringComparison.OrdinalIgnoreCase) ||
                span.StartsWith("Alert:", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("FLAG_SUPPORTS_PROTECTED_BUFFERS", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("mirroring can be restricted", StringComparison.OrdinalIgnoreCase) ||
                span.Contains("deprecated", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessLogLevel.Warning;
            }

            // 4. Default: Stderr output that is not an error is treated as a warning (alert),
            // while stdout output defaults to Info.
            return isStderr ? ProcessLogLevel.Warning : ProcessLogLevel.Info;
        }

        private async Task PumpStreamAsync(StreamReader reader, bool isStderr, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && !reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line != null)
                    {
                        var level = ClassifyLogLevel(line, isStderr);
                        var entry = new ProcessLogEntry(DateTime.UtcNow, line, level);
                        _channel.Writer.TryWrite(entry);
                        EntryReceived?.Invoke(entry);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                var entry = new ProcessLogEntry(DateTime.UtcNow, $"[Stream error: {ex.Message}]", ProcessLogLevel.Error);
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
