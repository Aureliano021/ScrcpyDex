# Security Policy

## Supported Versions

Only the latest release receives security updates and fixes.

| Version | Supported          |
| ------- | ------------------ |
| 2.0.x   | :white_check_mark: |
| < 2.0.0 | :x:                |

---

## Threat Model & Architecture Security

ScrcpyDeX orchestrates desktop-mode display activation on supported Samsung Galaxy devices. It operates via the Android Debug Bridge (ADB) in developer mode without requiring device root privileges.

### 1. Process Privilege Boundary (Android Shell Context)
* The on-device companion component (`scrcpydex-server.jar`) runs under Android's `shell` context (UID 2000) via `app_process`.
* In v2.0.0 and later, `scrcpydex-server.jar` does **not** listen on any TCP or network sockets. Its role is solely to invoke Samsung framework APIs via Java reflection to initialize the virtual display and output the resulting display ID to stdout (`DEX_ACTIVATED_DISPLAY_ID=<id>`).
* All media encoding, streaming, video decoding, touch events, and keyboard input injection are handled entirely by upstream official `scrcpy` (version 2.4 or later), which relies on ephemeral abstract UNIX domain sockets rather than exposed network sockets.

### 2. Samsung System Loopback RTSP (Port 7236)
* When activating Samsung DeX over loopback, the Samsung system service (`system_server` via `SemWifiDisplayConfig`) creates a local RTSP loopback server listening on `127.0.0.1:7236`.
* On Android, `127.0.0.1` (localhost) is shared across all processes. A malicious application installed on the device with `INTERNET` permission could hypothetically probe or connect to this system port if timed concurrently during session negotiation.
* This port is created and governed directly by Samsung's proprietary framework services and cannot be hardened or restricted by userland shell tooling. Users should exercise standard discretion regarding applications installed on their devices.

### 3. ADB Network Transport
* When connecting over USB, communication occurs over direct physical cable.
* If connecting via ADB over Wi-Fi (`adb connect`), traffic between the host workstation and the mobile device is subject to local network security. Users should only enable wireless debugging on private, trusted local networks.

---

## Reporting a Vulnerability

If you discover a security vulnerability or security-relevant issue in ScrcpyDeX:

1. **Do not disclose publicly.** Avoid opening public GitHub issues for unpatched vulnerabilities.
2. Report via **GitHub Private Vulnerability Reporting** on the repository page:
   [https://github.com/Aureliano021/ScrcpyDex/security/advisories](https://github.com/Aureliano021/ScrcpyDex/security/advisories)
3. Include:
   - Detailed description of the vulnerability.
   - Exact steps or script to reproduce the behavior.
   - Hardware model, One UI version, Android version, and operating system of the host PC.
   - Any observed impact or suggested remediation.

Reports are acknowledged within 48 hours, and validated fixes are prioritized for release.
