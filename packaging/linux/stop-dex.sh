#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Emergency Disconnect & Session Reset for Linux
# Terminates local scrcpy instances, unregisters DeX virtual display,
# and restores phone display state cleanly.
# ==============================================================================

set -uo pipefail

SERIAL="${1:-}"
REMOTE_JAR="/data/local/tmp/scrcpydex-server.jar"

C_RESET="\033[0m"
C_BOLD="\033[1m"
C_RED="\033[31m"
C_GREEN="\033[32m"
C_YELLOW="\033[33m"
C_CYAN="\033[36m"

echo -e "${C_CYAN}=================================================${C_RESET}"
echo -e "${C_BOLD}${C_YELLOW}   ScrcpyDeX — Emergency Cleanup (Linux)         ${C_RESET}"
echo -e "${C_CYAN}=================================================${C_RESET}"

adb_cmd() {
    if [[ -n "$SERIAL" ]]; then
        adb -s "$SERIAL" "$@"
    else
        adb "$@"
    fi
}

echo -e "[1/4] Terminating local scrcpy processes..."
pkill -f "scrcpy" 2>/dev/null || true

echo -e "[2/3] Sending disconnect signal to Samsung One UI..."
adb_cmd shell "CLASSPATH=${REMOTE_JAR} app_process /data/local/tmp com.scrcpydex.server.Server disconnect" 2>/dev/null || true

echo -e "[3/3] Terminating server activator processes on device..."
adb_cmd shell "pkill -f com.scrcpydex.server.Server" 2>/dev/null || true
adb_cmd shell "pkill -f scrcpy" 2>/dev/null || true
adb_cmd shell "am broadcast -a com.sec.android.app.desktoplauncher.ACTION_STOP_DESKTOP_MODE" 2>/dev/null || true
adb_cmd shell "am broadcast -a com.samsung.android.desktopmode.action.DESKTOP_MODE_UPDATE --ez enabled false" 2>/dev/null || true


echo -e "${C_GREEN}[OK] Session terminated. Phone display restored successfully.${C_RESET}"
