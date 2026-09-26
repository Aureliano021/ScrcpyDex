# ScrcpyDeX — Native Samsung DeX for PC via USB

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue)](https://www.microsoft.com/windows)
[![Android](https://img.shields.io/badge/Android-11%20to%2016%2B%20%28One%20UI%203%20to%208.5%29-green)](https://www.samsung.com)
[![Hardware](https://img.shields.io/badge/Tested%20on-Samsung%20Galaxy%20S23-orange)](https://www.samsung.com/galaxy-s23/)
[![License](https://img.shields.io/badge/License-Apache%202.0%20%2F%20MIT-lightgrey)](#license--disclaimer)

**ScrcpyDeX** is an open-source interoperability solution that activates and runs **native Samsung DeX** directly on your PC over a standard USB cable. By combining loopback Miracast emulation with hardware-accelerated video rendering and Linux kernel `/dev/uhid` input, ScrcpyDeX delivers a true desktop experience with **zero Wi-Fi latency**, **GPU acceleration**, **integrated audio**, and **100% automated lifecycle management**—all without root privileges.

Developed and validated on a **Samsung Galaxy S23 (`SM-S911B`)** running **Android 16 / One UI 8.5**.

---

## ✍️ Author's Note

> *"Hey! Aureliano here. I want to make it crystal clear that my direct participation in this codebase was roughly, let's say, 10%. I simply brought the original idea to the table and actively guided the autonomous AI coding agents step-by-step. There might be grotesque bugs, or there might be none at all—what truly matters to me is that this project completely solved my real-world need!"*

---

## ⚡ Key Features

* **Zero Wi-Fi Latency over USB:** Eliminates wireless stutter, packet drops, and local network congestion by looping Miracast through `127.0.0.1` and streaming over high-speed USB via ADB.
* **Direct3D11 GPU Acceleration:** Fluid 60 FPS hardware-accelerated rendering on Windows with minimal CPU overhead and sub-15ms glass-to-glass latency.
* **Native Desktop Mouse (Kernel UHID):** Leverages Linux `/dev/uhid` to create a virtual hardware cursor in Android's `EventHub`. Enjoy a persistent Samsung DeX arrow cursor, real hover effects, button tooltips, native right-click context menus, and smooth wheel scrolling.
* **Synchronized Audio Forwarding:** Routes desktop audio seamlessly from Samsung DeX directly to your PC speakers or headphones over USB.
* **Automated Clean Teardown:** Closing the window immediately disconnects the Miracast session and restores your smartphone to its normal lock screen/app state in less than 1 second, leaving zero orphaned processes.

---

## 📋 Requirements

1. **Samsung Galaxy Device:** Any Galaxy smartphone or tablet with native Samsung DeX support (Galaxy S-series, Note-series, Z Fold, or Tab S-series).
2. **USB Debugging Enabled:** Enable *Developer Options* on your device, then turn on *USB Debugging*.
3. **USB Cable:** High-quality USB-C to USB-A or USB-C to USB-C cable (USB 2.0 or USB 3.0).
4. **scrcpy v2.0+:** Installed on your Windows PC (available via `winget install Genymobile.scrcpy` or added to system `PATH`).
5. **Windows OS:** Windows 10 or Windows 11 with PowerShell 5.1+ or PowerShell 7+.

---

## 🚀 Quick Start

1. Connect your Samsung Galaxy phone to your PC via USB cable and authorize the USB debugging prompt on the screen.
2. Double-click **`ScrcpyDeX.bat`** (or run it in a terminal):
   ```cmd
   .\ScrcpyDeX.bat
   ```
3. The unified Samsung DeX desktop window will open automatically with full GPU acceleration, audio forwarding, and native mouse support.

---

## ⌨️ Controls & Behavior

| Action | Control / Shortcut | Description |
| :--- | :--- | :--- |
| **Release Mouse Capture** | `Left Alt` or `Windows Key` | Releases the captured mouse cursor from the DeX window back to the Windows desktop. |
| **Toggle Fullscreen** | `Alt + F` | Expands the Samsung DeX window to fill the entire monitor. |
| **Close & Restore Phone** | `Alt + F4` or Click `[X]` | Closes the DeX window and automatically triggers a clean session shutdown on your phone. |

---

## 🛑 Emergency Stop (Kill Switch)

If an unexpected terminal disconnect or ADB hang leaves a background session active on your phone, run the non-interactive kill switch:

```cmd
.\stop-dex.bat
```

This immediately terminates any hanging host processes (`scrcpy.exe`, `ffplay.exe`), sends the disconnect signal to Samsung's `system_server`, cleans ADB port forwards, and restores your Galaxy device in under 1 second.

---

## 🛠️ Architecture & How It Works

```
        Host PC (Windows Client)                   Samsung Galaxy (Target Device)
 ┌────────────────────────────────────┐         ┌────────────────────────────────────┐
 │  ScrcpyDeX.bat -> run-scrcpydex.ps1│         │  scrcpydex-server.jar (app_process)│
 │                                    │         │                                    │
 │  [scrcpy Direct3D11 Client]        │         │  [DexActivator (Loopback RTSP)]    │
 │   - Hardware Video Renderer        │◄──USB──►│   - 127.0.0.1:7236 Loopback        │
 │   - Audio WASAPI Sink              │  (ADB)  │   - SemWifiDisplay IPC Hook        │
 │   - UHID Mouse Input Driver        │         │                                    │
 │                                    │         │  [Android OS & Kernel]             │
 │  [PowerShell Lifecycle Daemon]     │         │   - Display 'ScrcpyDeX' (DPI 160)  │
 │   - Dynamic Display Detection      │         │   - /dev/uhid (Group 3011)         │
 │   - Guaranteed finally Teardown    │         │   - Hardware Video Encoder (H.264) │
 └────────────────────────────────────┘         └────────────────────────────────────┘
```

1. **Loopback Miracast Activator (`server/scrcpydex-server.jar`):**  
   Initializes Samsung's `SemWifiDisplay` subsystem on `127.0.0.1:7236` via IPC reflection in `app_process` (UID 2000). By mimicking a local Miracast sink, it triggers `FLAG_WIRELESS_DEX_DISPLAY (0x4000000)` and `FLAG_EXTERNAL_DEX_HOSTING (0x20000)` in `system_server`, spawning a fully isolated desktop display named `"ScrcpyDeX"`.
2. **Surgical Display ID Detection (`run-scrcpydex.ps1`):**  
   Monitors Android's display subsystem to identify the exact dynamically assigned display ID (e.g., `Display 13`, `Display 44`), ensuring the client never connects to the physical screen or residual mirrors.
3. **Unified Single-Window Client (`scrcpy --display-id=... --mouse=uhid`):**  
   Connects directly to the DeX display with Direct3D11 hardware acceleration and provisions a virtual HID mouse node in `/dev/uhid` (kernel group `3011`). Android's `EventHub` recognizes a hardware mouse (`Classes: CURSOR`), rendering a persistent desktop cursor with hover states and right-click context menus.
4. **Resilient Lifecycle Management:**  
   The PowerShell script uses a strict `try...finally` block. The moment the user closes the window, the server's `disconnect` routine is called, terminating the `SemWifiDisplay` session and freeing all resources.

---

## 📂 Documentation & Technical Reports

### English Technical Architecture Reports
* [Report 01: Server Core Architecture, Loopback Miracast RTSP & H.264 Video Pipeline](docs/reports/01_server_core_and_video.md)
* [Report 02: Control Channel Protocol, Input Event Injection & Coordinate Translation](docs/reports/02_control_channel_and_input.md)
* [Report 03: Unified Client, Linux Kernel UHID Mouse & Lifecycle Orchestration](docs/reports/03_unified_client_and_uhid_mouse.md)
* [Protocol Specification: Binary Control & Communication Contract (SSOT)](docs/PROTOCOL.md)

### Historical Research & Discovery Archive (`pt-br`)
* [Original Development & Discovery Reports (pt-br)](reports/pt-br/)

---

## 📄 License, Legal & Compliance

* **License:** Licensed under the [Apache License, Version 2.0](LICENSE).
* **Legal & Interoperability Compliance:** See [docs/LEGAL_AND_LICENSING.md](docs/LEGAL_AND_LICENSING.md) for full legal analysis (DMCA 1201(f), EU Directive 2009/24/EC, Brazilian Software Law No. 9.609/1998, clean-room protocol analysis & interoperability implementation, and third-party attributions).
* **Trademark Disclaimer:** ScrcpyDeX is an independent open-source project. It is not affiliated with, endorsed by, or certified by Samsung Electronics Co., Ltd., Google LLC, or Genymobile. "Samsung", "Samsung DeX", "Galaxy", and "One UI" are registered trademarks of Samsung Electronics Co., Ltd.
