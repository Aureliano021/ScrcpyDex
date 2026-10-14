#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - One-Click Linux Launcher
# Automatically locates and runs the ScrcpyDeX desktop interface or CLI
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Resolve source root
if [ -d "$REPO_ROOT/ScrcpyDex/src" ]; then
    BASE_DIR="$REPO_ROOT/ScrcpyDex"
else
    BASE_DIR="$REPO_ROOT"
fi

APP_PROJECT="$BASE_DIR/src/ScrcpyDex.App/ScrcpyDex.App.csproj"
CLI_PROJECT="$BASE_DIR/src/ScrcpyDex.Cli/ScrcpyDex.Cli.csproj"

# Check if user requested CLI
USE_CLI=false
ARGS=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --cli)
            USE_CLI=true
            shift
            ;;
        *)
            ARGS+=("$1")
            shift
            ;;
    esac
done

if [ "$USE_CLI" = true ]; then
    echo "[ScrcpyDeX] Launching Command-Line Interface..."
    exec dotnet run --project "$CLI_PROJECT" -- "${ARGS[@]}"
else
    echo "[ScrcpyDeX] Launching Desktop GUI..."
    exec dotnet run --project "$APP_PROJECT" -- "${ARGS[@]}"
fi
