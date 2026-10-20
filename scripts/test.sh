#!/usr/bin/env bash
# ==============================================================================
# ScrcpyDeX - Linux Automated Test & Verification Script
# Executes .NET Core automated tests
# ==============================================================================

set -euo pipefail

CONFIGURATION="Release"
RUN_UNIT=true

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
        -h|--help)
            echo "Usage: $0 [-c|--configuration <Release|Debug>] [--skip-unit]"
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
    echo "Running ScrcpyDex.Core unit test suite..."
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
    echo "Unit tests skipped (--skip-unit)."
fi

echo "================================================="
if [ "$TEST_FAILURES" -eq 0 ]; then
    echo "All automated test suites completed successfully."
    exit 0
else
    echo "Test failures encountered: $TEST_FAILURES suite(s) failed." >&2
    exit 1
fi
