#!/bin/bash
# ProxyTool Build Script (Linux/macOS) - packaging only, no tests
# Supports self-contained and framework-dependent. Outputs to publish/ (no archives).
# Usage:
#   ./build.sh                    # Full build, self-contained
#   ./build.sh --fd               # Framework-dependent (smaller)
#   ./build.sh --cli-only         # CLI only
#   ./build.sh --api-only         # API only
#   ./build.sh --runtimes "linux-x64,win-x64"

set -e

VERSION="${VERSION:-1.0.0}"
SRC_DIR="$(cd "$(dirname "$0")/src" && pwd)"
PUBLISH_DIR="$(cd "$(dirname "$0")" && pwd)/publish"
SLN_PATH="$SRC_DIR/ProxyTool.sln"
CLI_PROJECT="$SRC_DIR/ProxyTool.CLI/ProxyTool.CLI.csproj"
API_PROJECT="$SRC_DIR/ProxyTool.API/ProxyTool.API.csproj"

PROJECTS="all"
RUNTIMES="win-x64,linux-x64,osx-x64"
SELF_CONTAINED=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --fd)              SELF_CONTAINED=false; shift ;;
        --cli-only)       PROJECTS="cli"; shift ;;
        --api-only)       PROJECTS="api"; shift ;;
        --runtimes)       RUNTIMES="$2"; shift 2 ;;
        --version)        VERSION="$2"; shift 2 ;;
        *) echo "Unknown: $1"; exit 1 ;;
    esac
done

DEPLOY_SUFFIX=$([ "$SELF_CONTAINED" = true ] && echo "" || echo "-fd")

echo "========================================"
echo "  ProxyTool Build Script v$VERSION"
echo "  Mode: $([ "$SELF_CONTAINED" = true ] && echo "Self-contained" || echo "Framework-dependent")"
echo "========================================"
echo ""

if ! command -v dotnet &>/dev/null; then
    echo "Error: dotnet CLI not found. Please install .NET SDK."
    exit 1
fi

mkdir -p "$PUBLISH_DIR"

echo "[1/4] Restoring NuGet packages..."
dotnet restore "$SLN_PATH"

echo "[2/4] Building..."
dotnet build "$SLN_PATH" -c Release --no-restore

echo "[3/4] Publishing..."

publish_cli() {
    local rid=$1
    local out_dir="$PUBLISH_DIR/proxy-tool-$rid$DEPLOY_SUFFIX"
    echo "  Publish CLI -> $rid"
    if [ "$SELF_CONTAINED" = true ]; then
        dotnet publish "$CLI_PROJECT" -r "$rid" -c Release \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -p:EnableCompressionInSingleFile=true \
            -p:DebugType=None -p:DebugSymbols=false \
            -o "$out_dir"
    else
        dotnet publish "$CLI_PROJECT" -r "$rid" -c Release \
            --self-contained false \
            -p:PublishSingleFile=true \
            -p:DebugType=None -p:DebugSymbols=false \
            -o "$out_dir"
    fi
    echo "    -> $out_dir"
}

publish_api() {
    local rid=$1
    local out_dir="$PUBLISH_DIR/proxy-tool-api-$rid$DEPLOY_SUFFIX"
    echo "  Publish API -> $rid"
    dotnet publish "$API_PROJECT" -r "$rid" -c Release \
        --self-contained $SELF_CONTAINED \
        -p:DebugType=None -p:DebugSymbols=false \
        -o "$out_dir"
    echo "    -> $out_dir"
}

IFS=',' read -ra RIDS <<< "$RUNTIMES"
for rid in "${RIDS[@]}"; do
    rid=$(echo "$rid" | xargs)
    [[ -z "$rid" ]] && continue

    if [ "$PROJECTS" = "all" ] || [ "$PROJECTS" = "cli" ]; then
        publish_cli "$rid"
    fi
    if [ "$PROJECTS" = "all" ] || [ "$PROJECTS" = "api" ]; then
        publish_api "$rid"
    fi
done

echo "[4/4] Cleaning build cache..."
dotnet clean "$SLN_PATH" -c Release -v q 2>/dev/null || true
for dir in obj bin; do
    find "$SRC_DIR" -type d -name "$dir" 2>/dev/null | sort -r | while read -r d; do [ -d "$d" ] && rm -rf "$d"; done
done
rm -rf "$(dirname "$0")/obj" "$(dirname "$0")/bin" 2>/dev/null || true
echo "  Cache cleaned"

echo ""
echo "========================================"
echo "  Build completed!"
echo "  Output: $PUBLISH_DIR"
echo "========================================"
ls -d "$PUBLISH_DIR"/*/ 2>/dev/null || true
