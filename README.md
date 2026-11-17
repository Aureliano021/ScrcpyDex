<p align="center">
  <img src="assets/app_transparent.png" width="120" height="120" alt="ScrcpyDeX — Samsung DeX on Windows and Linux via USB or Wi-Fi" />
</p>

<h1 align="center">ScrcpyDeX</h1>

<p align="center">
  <strong>Use Samsung DeX on Windows and Linux over USB or Wi-Fi</strong><br>
  Open-source desktop client powered by <code>scrcpy</code> and ADB — works on Galaxy devices (including <strong>One UI 7+</strong> where the official <em>Samsung DeX for PC</em> app was discontinued) without root.
</p>

---

> **Disclaimer:** ScrcpyDeX is an independent, community-developed open-source project and is **not affiliated with, endorsed by, or sponsored by Samsung Electronics Co., Ltd.** "Samsung", "Samsung DeX", and "Galaxy" are trademarks of Samsung Electronics Co., Ltd.

## Author's Note

> *"Hey! Aureliano here. I want to make it crystal clear that my direct participation in this codebase was roughly, let's say, 10%. I simply brought the original idea to the table and actively guided the autonomous AI coding agents step-by-step. There might be grotesque bugs, or there might be none at all—what truly matters to me is that this project completely solved my real-world need!"*

---

## Preview

<table>
  <tr>
    <td align="center"><img src="assets/preview_main.jpg" alt="Samsung DeX on Windows and Linux — ScrcpyDeX Main Window" width="420" /><br><sub><b>Main Device &amp; Telemetry View</b></sub></td>
    <td align="center"><img src="assets/preview_settings.png" alt="ScrcpyDeX Display and Encoder Settings" width="420" /><br><sub><b>Settings Window</b></sub></td>
  </tr>
</table>

---

## Features

* **USB & Wireless Connection (QR Code / One-Click Wi-Fi):** Connect over a standard **USB cable**, pair wirelessly by scanning the **in-app QR Code** (Android 11+ Wireless Debugging with TLS), or switch an active USB connection to **Wi-Fi** in one click.
* **Alternative to Discontinued Samsung DeX for PC:** Launches the native Samsung DeX virtual desktop over ADB and streams it with low latency via `scrcpy`.
* **In-App Settings:** Configure display resolution (including custom dimensions), DPI, frame rate (FPS), bitrate, video codec (`H.264` / `H.265`), render driver, audio/input options, and custom `scrcpy`/`adb` paths.
* **Multi-Language Support:** Localized across 6 languages with instant switching (`English`, `Português (BR)`, `Español`, `Deutsch`, `Français`, `简体中文`).

---

## Testing Scope & Device Behavior

* **Testing:** Tests were performed on **Windows** and **Ubuntu** using a **Samsung Galaxy S23**. Other Galaxy DeX devices and non-DeX Android models have not been tested.
* **Non-DeX Android Devices:** Phones and tablets without Samsung DeX automatically fall back to standard **Screen Mirroring** (main display).

---

## Prerequisites & System Requirements

1. **Operating System:** **Windows 10** (1607+) / **Windows 11** (64-bit) or **Linux** (`x64`, tested on Ubuntu). Windows 7 and 8.1 are not supported (`.NET 8` requirement).
2. **Official `scrcpy` (>= 2.4) & `adb`:** Required for `--display-id` and `--mouse=uhid` support. On Windows, the official `scrcpy` package **already includes `adb.exe`** in the same directory:
   ```powershell
   # Windows (WinGet)
   winget install Genymobile.scrcpy

   # Windows (Scoop)
   scoop install scrcpy
   ```
   *(Alternatively, extract the official archive from [Genymobile/scrcpy Releases](https://github.com/Genymobile/scrcpy/releases) and add it to `PATH`, or select the executables manually in Settings.)*
3. **Developer Options Enabled on Phone:** Enable **USB debugging** (for wired connection or USB-to-Wi-Fi switch) or **Wireless debugging** (to pair directly via QR Code).

---

## How to Use Samsung DeX on Windows & Linux (Quick Start)

1. Download and extract the latest release archive for Windows or Linux from [GitHub Releases](https://github.com/Aureliano021/ScrcpyDex/releases).
2. Ensure **`adb`** and **`scrcpy` (>= 2.4)** are installed.
3. Launch **`ScrcpyDex.App.exe`** (Windows) or **`./ScrcpyDex.App`** (Linux).
4. **Connect your phone:**
   * **Via USB:** Plug in the USB cable with **USB debugging** enabled and accept the authorization prompt on the phone *(optional: click the Wi-Fi button next to Rescan to switch to wireless)*.
   * **Via Wi-Fi (QR Code):** On your phone, open *Developer options → Wireless debugging → Pair device with QR code* and scan the QR Code displayed on the left panel.
5. Click **Start DeX Session** (or **Start Screen Mirroring** on non-DeX devices).
6. To stop the session and restore the phone display cleanly, click **Stop Session** or close the `scrcpy` window (`Alt + F4`).
