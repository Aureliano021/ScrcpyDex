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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScrcpyDex.Core.Adb;
using ScrcpyDex.Models;
using ScrcpyDex.Services;
using Xunit;

namespace ScrcpyDex.Core.Tests
{
    public class AdbWirelessTests
    {
        [Theory]
        [InlineData("R5CW123456", null, DeviceTransport.Usb)]
        [InlineData("R5CW123456", "R5CW123456\tdevice usb:1-2 product:dm3q model:SM_S918B", DeviceTransport.Usb)]
        [InlineData("192.0.2.10:5555", null, DeviceTransport.Wireless)]
        [InlineData("[2001:db8::1]:5555", null, DeviceTransport.Wireless)]
        [InlineData("adb-R5CW123456-xYz123._adb-tls-connect._tcp", null, DeviceTransport.Wireless)]
        [InlineData("emulator-5554", null, DeviceTransport.Unknown)]
        [InlineData("", null, DeviceTransport.Unknown)]
        public void ClassifyTransport_DetectsUsbAndWirelessCorrectly(string serial, string? rawLine, DeviceTransport expected)
        {
            var actual = DeviceTransportHelper.ClassifyTransport(serial, rawLine);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseDeviceList_AssignsTransportCorrectly()
        {
            const string payload =
                "R5CW123456\tdevice usb:1-2 product:dm3q model:SM_S918B\n" +
                "192.0.2.42:5555\tdevice product:dm3q model:SM_S918B\n";

            var devices = AdbDeviceTracker.ParseDeviceList(payload);

            Assert.Equal(2, devices.Count);
            Assert.Equal(DeviceTransport.Usb, devices[0].Transport);
            Assert.True(devices[0].IsUsb);
            Assert.False(devices[0].IsWireless);

            Assert.Equal(DeviceTransport.Wireless, devices[1].Transport);
            Assert.True(devices[1].IsWireless);
            Assert.False(devices[1].IsUsb);
        }

        [Fact]
        public void ParseDeviceIpFromIpRoute_PrefersWlanAndIgnoresLoopback()
        {
            const string routeOutput =
                "127.0.0.0/8 dev lo scope link src 127.0.0.1\n" +
                "198.51.100.0/24 dev rmnet0 proto kernel scope link src 198.51.100.12\n" +
                "192.0.2.0/24 dev wlan0 proto kernel scope link src 192.0.2.55\n";

            string? ip = AdbService.ParseDeviceIpFromIpRoute(routeOutput);
            Assert.Equal("192.0.2.55", ip);
        }

        [Fact]
        public void ParseDeviceIpFromIpAddr_ExtractsIpv4()
        {
            const string addrOutput =
                "26: wlan0: <BROADCAST,MULTICAST,UP,LOWER_UP> mtu 1500\n" +
                "    inet 192.0.2.77/24 brd 192.0.2.255 scope global wlan0\n";

            string? ip = AdbService.ParseDeviceIpFromIpAddr(addrOutput);
            Assert.Equal("192.0.2.77", ip);
        }

        [Fact]
        public void ParseMdnsServicesOutput_ParsesPairingAndConnectEndpoints()
        {
            const string output =
                "List of discovered mdns services\n" +
                "scrcpydex-a1b2c3d4\t_adb-tls-pairing._tcp\t192.0.2.33:41235\n" +
                "adb-R5CW123456-xyz\t_adb-tls-connect._tcp\t192.0.2.33:37891\n";

            var endpoints = AdbService.ParseMdnsServicesOutput(output);

            Assert.Equal(2, endpoints.Count);
            Assert.Equal("scrcpydex-a1b2c3d4", endpoints[0].InstanceName);
            Assert.Equal("_adb-tls-pairing._tcp", endpoints[0].ServiceType);
            Assert.Equal("192.0.2.33", endpoints[0].IpAddress);
            Assert.Equal(41235, endpoints[0].Port);

            Assert.Equal("_adb-tls-connect._tcp", endpoints[1].ServiceType);
            Assert.Equal(37891, endpoints[1].Port);
        }

        [Theory]
        [InlineData("connected to 192.0.2.10:5555", true)]
        [InlineData("already connected to 192.0.2.10:5555", true)]
        [InlineData("cannot connect to 192.0.2.10:5555: Connection refused", false)]
        [InlineData("failed to connect to '192.0.2.10:5555': Connection timed out", false)]
        public void IsAdbConnectSuccess_MatchesExpectedResponses(string output, bool expected)
        {
            Assert.Equal(expected, AdbService.IsAdbConnectSuccess(output));
        }

        [Fact]
        public void MdnsPacketParser_BuildAndParseSrvWithARecord_ReturnsEndpoint()
        {
            byte[] query = MdnsPacketParser.BuildPtrQuery(MdnsPacketParser.PairingServiceType);
            Assert.True(query.Length > 12);

            byte[] packet = BuildSyntheticMdnsResponse(
                instanceFullName: "scrcpydex-deadbeef._adb-tls-pairing._tcp.local",
                targetHost: "android-host.local",
                port: 43210,
                ipv4: new byte[] { 192, 0, 2, 88 });

            var endpoints = MdnsPacketParser.ParseResponse(packet);

            Assert.Single(endpoints);
            Assert.Equal("scrcpydex-deadbeef", endpoints[0].InstanceName);
            Assert.Equal("_adb-tls-pairing._tcp", endpoints[0].ServiceType);
            Assert.Equal("192.0.2.88", endpoints[0].IpAddress);
            Assert.Equal(43210, endpoints[0].Port);
        }

        [Fact]
        public void MdnsPacketParser_MalformedPacket_DoesNotThrow()
        {
            var endpoints = MdnsPacketParser.ParseResponse(new byte[] { 0, 1, 2, 3, 255, 255 });
            Assert.Empty(endpoints);
        }

        [Fact]
        public void WirelessPairingService_CreateSession_GeneratesValidAndroidQrPayload()
        {
            var service = new WirelessPairingService(new FakeAdbService());
            var session = service.CreateSession();

            Assert.StartsWith("dex-", session.ServiceName);
            Assert.Equal(6, session.Password.Length);
            Assert.Equal($"WIFI:T:ADB;S:{session.ServiceName};P:{session.Password};;", session.QrPayload);
            Assert.Equal(25, QrCodeMatrixGenerator.Generate(session.QrPayload).GetLength(0));
        }

        [Fact]
        public void QrCodeMatrixGenerator_GeneratesValidMatrixWithFinderPatterns()
        {
            const string payload = "WIFI:T:ADB;S:scrcpydex-a1b2c3d4;P:AbCdEf2345;;";
            bool[,] matrix = QrCodeMatrixGenerator.Generate(payload);

            int size = matrix.GetLength(0);
            Assert.Equal(size, matrix.GetLength(1));
            Assert.True(size >= 29);

            // Top-left 7x7 finder pattern outer ring and center
            Assert.True(matrix[0, 0]);
            Assert.True(matrix[0, 6]);
            Assert.True(matrix[6, 0]);
            Assert.False(matrix[1, 1]);
            Assert.True(matrix[3, 3]);

            // Dark module at (8, size - 8)
            Assert.True(matrix[size - 8, 8]);
        }

        [Fact]
        public void ParseTlsPortFromDumpsysAdb_ExtractsTlsPortFromAdbWifiBlock()
        {
            const string dumpsys =
                "ADB MANAGER STATE (dumpsys adb):\n" +
                "  debugging_manager={\n" +
                "    adb_wifi={\n" +
                "      enabled=true\n" +
                "      is_trusted_network=true\n" +
                "      tls_port=46311\n" +
                "    }\n" +
                "  }";

            int? port = AdbService.ParseTlsPortFromDumpsysAdb(dumpsys);
            Assert.Equal(46311, port);
        }

        [Theory]
        [InlineData("R5CW123456", "R5CW123456", false)]
        [InlineData("adb-R5CW123456-xYz123._adb-tls-connect._tcp", "R5CW123456", true)]
        [InlineData("192.0.2.10:5555", "", false)]
        public void DeviceInfo_ExtractsHardwareSerialAndTlsStatus(string serial, string expectedHw, bool expectedTls)
        {
            var dev = new DeviceInfo { Serial = serial };
            Assert.Equal(expectedHw, dev.HardwareSerial);
            Assert.Equal(expectedTls, dev.IsTlsWireless);
        }

        [Fact]
        public async Task WirelessPairingService_StartPairingAsync_PairsAndConnectsViaMockAdb()
        {
            var fakeAdb = new FakeAdbService();
            var service = new WirelessPairingService(fakeAdb);
            var session = service.CreateSession();

            fakeAdb.MdnsEndpoints = new[]
            {
                new MdnsAdbServiceEndpoint(session.ServiceName, "_adb-tls-pairing._tcp", "192.0.2.99", 41111),
                new MdnsAdbServiceEndpoint("adb-R5CW123456-abc", "_adb-tls-connect._tcp", "192.0.2.99", 37777)
            };

            var observedStates = new List<WirelessPairingState>();
            service.StateChanged += (_, e) => observedStates.Add(e.State);

            var result = await service.StartPairingAsync(session);

            Assert.True(result.Succeeded);
            Assert.Equal("192.0.2.99:37777", result.TargetSerial);
            Assert.Equal(("192.0.2.99", 41111, session.Password), fakeAdb.PairedCall);
            Assert.Equal(("192.0.2.99", 37777), fakeAdb.ConnectedCall);
            Assert.Contains(WirelessPairingState.WaitingForScan, observedStates);
            Assert.Contains(WirelessPairingState.Pairing, observedStates);
            Assert.Contains(WirelessPairingState.Connecting, observedStates);
            Assert.Contains(WirelessPairingState.Paired, observedStates);
        }

        private static byte[] BuildSyntheticMdnsResponse(string instanceFullName, string targetHost, ushort port, byte[] ipv4)
        {
            using var ms = new MemoryStream();
            // Header: ID=0, Flags=0x8400 (Response), QD=0, AN=2, NS=0, AR=0
            WriteU16(ms, 0);
            WriteU16(ms, 0x8400);
            WriteU16(ms, 0);
            WriteU16(ms, 2);
            WriteU16(ms, 0);
            WriteU16(ms, 0);

            // Answer 1: SRV
            WriteName(ms, instanceFullName);
            WriteU16(ms, 33); // SRV
            WriteU16(ms, 1);  // IN
            WriteU32(ms, 120);
            byte[] targetEncoded = EncodeName(targetHost);
            WriteU16(ms, (ushort)(6 + targetEncoded.Length));
            WriteU16(ms, 0); // priority
            WriteU16(ms, 0); // weight
            WriteU16(ms, port);
            ms.Write(targetEncoded, 0, targetEncoded.Length);

            // Answer 2: A
            WriteName(ms, targetHost);
            WriteU16(ms, 1); // A
            WriteU16(ms, 1); // IN
            WriteU32(ms, 120);
            WriteU16(ms, 4);
            ms.Write(ipv4, 0, 4);

            return ms.ToArray();
        }

        private static byte[] EncodeName(string name)
        {
            using var ms = new MemoryStream();
            WriteName(ms, name);
            return ms.ToArray();
        }

        private static void WriteName(Stream ms, string name)
        {
            foreach (string label in name.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                byte[] b = Encoding.UTF8.GetBytes(label);
                ms.WriteByte((byte)b.Length);
                ms.Write(b, 0, b.Length);
            }
            ms.WriteByte(0);
        }

        private static void WriteU16(Stream ms, ushort v)
        {
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)(v & 0xFF));
        }

        private static void WriteU32(Stream ms, uint v)
        {
            ms.WriteByte((byte)(v >> 24));
            ms.WriteByte((byte)((v >> 16) & 0xFF));
            ms.WriteByte((byte)((v >> 8) & 0xFF));
            ms.WriteByte((byte)(v & 0xFF));
        }

        private sealed class FakeAdbService : IAdbService
        {
            public string AdbPath => "adb";
            public IReadOnlyList<MdnsAdbServiceEndpoint> MdnsEndpoints { get; set; } = Array.Empty<MdnsAdbServiceEndpoint>();
            public (string Host, int Port, string Code)? PairedCall { get; private set; }
            public (string Host, int Port)? ConnectedCall { get; private set; }

            public void ConfigurePaths(string? customAdbPath = null, string? scrcpyPath = null) { }
            public Task<DeviceInfo> DetectDeviceAsync(CancellationToken ct = default) => Task.FromResult(new DeviceInfo());
            public Task<IReadOnlyList<DeviceInfo>> GetConnectedDevicesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<DeviceInfo>>(Array.Empty<DeviceInfo>());
            public Task<DeviceInfo> QueryDeviceInfoAsync(string serial, CancellationToken ct = default) => Task.FromResult(new DeviceInfo());
            public Task<int?> FindDeXDisplayIdAsync(string serial, CancellationToken ct = default) => Task.FromResult<int?>(null);
            public Task<int?> WaitForDeXDisplayIdAsync(string serial, TimeSpan timeout, CancellationToken ct = default) => Task.FromResult<int?>(null);
            public Task<int?> DeployAndActivateDeXAsync(string serial, string? serverJarPath = null, CancellationToken ct = default) => Task.FromResult<int?>(null);
            public Task<bool> SetDisplaySizeAsync(string serial, int displayId, int width, int height, CancellationToken ct = default) => Task.FromResult(true);
            public Task<bool> SetDisplayDpiAsync(string serial, int displayId, int dpi, CancellationToken ct = default) => Task.FromResult(true);
            public Task StopDeXSessionAsync(string? serial = null, CancellationToken ct = default) => Task.CompletedTask;
            public Task<AdbExecutionResult> RunCommandAsync(string arguments, CancellationToken ct = default) =>
                Task.FromResult(new AdbExecutionResult(0, string.Empty, string.Empty));
            public Task<string?> GetDeviceLanIpAddressAsync(string serial, CancellationToken ct = default) => Task.FromResult<string?>("192.0.2.99");
            public Task<string?> GetHardwareSerialAsync(string serial, CancellationToken ct = default) => Task.FromResult<string?>("R5CW123456");
            public Task<int?> GetWirelessTlsPortAsync(string usbSerial, CancellationToken ct = default) => Task.FromResult<int?>(46311);
            public Task DisableTcpIpModeAsync(string? usbSerial, string? tcpIpSerial = null, CancellationToken ct = default) => Task.CompletedTask;
            public Task<WirelessTransitionResult> SwitchToTcpIpAsync(string usbSerial, int port = 5555, CancellationToken ct = default) =>
                Task.FromResult(new WirelessTransitionResult(true, $"192.0.2.99:{port}", "192.0.2.99", port, "OK", IsTls: true));
            public Task<bool> PairWirelessDeviceAsync(string host, int port, string pairingCode, CancellationToken ct = default)
            {
                PairedCall = (host, port, pairingCode);
                return Task.FromResult(true);
            }
            public Task<bool> ConnectWirelessDeviceAsync(string host, int port, CancellationToken ct = default)
            {
                ConnectedCall = (host, port);
                return Task.FromResult(true);
            }
            public Task<bool> DisconnectWirelessDeviceAsync(string targetSerial, CancellationToken ct = default) => Task.FromResult(true);
            public Task<IReadOnlyList<MdnsAdbServiceEndpoint>> GetMdnsServicesAsync(CancellationToken ct = default) =>
                Task.FromResult(MdnsEndpoints);
            public void ResetResilienceState() { }
        }
    }
}
