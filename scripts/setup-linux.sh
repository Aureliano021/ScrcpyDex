#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Automated One-Step Linux Environment Setup & Optimizer
# Configures all required dependencies, permissions, and drivers in one command.
# Supported Distros: Ubuntu 22.04/24.04+, Linux Mint 21/22+, Debian 12+
# ==============================================================================

set -euo pipefail

BOLD='\033[1m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BOLD}${BLUE}====================================================${NC}"
echo -e "${BOLD}${BLUE}   ScrcpyDeX — Automated Linux Setup & Optimizer    ${NC}"
echo -e "${BOLD}${BLUE}====================================================${NC}"
echo ""

# 1. Determine Privileges & Target User
if [ "$EUID" -ne 0 ]; then
    SUDO="sudo"
    TARGET_USER="$USER"
else
    SUDO=""
    TARGET_USER="${SUDO_USER:-$USER}"
fi

echo -e "[i] Target user: ${BOLD}$TARGET_USER${NC}"

# 2. Package Manager Verification
if ! command -v apt-get >/dev/null 2>&1; then
    echo -e "${YELLOW}[!] Non-APT distribution detected. Please install adb, vainfo, and scrcpy >= 2.4 using your system package manager.${NC}"
    exit 1
fi

echo -e "\n${BOLD}[1/5] Installing System Prerequisites (ADB & Video Acceleration)...${NC}"
$SUDO apt-get update -qq
$SUDO apt-get install -y -qq \
    adb \
    curl \
    wget \
    vainfo \
    mesa-va-drivers \
    intel-media-va-driver \
    libfontconfig1 \
    libx11-6 \
    libice6 \
    libsm6 >/dev/null

echo -e " ${GREEN}✓${NC} ADB and VA-API acceleration drivers installed."

# 3. Configure udev Rules & plugdev Group Permissions
echo -e "\n${BOLD}[2/5] Configuring Android USB Permissions (udev)...${NC}"
UDEV_RULES_FILE="/etc/udev/rules.d/51-android.rules"

$SUDO tee "$UDEV_RULES_FILE" > /dev/null << 'EOF'
# udev rules for Samsung Android devices and ADB debugging
# Samsung Electronics
SUBSYSTEM=="usb", ATTR{idVendor}=="04e8", MODE="0666", GROUP="plugdev", TAG+="uaccess"

# Google (Generic ADB)
SUBSYSTEM=="usb", ATTR{idVendor}=="18d1", MODE="0666", GROUP="plugdev", TAG+="uaccess"
EOF

$SUDO chmod 644 "$UDEV_RULES_FILE"

# Ensure plugdev group exists and user is member
if ! getent group plugdev >/dev/null 2>&1; then
    $SUDO groupadd plugdev
fi

$SUDO usermod -aG plugdev "$TARGET_USER"

# Reload udev rules
if command -v udevadm >/dev/null 2>&1; then
    $SUDO udevadm control --reload-rules
    $SUDO udevadm trigger
fi

echo -e " ${GREEN}✓${NC} udev rules deployed to $UDEV_RULES_FILE."
echo -e " ${GREEN}✓${NC} User '$TARGET_USER' added to 'plugdev' group."

# 4. Check & Install Modern scrcpy (>= 2.4.0)
echo -e "\n${BOLD}[3/5] Verifying scrcpy Version...${NC}"
SCRCPY_MIN_VERSION="2.4"
NEEDS_SCRCPY=true

version_ge() {
    # Returns 0 if $1 >= $2
    [ "$(printf '%s\n' "$2" "$1" | sort -V | head -n1)" = "$2" ]
}

if command -v scrcpy >/dev/null 2>&1; then
    CURRENT_SCRCPY_VER="$(scrcpy --version 2>&1 | head -n 1 | awk '{print $2}')"
    if [ -n "$CURRENT_SCRCPY_VER" ] && version_ge "$CURRENT_SCRCPY_VER" "$SCRCPY_MIN_VERSION"; then
        echo -e " ${GREEN}✓${NC} Compatible scrcpy found: version ${BOLD}$CURRENT_SCRCPY_VER${NC} (>= $SCRCPY_MIN_VERSION)."
        NEEDS_SCRCPY=false
    else
        echo -e " ${YELLOW}!${NC} Obsolete scrcpy detected ($CURRENT_SCRCPY_VER). Version >= $SCRCPY_MIN_VERSION is required."
    fi
fi

if [ "$NEEDS_SCRCPY" = true ]; then
    echo -e " [i] Installing upstream official scrcpy binary..."
    if command -v snap >/dev/null 2>&1; then
        echo -e " [i] Installing via Snap..."
        $SUDO snap install scrcpy || true
    fi

    # Fallback to direct official static archive if snap not available or outdated
    if ! command -v scrcpy >/dev/null 2>&1 || ! version_ge "$(scrcpy --version 2>&1 | head -n 1 | awk '{print $2}')" "$SCRCPY_MIN_VERSION"; then
        echo -e " [i] Downloading official standalone scrcpy v3.1 archive..."
        TMP_DIR="$(mktemp -d)"
        TAR_URL="https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-linux-x86_64-v3.1.tar.gz"
        curl -sSL "$TAR_URL" -o "$TMP_DIR/scrcpy.tar.gz"
        $SUDO mkdir -p /usr/local/scrcpy-v3.1
        $SUDO tar -xzf "$TMP_DIR/scrcpy.tar.gz" -C /usr/local/
        $SUDO ln -sf /usr/local/scrcpy-linux-x86_64-v3.1/scrcpy /usr/local/bin/scrcpy
        rm -rf "$TMP_DIR"
    fi

    echo -e " ${GREEN}✓${NC} Official scrcpy $(scrcpy --version 2>&1 | head -n 1 | awk '{print $2}') installed."
fi

# 5. Check .NET 8 Runtime / SDK
echo -e "\n${BOLD}[4/5] Checking .NET Environment...${NC}"
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VER="$(dotnet --version)"
    echo -e " ${GREEN}✓${NC} .NET SDK found: version ${BOLD}$DOTNET_VER${NC}."
else
    echo -e " [i] .NET SDK not detected. Installing dotnet-sdk-8.0..."
    $SUDO apt-get install -y -qq dotnet-sdk-8.0 || {
        echo -e " ${YELLOW}[!] Could not install dotnet-sdk-8.0 via apt. If using prebuilt binary or package, SDK is not required.${NC}"
    }
fi

# 6. Verify ADB & Connected Devices
echo -e "\n${BOLD}[5/5] Checking Device Connection...${NC}"
$SUDO -u "$TARGET_USER" adb start-server >/dev/null 2>&1 || adb start-server >/dev/null 2>&1 || true

DEVICES_COUNT="$(adb devices 2>/dev/null | grep -v "List of devices" | grep -v "^$" | wc -l || echo 0)"
if [ "$DEVICES_COUNT" -gt 0 ]; then
    echo -e " ${GREEN}✓${NC} Detected $DEVICES_COUNT connected device(s):"
    adb devices
else
    echo -e " ${YELLOW}[i] No devices detected yet.${NC}"
    echo -e "     Connect your Samsung phone via USB with 'USB debugging' enabled."
fi

echo ""
echo -e "${BOLD}${GREEN}====================================================${NC}"
echo -e "${BOLD}${GREEN}   Setup Complete! Otimização concluída com sucesso.${NC}"
echo -e "${BOLD}${GREEN}====================================================${NC}"
echo ""
echo -e "Para iniciar o ScrcpyDeX imediatamente no Linux:"
echo -e "  ${BOLD}./scripts/run-linux.sh${NC}"
echo ""
echo -e "Ou teste diagnósticos no terminal:"
echo -e "  ${BOLD}dotnet run --project ScrcpyDex/src/ScrcpyDex.Cli -- doctor${NC}"
echo ""
