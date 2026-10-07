#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Linux Automated Test & Verification Script
# Executes .NET Core automated tests and PowerShell prototype verification
# ==============================================================================

set -euo pipefail

CONFIGURATION="Release"
RUN_UNIT=true
RUN_PROTOTYPE=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --skip-unit)
            RUN_UNIT=false
            shift
            ;;
        --skip-prototype)
            RUN_PROTOTYPE=false
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [-c|--configuration <Release|Debug>] [--skip-unit] [--skip-prototype]"
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
echo "   ScrcpyDeX - Linux Automated Test Runner       "
echo "================================================="
echo "Configuration: $CONFIGURATION"
echo "Repository:    $REPO_ROOT"
echo ""

TEST_FAILURES=0

# 1. Automated Core Unit Tests
if [ "$RUN_UNIT" = true ]; then
    echo "[1/2] Running ScrcpyDex.Core unit test suite..."
    TEST_PROJ="$REPO_ROOT/tests/ScrcpyDex.Core.Tests/ScrcpyDex.Core.Tests.csproj"
    if [ -f "$TEST_PROJ" ] && command -v dotnet >/dev/null 2>&1; then
        if dotnet test "$TEST_PROJ" -c "$CONFIGURATION" --verbosity normal; then
            echo "[OK] ScrcpyDex.Core tests passed."
        else
            echo "[FAIL] ScrcpyDex.Core tests failed." >&2
            TEST_FAILURES=$((TEST_FAILURES + 1))
        fi
    else
        echo "[SKIP] .NET test project or dotnet command not available."
    fi
else
    echo "[1/2] Unit tests skipped (--skip-unit)."
fi

# 2. Automated Prototype Verification
if [ "$RUN_PROTOTYPE" = true ]; then
    echo "[2/2] Running Prototype Verification Suite (test-ui-prototype.ps1)..."
    PROTO_SCRIPT="$REPO_ROOT/tests/test-ui-prototype.ps1"
    if [ -f "$PROTO_SCRIPT" ] && command -v pwsh >/dev/null 2>&1; then
        # Ensure pwsh.exe is resolvable if called inside test-ui-prototype.ps1
        SHIM_DIR=""
        if ! command -v pwsh.exe >/dev/null 2>&1; then
            SHIM_DIR="$(mktemp -d -t pwsh-shim.XXXXXX)"
            ln -sf "$(command -v pwsh)" "$SHIM_DIR/pwsh.exe"
            export PATH="$SHIM_DIR:$PATH"
        fi

        if pwsh -ExecutionPolicy Bypass -NoProfile -File "$PROTO_SCRIPT"; then
            echo "[OK] Prototype verification suite passed."
        else
            echo "[FAIL] Prototype verification suite failed." >&2
            TEST_FAILURES=$((TEST_FAILURES + 1))
        fi

        if [ -n "$SHIM_DIR" ] && [ -d "$SHIM_DIR" ]; then
            rm -rf "$SHIM_DIR"
        fi
    else
        echo "[SKIP] PowerShell (pwsh) or test script not available."
    fi
else
    echo "[2/2] Prototype verification skipped (--skip-prototype)."
fi

echo "================================================="
if [ "$TEST_FAILURES" -eq 0 ]; then
    echo "All automated test suites completed successfully."
    exit 0
else
    echo "Test failures encountered: $TEST_FAILURES suite(s) failed." >&2
    exit 1
fi
