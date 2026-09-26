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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.IO;
using ScrcpyDex.Core.Lifecycle;

namespace ScrcpyDex.Core.Adb
{
    public sealed class AdbExecutionResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }
        public bool Succeeded => ExitCode == 0;

        public AdbExecutionResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }

    /// <summary>
    /// Executes ADB commands with retry logic, circuit-breaker protection, and process tracking.
    /// </summary>
    public sealed class AdbResilienceService
    {
        private readonly string _adbPath;
        private readonly ProcessJobTracker _jobTracker;
        private int _consecutiveFailures;
        private DateTime _circuitBreakerUntilUtc = DateTime.MinValue;
        private readonly object _circuitLock = new object();

        private const int MaxConsecutiveFailures = 3;
        private static readonly TimeSpan CircuitBreakDuration = TimeSpan.FromSeconds(5);

        public AdbResilienceService(string adbPath, ProcessJobTracker jobTracker)
        {
            _adbPath = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath;
            _jobTracker = jobTracker;
        }

        public bool IsCircuitBreakerOpen
        {
            get
            {
                lock (_circuitLock)
                {
                    return DateTime.UtcNow < _circuitBreakerUntilUtc;
                }
            }
        }

        public void ResetCircuitBreaker()
        {
            lock (_circuitLock)
            {
                _consecutiveFailures = 0;
                _circuitBreakerUntilUtc = DateTime.MinValue;
            }
        }

        public async Task<AdbExecutionResult> ExecuteDirectAsync(
            string arguments,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
            using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            return await ExecuteCoreAsync(arguments, linkedCts.Token).ConfigureAwait(false);
        }

        public async Task<AdbExecutionResult> ExecuteWithRetryAsync(
            string arguments,
            int maxRetries = 3,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(8);

            // Check Circuit Breaker
            lock (_circuitLock)
            {
                if (DateTime.UtcNow < _circuitBreakerUntilUtc)
                {
                    return new AdbExecutionResult(-1, string.Empty, "Circuit breaker is open. ADB is temporarily failing.");
                }
            }

            Exception? lastException = null;
            var random = new Random();

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    var result = await ExecuteCoreAsync(arguments, linkedCts.Token).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        // Reset circuit breaker upon success
                        lock (_circuitLock)
                        {
                            _consecutiveFailures = 0;
                        }
                        return result;
                    }

                    // Only trip the circuit breaker on true ADB transport/daemon failures
                    if (IsAdbTransportFailure(result.ExitCode, result.Error, result.Output))
                    {
                        HandleTransportFailure();
                    }

                    if (attempt == maxRetries) return result;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    HandleTransportFailure();
                    if (attempt == maxRetries)
                    {
                        return new AdbExecutionResult(-1, string.Empty, $"Execution failed after {maxRetries} attempts: {ex.Message}");
                    }
                }

                // Exponential backoff with jitter: 200ms * 2^(attempt-1) + jitter
                int baseDelay = 200 * (1 << (attempt - 1));
                int jitter = random.Next(50, 150);
                await Task.Delay(baseDelay + jitter, ct).ConfigureAwait(false);
            }

            return new AdbExecutionResult(-1, string.Empty, lastException?.Message ?? "Unknown failure.");
        }

        private static bool IsAdbTransportFailure(int exitCode, string error, string output)
        {
            if (exitCode == 0) return false;
            string combined = $"{error} {output}".ToLowerInvariant();
            return combined.Contains("cannot connect to daemon") ||
                   combined.Contains("device not found") ||
                   combined.Contains("device offline") ||
                   combined.Contains("device unauthorized") ||
                   combined.Contains("no devices/emulators found") ||
                   combined.Contains("protocol fault") ||
                   combined.Contains("connection reset");
        }

        private async Task<AdbExecutionResult> ExecuteCoreAsync(string arguments, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            await using var stream = new NonBlockingProcessStream(1024);
            stream.EntryReceived += entry =>
            {
                if (entry.IsError)
                {
                    lock (errorBuilder) errorBuilder.AppendLine(entry.Text);
                }
                else
                {
                    lock (outputBuilder) outputBuilder.AppendLine(entry.Text);
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start ADB process.");
            }

            // Bind to job tracker immediately
            _jobTracker?.AssignProcess(process);
            stream.Bind(process);

            try
            {
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited) process.Kill();
                }
                catch { }
                throw;
            }

            return new AdbExecutionResult(
                process.ExitCode,
                outputBuilder.ToString().Trim(),
                errorBuilder.ToString().Trim());
        }

        private void HandleTransportFailure()
        {
            lock (_circuitLock)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _circuitBreakerUntilUtc = DateTime.UtcNow.Add(CircuitBreakDuration);
                }
            }
        }
    }
}
