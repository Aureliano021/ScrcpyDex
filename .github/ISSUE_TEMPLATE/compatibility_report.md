---
name: Compatibility Report
about: Report empirical testing results on a specific Samsung device and PC operating system
title: '[COMPAT]: <Device Model> on <Host OS>'
labels: ['compatibility']
assignees: ''
---

## Device Information
- **Device Model:** <!-- e.g., Samsung Galaxy S24 Ultra (SM-S928B) -->
- **One UI Version:** <!-- e.g., One UI 6.1 -->
- **Android Version:** <!-- e.g., Android 14 -->
- **Carrier / Firmware Region (if known):** <!-- e.g., Unlocked, ZTO -->

## Host PC Information
- **Operating System:** <!-- e.g., Windows 11 (23H2), Linux Mint 22.3, Ubuntu 24.04 LTS -->
- **Kernel Version (Linux):** <!-- e.g., uname -r -->
- **Display Server (Linux):** <!-- X11 or Wayland / XWayland -->
- **scrcpy Version:** <!-- scrcpy --version (must be >= 2.4) -->
- **Connection Transport:** <!-- USB cable (recommended) or Wireless ADB -->

## Compatibility Result
- [ ] **Fully Functional:** DeX activates, window streams at expected FPS, keyboard/mouse input works.
- [ ] **Partially Functional:** DeX activates, but with specific limitations (describe below).
- [ ] **Not Working:** DeX fails to activate or crashes.

## Description & Observations
<!-- Describe what worked, any latency or audio issues, and troubleshooting steps taken. -->

## Logs (if applicable)
```text
<!-- Paste relevant output from 'scrcpydex doctor' or console logs here. Please ensure device serials, MACs, and personal paths are redacted. -->
```
