#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Linux Build Script
# Restores dependencies and builds Core, Linux Platform, CLI, and Avalonia App
# ==============================================================================

set -euo pipefail

CONFIGURATION="Release"
DO_RESTORE=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --no-restore)
            DO_RESTORE=false
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [-c|--configuration <Release|Debug>] [--no-restore]"
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "================================================="
echo "   ScrcpyDeX - Linux Build Automation            "
echo "================================================="
echo "Configuration: $CONFIGURATION"
echo "Repository:    $REPO_ROOT"
echo ""

if ! command -v dotnet >/dev/null 2>&1; then
    echo "[ERROR] .NET SDK (dotnet) not found in PATH." >&2
    echo "Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0" >&2
    exit 1
fi

PROJECTS=(
    "src/ScrcpyDex.Core/ScrcpyDex.Core.csproj"
    "src/ScrcpyDex.Platform.Linux/ScrcpyDex.Platform.Linux.csproj"
    "src/ScrcpyDex.Cli/ScrcpyDex.Cli.csproj"
    "src/ScrcpyDex.App/ScrcpyDex.App.csproj"
    "tests/ScrcpyDex.Core.Tests/ScrcpyDex.Core.Tests.csproj"
)

# 1. Dependency Restoration
if [ "$DO_RESTORE" = true ]; then
    echo "[1/2] Restoring dependencies..."
    for rel_proj in "${PROJECTS[@]}"; do
        full_proj="$REPO_ROOT/$rel_proj"
        if [ -f "$full_proj" ]; then
            echo "  Restoring $rel_proj..."
            dotnet restore "$full_proj"
        fi
    done
fi

# 2. Build Execution
echo "[2/2] Building projects..."
BUILD_COUNT=0
for rel_proj in "${PROJECTS[@]}"; do
    full_proj="$REPO_ROOT/$rel_proj"
    if [ -f "$full_proj" ]; then
        echo "  Building $rel_proj ($CONFIGURATION)..."
        dotnet build "$full_proj" -c "$CONFIGURATION" --no-restore
        BUILD_COUNT=$((BUILD_COUNT + 1))
    fi
done

echo "================================================="
echo "Build completed: $BUILD_COUNT project(s) compiled successfully."
echo "================================================="
