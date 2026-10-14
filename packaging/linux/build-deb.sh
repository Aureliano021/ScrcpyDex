#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Debian Package Build Script
# Generates scrcpydex_${VERSION}_amd64.deb adhering to Debian policy
# ==============================================================================

set -euo pipefail

VERSION="${1:-${SCRCPYDEX_VERSION:-2.0.0}}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT_DIR="${OUTPUT_DIR:-$REPO_ROOT/publish}"

if [ -d "$REPO_ROOT/ScrcpyDex/src" ]; then
    BASE_DIR="$REPO_ROOT/ScrcpyDex"
else
    BASE_DIR="$REPO_ROOT"
fi

echo "[ScrcpyDeX Debian Packaging] Target version: $VERSION"
echo "[ScrcpyDeX Debian Packaging] Repository root: $REPO_ROOT"
echo "[ScrcpyDeX Debian Packaging] Output directory: $OUTPUT_DIR"

STAGE_DIR="$(mktemp -d -t scrcpydex-deb.XXXXXX)"
cleanup() {
    rm -rf "$STAGE_DIR"
}
trap cleanup EXIT

PKG_DIR="$STAGE_DIR/scrcpydex_${VERSION}_amd64"

safe_cp() {
    cat "$1" > "$2"
}
safe_cp_r() {
    mkdir -p "$2"
    (cd "$1" && tar cf - .) | (cd "$2" && tar xf -)
}

# 1. Create Filesystem Layout
mkdir -p "$PKG_DIR/DEBIAN"
mkdir -p "$PKG_DIR/usr/bin"
mkdir -p "$PKG_DIR/usr/lib/scrcpydex"
mkdir -p "$PKG_DIR/usr/share/scrcpydex/server"
mkdir -p "$PKG_DIR/lib/udev/rules.d"
mkdir -p "$PKG_DIR/usr/share/applications"
mkdir -p "$PKG_DIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$OUTPUT_DIR"

# 2. Stage Server JAR and Helper Scripts
SERVER_JAR=""
if [ -f "$REPO_ROOT/server/scrcpydex-server.jar" ]; then
    SERVER_JAR="$REPO_ROOT/server/scrcpydex-server.jar"
elif [ -f "$REPO_ROOT/publish/scrcpydex-server.jar" ]; then
    SERVER_JAR="$REPO_ROOT/publish/scrcpydex-server.jar"
fi

if [ -n "$SERVER_JAR" ] && [ -f "$SERVER_JAR" ]; then
    safe_cp "$SERVER_JAR" "$PKG_DIR/usr/share/scrcpydex/server/scrcpydex-server.jar"
    # Create symlink for compatibility with existing candidate search paths
    ln -s "server/scrcpydex-server.jar" "$PKG_DIR/usr/share/scrcpydex/scrcpydex-server.jar"
else
    echo "[ERROR] scrcpydex-server.jar not found." >&2
    exit 1
fi

if [ -f "$REPO_ROOT/packaging/linux/start-dex.sh" ]; then
    safe_cp "$REPO_ROOT/packaging/linux/start-dex.sh" "$PKG_DIR/usr/share/scrcpydex/start-dex.sh"
    chmod 755 "$PKG_DIR/usr/share/scrcpydex/start-dex.sh"
fi

if [ -f "$REPO_ROOT/packaging/linux/stop-dex.sh" ]; then
    safe_cp "$REPO_ROOT/packaging/linux/stop-dex.sh" "$PKG_DIR/usr/share/scrcpydex/stop-dex.sh"
    chmod 755 "$PKG_DIR/usr/share/scrcpydex/stop-dex.sh"
fi

# 3. Compile or Stage /usr/bin/scrcpydex (GUI App)
mkdir -p "$PKG_DIR/usr/lib/scrcpydex/app"
APP_CSPROJ="$BASE_DIR/src/ScrcpyDex.App/ScrcpyDex.App.csproj"
if [ -d "$BASE_DIR/publish/staging/app" ] && [ -n "$(ls -A "$BASE_DIR/publish/staging/app" 2>/dev/null)" ]; then
    echo "[ScrcpyDeX Debian Packaging] Staging pre-compiled ScrcpyDex.App from staging/app..."
    safe_cp_r "$BASE_DIR/publish/staging/app" "$PKG_DIR/usr/lib/scrcpydex/app"
    chmod 755 "$PKG_DIR/usr/lib/scrcpydex/app/ScrcpyDex.App" || true
    cat << 'EOF' > "$PKG_DIR/usr/bin/scrcpydex"
#!/bin/sh
exec /usr/lib/scrcpydex/app/ScrcpyDex.App "$@"
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex"
elif [ -f "$APP_CSPROJ" ] && command -v dotnet >/dev/null 2>&1; then
    echo "[ScrcpyDeX Debian Packaging] Compiling ScrcpyDex.App (self-contained linux-x64)..."
    dotnet publish "$APP_CSPROJ" -c Release -r linux-x64 --self-contained true -o "$PKG_DIR/usr/lib/scrcpydex/app"
    cat << 'EOF' > "$PKG_DIR/usr/bin/scrcpydex"
#!/bin/sh
exec /usr/lib/scrcpydex/app/ScrcpyDex.App "$@"
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex"
else
    echo "[ScrcpyDeX Debian Packaging] Staging launcher script for /usr/bin/scrcpydex..."
    cat << 'EOF' > "$PKG_DIR/usr/bin/scrcpydex"
#!/usr/bin/env bash
set -e
exec /usr/share/scrcpydex/start-dex.sh "$@"
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex"
fi

# 4. Compile or Stage /usr/bin/scrcpydex-cli (CLI Utility)
mkdir -p "$PKG_DIR/usr/lib/scrcpydex/cli"
CLI_CSPROJ="$BASE_DIR/src/ScrcpyDex.Cli/ScrcpyDex.Cli.csproj"
if [ -d "$BASE_DIR/publish/staging/cli" ] && [ -n "$(ls -A "$BASE_DIR/publish/staging/cli" 2>/dev/null)" ]; then
    echo "[ScrcpyDeX Debian Packaging] Staging pre-compiled ScrcpyDex.Cli from staging/cli..."
    safe_cp_r "$BASE_DIR/publish/staging/cli" "$PKG_DIR/usr/lib/scrcpydex/cli"
    chmod 755 "$PKG_DIR/usr/lib/scrcpydex/cli/scrcpy-dex" || true
    cat << 'EOF' > "$PKG_DIR/usr/bin/scrcpydex-cli"
#!/bin/sh
exec /usr/lib/scrcpydex/cli/scrcpy-dex "$@"
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex-cli"
    ln -sf "scrcpydex-cli" "$PKG_DIR/usr/bin/scrcpy-dex"
elif [ -f "$CLI_CSPROJ" ] && command -v dotnet >/dev/null 2>&1; then
    echo "[ScrcpyDeX Debian Packaging] Compiling ScrcpyDex.Cli (self-contained linux-x64)..."
    dotnet publish "$CLI_CSPROJ" -c Release -r linux-x64 --self-contained true -o "$PKG_DIR/usr/lib/scrcpydex/cli"
    CLI_BIN="$(find "$PKG_DIR/usr/lib/scrcpydex/cli" -maxdepth 1 -type f -executable -not -name "*.so" | head -n 1)"
    CLI_NAME="$(basename "${CLI_BIN:-scrcpy-dex}")"
    cat << EOF > "$PKG_DIR/usr/bin/scrcpydex-cli"
#!/bin/sh
exec /usr/lib/scrcpydex/cli/$CLI_NAME "\$@"
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex-cli"
    ln -sf "scrcpydex-cli" "$PKG_DIR/usr/bin/scrcpy-dex"
else
    echo "[ScrcpyDeX Debian Packaging] Staging CLI wrapper for /usr/bin/scrcpydex-cli..."
    cat << 'EOF' > "$PKG_DIR/usr/bin/scrcpydex-cli"
#!/usr/bin/env bash
set -e
case "${1:-}" in
    stop|disconnect|kill)
        shift
        exec /usr/share/scrcpydex/stop-dex.sh "$@"
        ;;
    *)
        exec /usr/share/scrcpydex/start-dex.sh "$@"
        ;;
esac
EOF
    chmod 755 "$PKG_DIR/usr/bin/scrcpydex-cli"
fi

# 5. Stage Udev Rules
if [ -f "$REPO_ROOT/packaging/linux/51-android.rules" ]; then
    safe_cp "$REPO_ROOT/packaging/linux/51-android.rules" "$PKG_DIR/lib/udev/rules.d/69-scrcpydex.rules"
    chmod 644 "$PKG_DIR/lib/udev/rules.d/69-scrcpydex.rules"
fi

# 6. Stage Desktop Launcher
if [ -f "$REPO_ROOT/packaging/linux/scrcpydex.desktop" ]; then
    sed 's|^Exec=.*|Exec=/usr/bin/scrcpydex|' "$REPO_ROOT/packaging/linux/scrcpydex.desktop" > "$PKG_DIR/usr/share/applications/scrcpydex.desktop"
    chmod 644 "$PKG_DIR/usr/share/applications/scrcpydex.desktop"
fi

# 7. Stage Application Icon
if [ -f "$REPO_ROOT/assets/app_icon.png" ]; then
    safe_cp "$REPO_ROOT/assets/app_icon.png" "$PKG_DIR/usr/share/icons/hicolor/256x256/apps/scrcpydex.png"
    chmod 644 "$PKG_DIR/usr/share/icons/hicolor/256x256/apps/scrcpydex.png"
fi

# 8. Stage Debian Control Metadata & Maintainer Scripts
CONTROL_SRC="$SCRIPT_DIR/debian/control"
if [ -f "$CONTROL_SRC" ]; then
    sed "s|^Version:.*|Version: $VERSION|" "$CONTROL_SRC" > "$PKG_DIR/DEBIAN/control"
    chmod 644 "$PKG_DIR/DEBIAN/control"
else
    echo "[ERROR] Debian control template not found at $CONTROL_SRC" >&2
    exit 1
fi

if [ -f "$SCRIPT_DIR/debian/postinst" ]; then
    safe_cp "$SCRIPT_DIR/debian/postinst" "$PKG_DIR/DEBIAN/postinst"
    chmod 755 "$PKG_DIR/DEBIAN/postinst"
fi

if [ -f "$SCRIPT_DIR/debian/prerm" ]; then
    safe_cp "$SCRIPT_DIR/debian/prerm" "$PKG_DIR/DEBIAN/prerm"
    chmod 755 "$PKG_DIR/DEBIAN/prerm"
fi

# 9. Build Debian Binary Package
OUTPUT_DEB="$OUTPUT_DIR/scrcpydex_${VERSION}_amd64.deb"
echo "[ScrcpyDeX Debian Packaging] Building package: $OUTPUT_DEB"

if ! command -v dpkg-deb >/dev/null 2>&1; then
    echo "[ERROR] dpkg-deb command not available. Install dpkg or run on a Debian/Ubuntu system." >&2
    exit 1
fi

dpkg-deb --build --root-owner-group "$PKG_DIR" "$OUTPUT_DEB"

echo "[ScrcpyDeX Debian Packaging] Successfully generated: $OUTPUT_DEB"
ls -lh "$OUTPUT_DEB"
