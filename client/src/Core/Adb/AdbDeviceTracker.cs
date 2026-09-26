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
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Models;

namespace ScrcpyDex.Core.Adb
{
    /// <summary>
    /// Connects directly to the local ADB daemon wire protocol on TCP 127.0.0.1:5037.
    /// Issues 'host:track-devices' to receive instantaneous, push-based device status updates
    /// without incurring CPU spikes, disk thrashing, or subprocess fork overhead.
    /// </summary>
    public sealed class AdbDeviceTracker : IAsyncDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _trackingTask;
        private bool _disposed;

        public event EventHandler<IReadOnlyList<DeviceInfo>>? DevicesChanged;
        public event EventHandler<string>? RawDataReceived;

        public AdbDeviceTracker(string host = "127.0.0.1", int port = 5037)
        {
            _host = host;
            _port = port;
        }

        public void Start()
        {
            if (_trackingTask == null)
            {
                _trackingTask = Task.Run(() => TrackingLoopAsync(_cts.Token));
            }
        }

        private async Task TrackingLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(_host, _port, ct).ConfigureAwait(false);
                    _stream = _tcpClient.GetStream();

                    // Send "0012host:track-devices" (18 bytes = 0x0012)
                    byte[] request = Encoding.ASCII.GetBytes("0012host:track-devices");
                    await _stream.WriteAsync(request, 0, request.Length, ct).ConfigureAwait(false);

                    // Read 4-byte status response (OKAY or FAIL)
                    byte[] responseStatus = new byte[4];
                    await ReadExactlyAsync(_stream, responseStatus, 4, ct).ConfigureAwait(false);
                    string status = Encoding.ASCII.GetString(responseStatus);

                    if (status != "OKAY")
                    {
                        // ADB daemon returned FAIL or unrecognized response; back off and reconnect
                        await Task.Delay(2000, ct).ConfigureAwait(false);
                        continue;
                    }

                    // Loop reading device changes pushed by adb server
                    byte[] lengthBuffer = new byte[4];
                    while (!ct.IsCancellationRequested)
                    {
                        await ReadExactlyAsync(_stream, lengthBuffer, 4, ct).ConfigureAwait(false);
                        string hexLen = Encoding.ASCII.GetString(lengthBuffer);
                        int payloadLength = Convert.ToInt32(hexLen, 16);

                        string payload = string.Empty;
                        if (payloadLength > 0)
                        {
                            byte[] payloadBuffer = new byte[payloadLength];
                            await ReadExactlyAsync(_stream, payloadBuffer, payloadLength, ct).ConfigureAwait(false);
                            payload = Encoding.UTF8.GetString(payloadBuffer);
                        }

                        RawDataReceived?.Invoke(this, payload);
                        var parsedDevices = ParseDeviceList(payload);
                        DevicesChanged?.Invoke(this, parsedDevices);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Connection lost (e.g. adb server restarted or killed)
                    // Wait 2 seconds before attempting reconnection
                    CleanupConnection();
                    try
                    {
                        await Task.Delay(2000, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private static List<DeviceInfo> ParseDeviceList(string raw)
        {
            var list = new List<DeviceInfo>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            using (var reader = new StringReader(raw))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string serial = parts[0];
                        string state = parts[1];
                        string model = parts.Length > 2 ? parts[2] : "Unknown";
                        list.Add(new DeviceInfo(serial, model, state));
                    }
                }
            }

            return list;
        }

        private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, ct).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("ADB stream reached EOF prematurely.");
                totalRead += read;
            }
        }

        private void CleanupConnection()
        {
            try { _stream?.Dispose(); } catch { }
            try { _tcpClient?.Dispose(); } catch { }
            _stream = null;
            _tcpClient = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            CleanupConnection();

            if (_trackingTask != null)
            {
                try { await _trackingTask.ConfigureAwait(false); } catch { }
            }

            _cts.Dispose();
        }
    }
}
