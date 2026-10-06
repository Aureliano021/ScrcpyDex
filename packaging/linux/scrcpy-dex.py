#!/usr/bin/env python3
"""
ScrcpyDeX - Python CLI Launcher for Linux
Connects Samsung Galaxy devices to Samsung DeX over USB using scrcpy.
"""

import argparse
import os
import re
import signal
import subprocess
import sys
import time
from typing import Optional, List, Tuple

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))

JAR_CANDIDATES = [
    os.path.join(SCRIPT_DIR, "scrcpydex-server.jar"),
    os.path.join(REPO_ROOT, "server", "scrcpydex-server.jar"),
    "/opt/scrcpydex/scrcpydex-server.jar",
    "/usr/share/scrcpydex/scrcpydex-server.jar",
]

REMOTE_JAR = "/data/local/tmp/scrcpydex-server.jar"

# ANSI Colors
C_RESET = "\033[0m"
C_BOLD = "\033[1m"
C_RED = "\033[31m"
C_GREEN = "\033[32m"
C_YELLOW = "\033[33m"
C_CYAN = "\033[36m"
C_GRAY = "\033[90m"

def log_info(msg: str):
    print(f"{C_CYAN}[INFO]{C_RESET} {msg}")

def log_ok(msg: str):
    print(f"{C_GREEN}[OK]{C_RESET} {msg}")

def log_warn(msg: str):
    print(f"{C_YELLOW}[WARN]{C_RESET} {msg}")

def log_err(msg: str):
    print(f"{C_RED}[ERROR]{C_RESET} {msg}", file=sys.stderr)

class DeXManager:
    def __init__(self, serial: Optional[str] = None, jar_path: Optional[str] = None):
        self.serial = serial
        self.jar_path = jar_path or self._find_jar()
        self.activator_proc: Optional[subprocess.Popen] = None
        self.dex_display_id: Optional[int] = None

    def _find_jar(self) -> str:
        for candidate in JAR_CANDIDATES:
            if os.path.isfile(candidate):
                return candidate
        return JAR_CANDIDATES[0]

    def run_adb(self, *args, capture: bool = True, check: bool = False) -> subprocess.CompletedProcess:
        cmd = ["adb"]
        if self.serial:
            cmd.extend(["-s", self.serial])
        cmd.extend(args)
        return subprocess.run(
            cmd,
            stdout=subprocess.PIPE if capture else None,
            stderr=subprocess.PIPE if capture else None,
            text=True,
            check=check
        )

    def detect_devices(self) -> List[Tuple[str, str]]:
        res = subprocess.run(["adb", "devices"], stdout=subprocess.PIPE, text=True)
        devices = []
        for line in res.stdout.strip().splitlines()[1:]:
            parts = line.strip().split()
            if len(parts) >= 2:
                devices.append((parts[0], parts[1]))
        return devices

    def query_device_info(self) -> dict:
        mfg = self.run_adb("shell", "getprop", "ro.product.manufacturer").stdout.strip()
        model = self.run_adb("shell", "getprop", "ro.product.model").stdout.strip()
        release = self.run_adb("shell", "getprop", "ro.build.version.release").stdout.strip()
        oneui = self.run_adb("shell", "getprop", "ro.build.version.oneui").stdout.strip()
        return {
            "manufacturer": mfg,
            "model": model,
            "android": release,
            "oneui": oneui
        }

    def cleanup(self):
        log_info("Disconnecting DeX session on device...")
        if self.activator_proc and self.activator_proc.poll() is None:
            try:
                self.activator_proc.terminate()
                self.activator_proc.wait(timeout=1.0)
            except Exception:
                self.activator_proc.kill()

        self.run_adb("shell", f"CLASSPATH={REMOTE_JAR} app_process /data/local/tmp com.scrcpydex.server.Server disconnect")
        self.run_adb("shell", "pkill -f com.scrcpydex.server.Server")
        log_ok("Session finished. Device display restored.")

    def find_display_id(self) -> Optional[int]:
        res = self.run_adb("shell", "dumpsys", "display")
        dump = res.stdout

        m1 = re.search(r'DisplayInfo\{"(?:ScrcpyDeX|Samsung DeX)"[^\}]*?displayId\s+(\d+)', dump, re.IGNORECASE)
        if m1:
            return int(m1.group(1))

        m2 = re.search(r'Display\s+(\d+)[\s\S]*?(?:mPrimaryDisplayDevice=ScrcpyDeX|name="ScrcpyDeX"|name="Samsung DeX")', dump, re.IGNORECASE)
        if m2:
            return int(m2.group(1))

        m3 = re.search(r'DisplayDeviceInfo\{"(?:ScrcpyDeX|Samsung DeX)"[^\}]*?displayId\s*[:=]?\s*(\d+)', dump, re.IGNORECASE)
        if m3:
            return int(m3.group(1))

        return None

    def start(self, args: argparse.Namespace):
        # 1. Check tools
        for tool in ["adb", "scrcpy"]:
            if subprocess.run(["which", tool], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode != 0:
                log_err(f"Tool '{tool}' not found. Please install it or ensure it is in PATH.")
                sys.exit(1)

        # 2. Select device
        devices = self.detect_devices()
        if not devices:
            log_err("No Android device detected via USB/ADB.")
            log_info("Connect your Samsung Galaxy and ensure USB Debugging is toggled ON.")
            sys.exit(1)

        if not self.serial:
            if len(devices) == 1:
                self.serial, state = devices[0]
            else:
                print(f"{C_BOLD}Multiple devices detected:{C_RESET}")
                for i, (ser, st) in enumerate(devices):
                    print(f"  [{i+1}] {ser} ({st})")
                choice = input(f"Select device [1-{len(devices)}]: ").strip()
                try:
                    idx = int(choice) - 1
                    self.serial, state = devices[idx]
                except (ValueError, IndexError):
                    log_err("Invalid selection.")
                    sys.exit(1)

        info = self.query_device_info()
        print(f"\n{C_BOLD}Target Device:{C_RESET} {info['manufacturer'].upper()} {info['model']} (Android {info['android']}, One UI: {info['oneui']})")

        # 3. Deploy JAR
        if not os.path.isfile(self.jar_path):
            log_err(f"Server JAR not found at '{self.jar_path}'.")
            sys.exit(1)

        log_info(f"Deploying {os.path.basename(self.jar_path)} to device...")
        self.run_adb("push", self.jar_path, REMOTE_JAR, check=True)

        # 4. Clean prior session
        log_info("Clearing stale sessions...")
        self.run_adb("shell", f"CLASSPATH={REMOTE_JAR} app_process /data/local/tmp com.scrcpydex.server.Server disconnect")
        time.sleep(0.5)

        # 5. Start activator daemon
        log_info("Activating Samsung DeX loopback display...")
        cmd = [
            "adb", "-s", self.serial, "shell",
            f"CLASSPATH={REMOTE_JAR} app_process /data/local/tmp com.scrcpydex.server.Server activate"
        ]
        self.activator_proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1
        )

        def signal_handler(sig, frame):
            print("\n")
            self.cleanup()
            sys.exit(0)

        signal.signal(signal.SIGINT, signal_handler)
        signal.signal(signal.SIGTERM, signal_handler)

        # 6. Await display ID
        log_info("Awaiting Samsung DeX display creation...")
        timeout = 15.0
        start_time = time.time()
        while time.time() - start_time < timeout:
            disp_id = self.find_display_id()
            if disp_id is not None:
                self.dex_display_id = disp_id
                break
            time.sleep(0.5)

        if self.dex_display_id is None:
            log_err("Timeout: Samsung DeX display was not detected within 15 seconds.")
            self.cleanup()
            sys.exit(1)

        log_ok(f"Samsung DeX Display initialized with ID={self.dex_display_id}!")

        # 7. Build and run scrcpy
        scrcpy_cmd = [
            "scrcpy",
            "-s", self.serial,
            f"--display-id={self.dex_display_id}",
            f"--mouse={args.mouse}",
            f"--video-codec={args.codec}",
            f"--video-bit-rate={args.bitrate}",
            f"--max-fps={args.fps}",
            f"--max-size={args.max_size}",
            "--window-title=ScrcpyDeX - Samsung DeX"
        ]

        if args.no_audio:
            scrcpy_cmd.append("--no-audio")
        else:
            scrcpy_cmd.append(f"--audio-buffer={args.audio_buffer}")

        if args.fullscreen:
            scrcpy_cmd.append("--fullscreen")

        log_info("Starting scrcpy streaming...")
        try:
            subprocess.run(scrcpy_cmd)
        finally:
            self.cleanup()

def main():
    parser = argparse.ArgumentParser(description="ScrcpyDeX CLI for Linux")
    parser.add_argument("-s", "--serial", help="Device serial number")
    parser.add_argument("--fps", type=int, default=60, help="Max streaming framerate (default: 60)")
    parser.add_argument("--bitrate", default="8M", help="Video bitrate (default: 8M)")
    parser.add_argument("--max-size", type=int, default=1920, help="Maximum video dimension (default: 1920)")
    parser.add_argument("--codec", default="h264", choices=["h264", "h265", "av1"], help="Video codec (default: h264)")
    parser.add_argument("--mouse", default="uhid", choices=["uhid", "sdk", "aoa"], help="Mouse driver mode (default: uhid)")
    parser.add_argument("--no-audio", action="store_true", help="Disable audio forwarding")
    parser.add_argument("--audio-buffer", type=int, default=50, help="Audio buffer size in ms (default: 50)")
    parser.add_argument("--fullscreen", action="store_true", help="Start scrcpy in fullscreen")
    parser.add_argument("--jar", help="Path to scrcpydex-server.jar")

    args = parser.parse_args()
    manager = DeXManager(serial=args.serial, jar_path=args.jar)
    manager.start(args)

if __name__ == "__main__":
    main()
