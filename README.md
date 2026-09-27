# ScrcpyDeX — Open-Source Client for Samsung DeX on PC via USB

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue)](https://www.microsoft.com/windows)
[![Android](https://img.shields.io/badge/Android-11%20to%2016%2B%20%28One%20UI%203%20to%208.5%29-green)](https://www.samsung.com)
[![Hardware](https://img.shields.io/badge/Tested%20on-Samsung%20Galaxy%20S23-orange)](https://www.samsung.com/galaxy-s23/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

**ScrcpyDeX** is an open-source tool that activates and runs **Samsung DeX** directly on your PC over a standard USB cable. It works by setting up a local loopback display connection and streaming it with `scrcpy`, giving you a desktop experience over USB with mouse and keyboard input and audio forwarding—without requiring root.

Developed and validated on a **Samsung Galaxy S23 (`SM-S911B`)** running **Android 16 / One UI 8.5**.

---

## ✍️ Author's Note

> *"Hey! Aureliano here. I want to make it crystal clear that my direct participation in this codebase was roughly, let's say, 10%. I simply brought the original idea to the table and actively guided the autonomous AI coding agents step-by-step. There might be grotesque bugs, or there might be none at all—what truly matters to me is that this project completely solved my real-world need!"*

---

## ⚡ Key Features

* **No Wi-Fi needed (runs over USB):** Runs directly through a USB cable via ADB, avoiding wireless interference, latency spikes, and local network restrictions.
* **Low latency rendering:** Hardware-accelerated 60 FPS video stream with low latency and low CPU usage via `scrcpy`'s Direct3D11 decoder.
* **Normal mouse behavior (UHID):** Uses Linux `/dev/uhid` so Android sees a real mouse cursor rather than a touch pointer, supporting hover effects, right-click menus, and scroll wheel.
* **Audio forwarding:** Plays audio from DeX through your PC speakers or headphones over USB.
* **Automatic cleanup:** Closing the window ends the session and restores your phone screen automatically, cleaning up background processes.

---

## 📋 Requirements

1. **Samsung Galaxy Device:** Any Galaxy smartphone or tablet with native Samsung DeX support (Galaxy S-series, Note-series, Z Fold, or Tab S-series).
2. **USB Debugging Enabled:** Enable *Developer Options* on your device, then turn on *USB Debugging*.
3. **USB Cable:** High-quality USB-C to USB-A or USB-C to USB-C cable (USB 2.0 or USB 3.0).
4. **scrcpy v2.0+:** Installed on your Windows PC (available via `winget install Genymobile.scrcpy` or added to system `PATH`).
5. **Windows OS:** Windows 10 or Windows 11 with PowerShell 5.1+ or PowerShell 7+.

---

## 🚀 Quick Start

### 1. GUI App:
Launch **`ScrcpyDeX.exe`** (or **`ScrcpyDeX.vbs`**). Choose your settings and click **Start DeX Session**.

### 2. Direct CLI Launch:
Run **`ScrcpyDeX.bat`** directly from Command Prompt or PowerShell:
```cmd
.\ScrcpyDeX.bat
```

---

## ⌨️ Controls & Behavior

| Action | Control / Shortcut | Description |
| :--- | :--- | :--- |
| **Release Mouse Capture** | `Left Alt` or `Windows Key` | Releases the captured mouse cursor from the DeX window back to the Windows desktop. |
| **Toggle Fullscreen** | `Alt + F` | Expands the Samsung DeX window to fill the entire monitor. |
| **Close & Restore Phone** | `Alt + F4` or Click `[X]` | Closes the DeX window and automatically shuts down the session on your phone. |

---

## 🛑 Emergency Stop (Kill Switch)

If the connection drops unexpectedly or a session stays open on your phone, run:

```cmd
.\stop-dex.bat
```

This stops lingering host processes (`scrcpy.exe`, `ffplay.exe`), sends a disconnect signal to Samsung's `system_server`, resets ADB port forwards, and restores your phone display.

---

## 🛠️ Architecture & How It Works

```
        Host PC (Windows Client)                   Samsung Galaxy (Target Device)
 ┌────────────────────────────────────┐         ┌────────────────────────────────────┐
 │  ScrcpyDeX.exe / run-scrcpydex.ps1 │         │  scrcpydex-server.jar (app_process)│
 │                                    │         │                                    │
 │  [scrcpy Direct3D11 Client]        │         │  [DexActivator (Loopback RTSP)]    │
 │   - Hardware Video Renderer        │◄──USB──►│   - 127.0.0.1:7236 Loopback        │
 │   - Audio WASAPI Sink              │  (ADB)  │   - SemWifiDisplay IPC Hook        │
 │   - UHID Mouse Input Driver        │         │                                    │
 │                                    │         │  [Android OS & Kernel]             │
 │  [Client & Lifecycle Control]      │         │   - Display 'ScrcpyDeX' (DPI 160)  │
 │   - Display Detection              │         │   - /dev/uhid (Group 3011)         │
 │   - Session Cleanup                │         │   - Hardware Video Encoder (H.264) │
 └────────────────────────────────────┘         └────────────────────────────────────┘
```

1. **Loopback Display Activator (`server/scrcpydex-server.jar`):**  
   Starts Samsung's wireless display service bound to `127.0.0.1:7236` via `app_process` (UID 2000). The phone treats this local connection as a display target and creates an isolated DeX desktop display named `"ScrcpyDeX"`.
2. **Display ID Detection:**  
   Checks `dumpsys display` to find the ID assigned to the new DeX display so `scrcpy` connects to the desktop instead of the phone's primary screen.
3. **Display & Input (`scrcpy --display-id=... --mouse=uhid`):**  
   `scrcpy` captures the DeX display and handles video decoding and audio. It uses `/dev/uhid` to forward mouse input as a standard mouse device, supporting DeX desktop cursor, hover states, and right-click menus.
4. **Session Cleanup:**  
   When the window is closed, a cleanup routine sends a disconnect signal to end the session, restore the phone display, and close background processes.

---

## 🔒 Security, Trust Model & Operational Guidelines

* **ADB Privilege Context (`UID 2000`):** The ScrcpyDeX server component runs under Android's standard development `shell` user. It operates **without root** privileges and relies only on authorized developer capabilities.
* **Local Loopback Transport:** All video and control communication binds strictly to `127.0.0.1`.
* **Direct USB Cable Recommended:** Use a physical USB connection with `adb forward`. **Using Wi-Fi ADB across untrusted or public networks is strongly discouraged**, as unauthenticated Wi-Fi debugging could expose input injection interfaces to local network adversaries.
* **Authorized Devices Only:** Use ScrcpyDeX only on personal devices or hardware you are legitimately authorized to manage.

---

## 📂 Documentation & Technical Reports

### English Technical Architecture Reports
* [Report 01: Server Core Architecture, Loopback Miracast RTSP & H.264 Video Pipeline](docs/reports/01_server_core_and_video.md)
* [Report 02: Control Channel Protocol, Input Event Injection & Coordinate Translation](docs/reports/02_control_channel_and_input.md)
* [Report 03: Unified Client, Linux Kernel UHID Mouse & Lifecycle Orchestration](docs/reports/03_unified_client_and_uhid_mouse.md)
* [Report 04: Desktop Configuration UI, Settings Persistence & Live Telemetry](docs/reports/04_configuration_ui_and_settings.md)
* [Report 05: Client Architecture, Resilient Process Engine & Systems Blueprint](docs/reports/05_client_architecture_and_design_patterns.md)
* [Protocol Specification: Binary Control & Communication Contract (SSOT)](docs/PROTOCOL.md)


---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our clean-room engineering standards, Developer Certificate of Origin (DCO 1.1) signing, and pull request procedures.

---

## 📄 License, Legal & Compliance

* **License:** Licensed under the [Apache License, Version 2.0](LICENSE).
* **Third-Party Notices:** See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for licenses and copyright attributions of external tools (`scrcpy`, AOSP tools, FFmpeg).
* **Legal & Interoperability Compliance:** See [docs/LEGAL_AND_LICENSING.md](docs/LEGAL_AND_LICENSING.md) for architectural analysis and legal compliance details (DMCA 1201(f), EU Directive 2009/24/EC, Brazilian Software Law No. 9.609/1998, verifiable clean-room audit, and cryptographic hashes).
* **Legal Disclaimer & Terms of Interoperability:** See [DISCLAIMER.md](DISCLAIMER.md) for comprehensive bilingual warranty disclaimers, non-affiliation notice, Knox warranty preservation (0x0), non-root shell UID 2000 architecture, and statutory reverse engineering protections.
* **Trademark Disclaimer:** ScrcpyDeX is an independent open-source project. It is not affiliated with, endorsed by, or certified by Samsung Electronics Co., Ltd., Google LLC, or Genymobile. "Samsung", "Samsung DeX", "Galaxy", and "One UI" are registered trademarks of Samsung Electronics Co., Ltd.
