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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Adb;

namespace ScrcpyDex.Services
{
    public interface IWirelessPairingService
    {
        event EventHandler<WirelessPairingStateChangedEventArgs>? StateChanged;
        WirelessPairingSessionInfo CreateSession();
        Task<WirelessTransitionResult> StartPairingAsync(WirelessPairingSessionInfo session, CancellationToken ct = default);
    }

    /// <summary>
    /// Orchestrates Android 11+ wireless ADB QR code pairing via mDNS discovery (_adb-tls-pairing._tcp / _adb-tls-connect._tcp)
    /// with fallback polling against 'adb mdns services'.
    /// </summary>
    public sealed class WirelessPairingService : IWirelessPairingService
    {
        private static readonly IPAddress MdnsMulticastAddress = IPAddress.Parse("224.0.0.251");
        private const int MdnsPort = 5353;
        private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        private readonly IAdbService _adbService;

        public event EventHandler<WirelessPairingStateChangedEventArgs>? StateChanged;

        public WirelessPairingService(IAdbService? adbService = null)
        {
            _adbService = adbService ?? AdbService.Instance;
        }

        /// <summary>
        /// Generates a fresh Android wireless debugging QR pairing session (WIFI:T:ADB;S:...;P:...;;).
        /// </summary>
        public WirelessPairingSessionInfo CreateSession()
        {
            byte[] idBytes = RandomNumberGenerator.GetBytes(4);
            string suffix = Convert.ToHexString(idBytes).ToLowerInvariant();
            string serviceName = $"scrcpydex-{suffix}";

            var pwdChars = new char[10];
            for (int i = 0; i < pwdChars.Length; i++)
            {
                pwdChars[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
            }
            string password = new string(pwdChars);

            string qrPayload = $"WIFI:T:ADB;S:{serviceName};P:{password};;";
            return new WirelessPairingSessionInfo(serviceName, password, qrPayload);
        }

        public async Task<WirelessTransitionResult> StartPairingAsync(WirelessPairingSessionInfo session, CancellationToken ct = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            RaiseState(WirelessPairingState.WaitingForScan, null, "Waiting for QR code scan on Android device...");

            var discoveredConnectEndpoints = new ConcurrentDictionary<string, MdnsAdbServiceEndpoint>(StringComparer.OrdinalIgnoreCase);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                var pairingEndpoint = await WaitForPairingEndpointAsync(session.ServiceName, discoveredConnectEndpoints, linkedCts.Token)
                    .ConfigureAwait(false);

                if (pairingEndpoint == null)
                {
                    RaiseState(WirelessPairingState.Failed, null, "Wireless pairing timed out or did not discover the device.");
                    return new WirelessTransitionResult(false, null, null, 0, "Pairing endpoint not discovered.");
                }

                RaiseState(
                    WirelessPairingState.Pairing,
                    null,
                    $"Pairing with {pairingEndpoint.IpAddress}:{pairingEndpoint.Port}...");

                bool paired = await _adbService.PairWirelessDeviceAsync(
                    pairingEndpoint.IpAddress,
                    pairingEndpoint.Port,
                    session.Password,
                    linkedCts.Token).ConfigureAwait(false);

                if (!paired)
                {
                    RaiseState(WirelessPairingState.Failed, null, $"ADB pairing failed at {pairingEndpoint.IpAddress}:{pairingEndpoint.Port}.");
                    return new WirelessTransitionResult(
                        false,
                        null,
                        pairingEndpoint.IpAddress,
                        pairingEndpoint.Port,
                        "ADB pairing failed.");
                }

                RaiseState(WirelessPairingState.Connecting, null, $"Paired! Connecting to {pairingEndpoint.IpAddress}...");

                // After pairing, connect to _adb-tls-connect._tcp on the same IP
                var connectEndpoint = await WaitForConnectEndpointAsync(
                    pairingEndpoint.IpAddress,
                    discoveredConnectEndpoints,
                    TimeSpan.FromSeconds(10),
                    linkedCts.Token).ConfigureAwait(false);

                if (connectEndpoint != null)
                {
                    string targetSerial = $"{connectEndpoint.IpAddress}:{connectEndpoint.Port}";
                    bool connected = await _adbService.ConnectWirelessDeviceAsync(
                        connectEndpoint.IpAddress,
                        connectEndpoint.Port,
                        linkedCts.Token).ConfigureAwait(false);

                    if (connected)
                    {
                        RaiseState(WirelessPairingState.Paired, targetSerial, $"Connected wirelessly to {targetSerial}.");
                        return new WirelessTransitionResult(true, targetSerial, connectEndpoint.IpAddress, connectEndpoint.Port, "Connected wirelessly.");
                    }
                }

                // Fallback: modern ADB may auto-connect over mDNS after pairing
                var devices = await _adbService.GetConnectedDevicesAsync(linkedCts.Token).ConfigureAwait(false);
                var wirelessDevice = devices.FirstOrDefault(d => d.IsConnected && d.IsWireless);
                if (wirelessDevice != null)
                {
                    RaiseState(WirelessPairingState.Paired, wirelessDevice.Serial, $"Connected wirelessly to {wirelessDevice.Serial}.");
                    return new WirelessTransitionResult(true, wirelessDevice.Serial, pairingEndpoint.IpAddress, 0, "Connected wirelessly via ADB mDNS.");
                }

                RaiseState(WirelessPairingState.Failed, null, "Paired, but could not establish wireless ADB connection.");
                return new WirelessTransitionResult(false, null, pairingEndpoint.IpAddress, 0, "Could not connect after pairing.");
            }
            catch (OperationCanceledException)
            {
                RaiseState(WirelessPairingState.Cancelled, null, "Wireless pairing cancelled.");
                return new WirelessTransitionResult(false, null, null, 0, "Cancelled.");
            }
            catch (Exception ex)
            {
                RaiseState(WirelessPairingState.Failed, null, $"Wireless pairing error: {ex.Message}");
                return new WirelessTransitionResult(false, null, null, 0, ex.Message);
            }
        }

        private async Task<MdnsAdbServiceEndpoint?> WaitForPairingEndpointAsync(
            string serviceName,
            ConcurrentDictionary<string, MdnsAdbServiceEndpoint> connectEndpoints,
            CancellationToken ct)
        {
            using var pairingFoundCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            MdnsAdbServiceEndpoint? found = null;

            var mdnsTask = ListenMdnsAsync(
                endpoint =>
                {
                    if (endpoint.ServiceType.Equals("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase))
                    {
                        connectEndpoints[endpoint.IpAddress] = endpoint;
                    }
                    else if (endpoint.ServiceType.Equals("_adb-tls-pairing._tcp", StringComparison.OrdinalIgnoreCase) &&
                             endpoint.InstanceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        found ??= endpoint;
                        pairingFoundCts.Cancel();
                    }
                },
                pairingFoundCts.Token);

            var adbPollTask = PollAdbMdnsAsync(
                endpoint =>
                {
                    if (endpoint.ServiceType.Equals("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase))
                    {
                        connectEndpoints[endpoint.IpAddress] = endpoint;
                    }
                    else if (endpoint.ServiceType.Equals("_adb-tls-pairing._tcp", StringComparison.OrdinalIgnoreCase) &&
                             endpoint.InstanceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        found ??= endpoint;
                        pairingFoundCts.Cancel();
                    }
                },
                pairingFoundCts.Token);

            await Task.WhenAll(mdnsTask, adbPollTask).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return found;
        }

        private async Task<MdnsAdbServiceEndpoint?> WaitForConnectEndpointAsync(
            string targetIp,
            ConcurrentDictionary<string, MdnsAdbServiceEndpoint> connectEndpoints,
            TimeSpan timeout,
            CancellationToken ct)
        {
            if (connectEndpoints.TryGetValue(targetIp, out var alreadyFound))
            {
                return alreadyFound;
            }

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            MdnsAdbServiceEndpoint? found = null;

            var mdnsTask = ListenMdnsAsync(
                endpoint =>
                {
                    if (endpoint.ServiceType.Equals("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase))
                    {
                        connectEndpoints[endpoint.IpAddress] = endpoint;
                        if (endpoint.IpAddress.Equals(targetIp, StringComparison.OrdinalIgnoreCase))
                        {
                            found ??= endpoint;
                            linkedCts.Cancel();
                        }
                    }
                },
                linkedCts.Token);

            var adbPollTask = PollAdbMdnsAsync(
                endpoint =>
                {
                    if (endpoint.ServiceType.Equals("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase))
                    {
                        connectEndpoints[endpoint.IpAddress] = endpoint;
                        if (endpoint.IpAddress.Equals(targetIp, StringComparison.OrdinalIgnoreCase))
                        {
                            found ??= endpoint;
                            linkedCts.Cancel();
                        }
                    }
                },
                linkedCts.Token);

            await Task.WhenAll(mdnsTask, adbPollTask).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return found;
        }

        private async Task PollAdbMdnsAsync(Action<MdnsAdbServiceEndpoint> onEndpoint, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var endpoints = await _adbService.GetMdnsServicesAsync(ct).ConfigureAwait(false);
                    foreach (var ep in endpoints)
                    {
                        onEndpoint(ep);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Best-effort polling
                }

                try
                {
                    await Task.Delay(1200, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static async Task ListenMdnsAsync(Action<MdnsAdbServiceEndpoint> onEndpoint, CancellationToken ct)
        {
            UdpClient? udp = null;
            try
            {
                udp = new UdpClient();
                udp.ExclusiveAddressUse = false;
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));

                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || !ni.SupportsMulticast) continue;
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        try
                        {
                            udp.JoinMulticastGroup(MdnsMulticastAddress, addr.Address);
                        }
                        catch { }
                    }
                }

                byte[] queryPacket = MdnsPacketParser.BuildPtrQuery(
                    MdnsPacketParser.PairingServiceType,
                    MdnsPacketParser.ConnectServiceType);
                var multicastEndPoint = new IPEndPoint(MdnsMulticastAddress, MdnsPort);

                // Periodic PTR queries + receive loop
                using var queryLoopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = Task.Run(async () =>
                {
                    while (!queryLoopCts.IsCancellationRequested)
                    {
                        try
                        {
                            await udp.SendAsync(queryPacket, queryPacket.Length, multicastEndPoint).ConfigureAwait(false);
                            await Task.Delay(2000, queryLoopCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }, queryLoopCts.Token);

                while (!ct.IsCancellationRequested)
                {
                    var result = await udp.ReceiveAsync(ct).ConfigureAwait(false);
                    string senderIp = result.RemoteEndPoint.Address.ToString();
                    var endpoints = MdnsPacketParser.ParseResponse(result.Buffer, senderIp);
                    foreach (var ep in endpoints)
                    {
                        onEndpoint(ep);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch
            {
                // Port 5353 or multicast may be restricted; adb mdns polling fallback remains active
            }
            finally
            {
                try { udp?.Dispose(); } catch { }
            }
        }

        private void RaiseState(WirelessPairingState state, string? targetSerial, string? message)
        {
            StateChanged?.Invoke(this, new WirelessPairingStateChangedEventArgs(state, targetSerial, message));
        }
    }
}
