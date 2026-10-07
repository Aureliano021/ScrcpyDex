#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Standalone AppImage Build Script
# Packages ScrcpyDeX into a self-contained AppImage using appimagetool
# ==============================================================================

set -euo pipefail

VERSION="${1:-${SCRCPYDEX_VERSION:-1.1.0}}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
OUTPUT_DIR="${OUTPUT_DIR:-$REPO_ROOT/publish}"

echo "[ScrcpyDeX AppImage] Target version: $VERSION"
echo "[ScrcpyDeX AppImage] Repository root: $REPO_ROOT"
echo "[ScrcpyDeX AppImage] Output directory: $OUTPUT_DIR"

APPDIR="$(mktemp -d -t scrcpydex-appdir.XXXXXX)"
cleanup() {
    rm -rf "$APPDIR"
}
trap cleanup EXIT

# 1. Create Filesystem Structure
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/lib"
mkdir -p "$APPDIR/usr/lib/scrcpydex"
mkdir -p "$APPDIR/usr/share/scrcpydex/server"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$OUTPUT_DIR"

# 2. Stage Server JAR & Shell Scripts
SERVER_JAR=""
if [ -f "$REPO_ROOT/server/scrcpydex-server.jar" ]; then
    SERVER_JAR="$REPO_ROOT/server/scrcpydex-server.jar"
elif [ -f "$REPO_ROOT/publish/scrcpydex-server.jar" ]; then
    SERVER_JAR="$REPO_ROOT/publish/scrcpydex-server.jar"
fi

if [ -n "$SERVER_JAR" ] && [ -f "$SERVER_JAR" ]; then
    cp "$SERVER_JAR" "$APPDIR/usr/share/scrcpydex/server/scrcpydex-server.jar"
    ln -s "server/scrcpydex-server.jar" "$APPDIR/usr/share/scrcpydex/scrcpydex-server.jar"
else
    echo "[ERROR] scrcpydex-server.jar not found." >&2
    exit 1
fi

if [ -f "$REPO_ROOT/packaging/linux/start-dex.sh" ]; then
    cp "$REPO_ROOT/packaging/linux/start-dex.sh" "$APPDIR/usr/share/scrcpydex/start-dex.sh"
    chmod 755 "$APPDIR/usr/share/scrcpydex/start-dex.sh"
fi

if [ -f "$REPO_ROOT/packaging/linux/stop-dex.sh" ]; then
    cp "$REPO_ROOT/packaging/linux/stop-dex.sh" "$APPDIR/usr/share/scrcpydex/stop-dex.sh"
    chmod 755 "$APPDIR/usr/share/scrcpydex/stop-dex.sh"
fi

# 3. Stage Binaries
APP_CSPROJ="$REPO_ROOT/src/ScrcpyDex.App/ScrcpyDex.App.csproj"
if [ -f "$APP_CSPROJ" ] && command -v dotnet >/dev/null 2>&1; then
    echo "[ScrcpyDeX AppImage] Publishing ScrcpyDex.App (linux-x64)..."
    dotnet publish "$APP_CSPROJ" -c Release -r linux-x64 --self-contained false -o "$APPDIR/usr/lib/scrcpydex/app"
    cat << 'EOF' > "$APPDIR/usr/bin/scrcpydex"
#!/bin/sh
exec "$APPDIR/usr/lib/scrcpydex/app/ScrcpyDex.App" "$@"
EOF
    chmod 755 "$APPDIR/usr/bin/scrcpydex"
else
    echo "[ScrcpyDeX AppImage] Staging launcher script for /usr/bin/scrcpydex..."
    cat << 'EOF' > "$APPDIR/usr/bin/scrcpydex"
#!/usr/bin/env bash
set -e
exec "$APPDIR/usr/share/scrcpydex/start-dex.sh" "$@"
EOF
    chmod 755 "$APPDIR/usr/bin/scrcpydex"
fi

CLI_CSPROJ="$REPO_ROOT/src/ScrcpyDex.Cli/ScrcpyDex.Cli.csproj"
if [ -f "$CLI_CSPROJ" ] && command -v dotnet >/dev/null 2>&1; then
    echo "[ScrcpyDeX AppImage] Publishing ScrcpyDex.Cli (linux-x64)..."
    dotnet publish "$CLI_CSPROJ" -c Release -r linux-x64 --self-contained false -o "$APPDIR/usr/lib/scrcpydex/cli"
    CLI_BIN="$(find "$APPDIR/usr/lib/scrcpydex/cli" -maxdepth 1 -type f -executable -not -name "*.so" | head -n 1)"
    CLI_NAME="$(basename "${CLI_BIN:-scrcpy-dex}")"
    cat << EOF > "$APPDIR/usr/bin/scrcpydex-cli"
#!/bin/sh
exec "\$APPDIR/usr/lib/scrcpydex/cli/$CLI_NAME" "\$@"
EOF
    chmod 755 "$APPDIR/usr/bin/scrcpydex-cli"
    ln -sf "scrcpydex-cli" "$APPDIR/usr/bin/scrcpy-dex"
else
    echo "[ScrcpyDeX AppImage] Staging CLI wrapper for /usr/bin/scrcpydex-cli..."
    cat << 'EOF' > "$APPDIR/usr/bin/scrcpydex-cli"
#!/usr/bin/env bash
set -e
case "${1:-}" in
    stop|disconnect|kill)
        shift
        exec "$APPDIR/usr/share/scrcpydex/stop-dex.sh" "$@"
        ;;
    *)
        exec "$APPDIR/usr/share/scrcpydex/start-dex.sh" "$@"
        ;;
esac
EOF
    chmod 755 "$APPDIR/usr/bin/scrcpydex-cli"
fi

# 4. Stage Desktop File and Icons
if [ -f "$REPO_ROOT/packaging/linux/scrcpydex.desktop" ]; then
    sed 's|^Exec=.*|Exec=scrcpydex|' "$REPO_ROOT/packaging/linux/scrcpydex.desktop" > "$APPDIR/scrcpydex.desktop"
    cp "$APPDIR/scrcpydex.desktop" "$APPDIR/usr/share/applications/scrcpydex.desktop"
    chmod 644 "$APPDIR/scrcpydex.desktop" "$APPDIR/usr/share/applications/scrcpydex.desktop"
fi

if [ -f "$REPO_ROOT/assets/app_icon.png" ]; then
    cp "$REPO_ROOT/assets/app_icon.png" "$APPDIR/scrcpydex.png"
    cp "$REPO_ROOT/assets/app_icon.png" "$APPDIR/.DirIcon"
    cp "$REPO_ROOT/assets/app_icon.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/scrcpydex.png"
    chmod 644 "$APPDIR/scrcpydex.png" "$APPDIR/.DirIcon" "$APPDIR/usr/share/icons/hicolor/256x256/apps/scrcpydex.png"
fi

# 5. Stage AppRun Entrypoint
cp "$SCRIPT_DIR/AppRun" "$APPDIR/AppRun"
chmod 755 "$APPDIR/AppRun"

# 6. Locate or Download appimagetool
APPIMAGETOOL=""
if command -v appimagetool >/dev/null 2>&1; then
    APPIMAGETOOL="$(command -v appimagetool)"
elif [ -f "/tmp/appimagetool-x86_64.AppImage" ]; then
    APPIMAGETOOL="/tmp/appimagetool-x86_64.AppImage"
else
    echo "[ScrcpyDeX AppImage] Downloading appimagetool..."
    TOOL_URL="https://github.com/AppImage/AppImageKit/releases/download/13/appimagetool-x86_64.AppImage"
    curl -sSL -o /tmp/appimagetool-x86_64.AppImage "$TOOL_URL" || wget -qO /tmp/appimagetool-x86_64.AppImage "$TOOL_URL"
    chmod +x /tmp/appimagetool-x86_64.AppImage
    APPIMAGETOOL="/tmp/appimagetool-x86_64.AppImage"
fi

# 7. Generate AppImage
OUTPUT_APPIMAGE="$OUTPUT_DIR/ScrcpyDeX-${VERSION}-x86_64.AppImage"
echo "[ScrcpyDeX AppImage] Packaging: $OUTPUT_APPIMAGE"

# Use --appimage-extract-and-run if FUSE is not mounted
export ARCH=x86_64
if "$APPIMAGETOOL" --version >/dev/null 2>&1; then
    "$APPIMAGETOOL" "$APPDIR" "$OUTPUT_APPIMAGE"
else
    "$APPIMAGETOOL" --appimage-extract-and-run "$APPDIR" "$OUTPUT_APPIMAGE"
fi

echo "[ScrcpyDeX AppImage] Successfully generated: $OUTPUT_APPIMAGE"
ls -lh "$OUTPUT_APPIMAGE"
