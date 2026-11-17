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
using System.Net;
using System.Text;

namespace ScrcpyDex.Core.Adb
{
    /// <summary>
    /// Minimal RFC 6762 / RFC 1035 mDNS wire-format builder and parser for discovering
    /// Android 11+ wireless debugging services (_adb-tls-pairing._tcp and _adb-tls-connect._tcp).
    /// </summary>
    public static class MdnsPacketParser
    {
        public const string PairingServiceType = "_adb-tls-pairing._tcp.local";
        public const string ConnectServiceType = "_adb-tls-connect._tcp.local";

        private const ushort TypeA = 1;
        private const ushort TypePtr = 12;
        private const ushort TypeSrv = 33;

        /// <summary>
        /// Builds a standard mDNS PTR query packet for one or more service types (e.g. _adb-tls-pairing._tcp.local).
        /// </summary>
        public static byte[] BuildPtrQuery(params string[] serviceNames)
        {
            if (serviceNames == null || serviceNames.Length == 0)
            {
                throw new ArgumentException("At least one service name is required.", nameof(serviceNames));
            }

            using var ms = new MemoryStream();
            // Header: ID=0, Flags=0, QDCOUNT=N, ANCOUNT=0, NSCOUNT=0, ARCOUNT=0
            WriteUInt16BigEndian(ms, 0);
            WriteUInt16BigEndian(ms, 0);
            WriteUInt16BigEndian(ms, (ushort)serviceNames.Length);
            WriteUInt16BigEndian(ms, 0);
            WriteUInt16BigEndian(ms, 0);
            WriteUInt16BigEndian(ms, 0);

            foreach (string service in serviceNames)
            {
                WriteDnsName(ms, service);
                WriteUInt16BigEndian(ms, TypePtr);
                // Class IN (1) with unicast-response bit clear (0x0001)
                WriteUInt16BigEndian(ms, 1);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Parses an mDNS response packet and extracts discovered ADB TLS endpoints by combining PTR, SRV, and A records.
        /// Falls back to <paramref name="senderIp"/> if no A record is included in the packet.
        /// </summary>
        public static IReadOnlyList<MdnsAdbServiceEndpoint> ParseResponse(byte[] packet, string? senderIp = null)
        {
            var results = new List<MdnsAdbServiceEndpoint>();
            if (packet == null || packet.Length < 12)
            {
                return results;
            }

            try
            {
                ushort qdCount = ReadUInt16BigEndian(packet, 4);
                ushort anCount = ReadUInt16BigEndian(packet, 6);
                ushort nsCount = ReadUInt16BigEndian(packet, 8);
                ushort arCount = ReadUInt16BigEndian(packet, 10);

                int offset = 12;

                // Skip Question section
                for (int i = 0; i < qdCount; i++)
                {
                    if (!TryReadDnsName(packet, ref offset, out _)) return results;
                    if (offset + 4 > packet.Length) return results;
                    offset += 4; // QTYPE + QCLASS
                }

                int totalRecords = anCount + nsCount + arCount;
                var srvByInstance = new Dictionary<string, (string Target, int Port)>(StringComparer.OrdinalIgnoreCase);
                var ipByHost = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var ptrInstances = new List<(string ServiceType, string InstanceName)>();

                for (int i = 0; i < totalRecords; i++)
                {
                    if (!TryReadDnsName(packet, ref offset, out string name)) break;
                    if (offset + 10 > packet.Length) break;

                    ushort rrType = ReadUInt16BigEndian(packet, offset);
                    // offset + 2: class (2), offset + 4: ttl (4)
                    ushort rdLength = ReadUInt16BigEndian(packet, offset + 8);
                    offset += 10;

                    if (offset + rdLength > packet.Length) break;
                    int rdataStart = offset;

                    if (rrType == TypePtr)
                    {
                        int ptrOffset = rdataStart;
                        if (TryReadDnsName(packet, ref ptrOffset, out string ptrTarget))
                        {
                            ptrInstances.Add((name, ptrTarget));
                        }
                    }
                    else if (rrType == TypeSrv && rdLength >= 6)
                    {
                        // Priority (2), Weight (2), Port (2), Target (DNS name)
                        ushort port = ReadUInt16BigEndian(packet, rdataStart + 4);
                        int targetOffset = rdataStart + 6;
                        if (TryReadDnsName(packet, ref targetOffset, out string targetHost) && port > 0)
                        {
                            srvByInstance[name] = (targetHost, port);
                        }
                    }
                    else if (rrType == TypeA && rdLength == 4)
                    {
                        var ip = new IPAddress(new ReadOnlySpan<byte>(packet, rdataStart, 4)).ToString();
                        ipByHost[name] = ip;
                    }

                    offset = rdataStart + rdLength;
                }

                // Resolve SRV records belonging to _adb-tls-pairing._tcp or _adb-tls-connect._tcp
                foreach (var kvp in srvByInstance)
                {
                    string fullInstance = kvp.Key;
                    if (!TryClassifyAdbService(fullInstance, out string instanceName, out string serviceType))
                    {
                        continue;
                    }

                    string? ip = ipByHost.TryGetValue(kvp.Value.Target, out string? resolvedIp)
                        ? resolvedIp
                        : senderIp;

                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        results.Add(new MdnsAdbServiceEndpoint(instanceName, serviceType, ip!, kvp.Value.Port));
                    }
                }
            }
            catch
            {
                // Ignore malformed packets gracefully
            }

            return results;
        }

        private static bool TryClassifyAdbService(string fullName, out string instanceName, out string serviceType)
        {
            instanceName = string.Empty;
            serviceType = string.Empty;

            const string pairingSuffix = "._adb-tls-pairing._tcp.local";
            const string connectSuffix = "._adb-tls-connect._tcp.local";

            string trimmed = fullName.TrimEnd('.');
            if (trimmed.EndsWith(pairingSuffix, StringComparison.OrdinalIgnoreCase))
            {
                instanceName = trimmed.Substring(0, trimmed.Length - pairingSuffix.Length);
                serviceType = "_adb-tls-pairing._tcp";
                return !string.IsNullOrWhiteSpace(instanceName);
            }

            if (trimmed.EndsWith(connectSuffix, StringComparison.OrdinalIgnoreCase))
            {
                instanceName = trimmed.Substring(0, trimmed.Length - connectSuffix.Length);
                serviceType = "_adb-tls-connect._tcp";
                return !string.IsNullOrWhiteSpace(instanceName);
            }

            return false;
        }

        private static void WriteDnsName(Stream stream, string name)
        {
            string trimmed = name.Trim('.');
            foreach (string label in trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                byte[] labelBytes = Encoding.UTF8.GetBytes(label);
                if (labelBytes.Length > 63)
                {
                    throw new ArgumentException($"DNS label exceeds 63 bytes: {label}", nameof(name));
                }
                stream.WriteByte((byte)labelBytes.Length);
                stream.Write(labelBytes, 0, labelBytes.Length);
            }
            stream.WriteByte(0);
        }

        private static bool TryReadDnsName(byte[] packet, ref int offset, out string name)
        {
            var sb = new StringBuilder();
            int current = offset;
            bool jumped = false;
            int endOffset = offset;
            int jumps = 0;

            while (current < packet.Length)
            {
                byte len = packet[current];
                if (len == 0)
                {
                    if (!jumped) endOffset = current + 1;
                    offset = endOffset;
                    name = sb.ToString();
                    return true;
                }

                if ((len & 0xC0) == 0xC0)
                {
                    if (current + 1 >= packet.Length)
                    {
                        name = string.Empty;
                        return false;
                    }

                    int pointer = ((len & 0x3F) << 8) | packet[current + 1];
                    if (pointer >= packet.Length || ++jumps > 16)
                    {
                        name = string.Empty;
                        return false;
                    }

                    if (!jumped)
                    {
                        endOffset = current + 2;
                        jumped = true;
                    }
                    current = pointer;
                    continue;
                }

                current++;
                if (current + len > packet.Length)
                {
                    name = string.Empty;
                    return false;
                }

                if (sb.Length > 0) sb.Append('.');
                sb.Append(Encoding.UTF8.GetString(packet, current, len));
                current += len;
            }

            name = string.Empty;
            return false;
        }

        private static ushort ReadUInt16BigEndian(byte[] buffer, int offset) =>
            (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

        private static void WriteUInt16BigEndian(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value & 0xFF));
        }
    }
}
