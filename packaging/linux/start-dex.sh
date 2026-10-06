#!/bin/bash
# ScrcpyDeX — Standalone Linux Launcher for Samsung DeX via USB
# Automatically deploys server JAR, activates DeX display over RTSP loopback, and launches scrcpy.

set -e

# 1. Resolve Script and JAR locations
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JAR_CANDIDATES=(
    "$SCRIPT_DIR/scrcpydex-server.jar"
    "$SCRIPT_DIR/../../server/scrcpydex-server.jar"
    "/opt/scrcpydex/scrcpydex-server.jar"
    "/usr/share/scrcpydex/scrcpydex-server.jar"
)

JAR=""
for candidate in "${JAR_CANDIDATES[@]}"; do
    if [ -f "$candidate" ]; then
        JAR="$candidate"
        break
    fi
done

if [ -z "$JAR" ]; then
    echo "[ERROR] scrcpydex-server.jar not found." >&2
    echo "Checked locations: ${JAR_CANDIDATES[*]}" >&2
    exit 1
fi

# 2. Resolve target device serial
if [ -n "$1" ] && [ "${1:0:1}" != "-" ]; then
    SERIAL="$1"
    shift
else
    SERIAL="$(adb get-serialno 2>/dev/null || true)"
    if [ -z "$SERIAL" ] || [ "$SERIAL" = "unknown" ]; then
        echo "[ERROR] No authorized Android device detected via ADB." >&2
        echo "Check USB connection and ensure USB Debugging is enabled." >&2
        exit 1
    fi
fi

echo "[ScrcpyDeX] Target device: $SERIAL"
echo "[ScrcpyDeX] Deploying $JAR..."

# 3. Clean up previous sessions and push JAR
adb -s "$SERIAL" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" >/dev/null 2>&1 || true
adb -s "$SERIAL" push "$JAR" /data/local/tmp/scrcpydex-server.jar >/dev/null

LOG_FILE="/tmp/scrcpydex_activator_${SERIAL}.log"
rm -f "$LOG_FILE"

# 4. Launch DeX Activator daemon in background
adb -s "$SERIAL" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server activate" > "$LOG_FILE" 2>&1 &
ACT_PID=$!

cleanup() {
    echo ""
    echo "[ScrcpyDeX] Teardown: Stopping activator and restoring display..."
    kill $ACT_PID 2>/dev/null || true
    adb -s "$SERIAL" shell "CLASSPATH=/data/local/tmp/scrcpydex-server.jar app_process /data/local/tmp com.scrcpydex.server.Server disconnect" >/dev/null 2>&1 || true
    rm -f "$LOG_FILE"
    echo "[ScrcpyDeX] Disconnected cleanly."
}
trap cleanup EXIT INT TERM

# 5. Await Display ID confirmation
echo "[ScrcpyDeX] Awaiting Samsung DeX display creation..."
DEX_ID=""
for i in $(seq 1 40); do
    if grep -q "DEX_ACTIVATED_DISPLAY_ID=" "$LOG_FILE" 2>/dev/null; then
        DEX_ID=$(grep -o 'DEX_ACTIVATED_DISPLAY_ID=[0-9]*' "$LOG_FILE" | head -n1 | cut -d= -f2)
        break
    fi
    sleep 0.25
done

if [ -z "$DEX_ID" ]; then
    echo "[ERROR] Samsung DeX display was not detected within timeout." >&2
    cat "$LOG_FILE" >&2
    exit 1
fi

echo "[ScrcpyDeX] Samsung DeX ready! Display ID: $DEX_ID"

# 6. Execute scrcpy
SCRCPY_BIN="$(command -v scrcpy || echo "/opt/scrcpy/scrcpy")"
if ! command -v "$SCRCPY_BIN" >/dev/null 2>&1 && [ ! -x "$SCRCPY_BIN" ]; then
    echo "[ERROR] scrcpy binary not found in PATH or /opt/scrcpy/scrcpy." >&2
    exit 1
fi

exec "$SCRCPY_BIN" \
    -s "$SERIAL" \
    --display-id="$DEX_ID" \
    --mouse=uhid \
    --video-codec=h264 \
    --video-bit-rate=8000000 \
    --max-fps=60 \
    --max-size=1920 \
    --window-title="ScrcpyDeX - Samsung DeX" \
    "$@"
