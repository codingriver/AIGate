#!/bin/bash
# Gate Build Script（Linux/macOS）- 仅打包 CLI，Core 随 CLI 引用发布
# 支持自包含与框架依赖。输出到 publish/ 子目录（不生成压缩包）。
# Usage:
#   ./build.sh                    # 全量自包含 win/linux/osx
#   ./build.sh --fd               # 框架依赖（体积更小）
#   ./build.sh --runtimes "linux-x64,osx-x64"
#   ./build.sh --version "1.1.0"

set -e

VERSION="${VERSION:-1.0.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/src"
PUBLISH_DIR="$SCRIPT_DIR/publish"
SLN_PATH="$SRC_DIR/Gate.sln"
CLI_PROJECT="$SRC_DIR/Gate.CLI/Gate.CLI.csproj"

RUNTIMES="win-x64,linux-x64,osx-x64"
SELF_CONTAINED=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --fd)              SELF_CONTAINED=false; shift ;;
        --runtimes)       RUNTIMES="$2"; shift 2 ;;
        --version)        VERSION="$2"; shift 2 ;;
        --cli-only)       shift ;; # 已仅 CLI，保留参数以兼容旧脚本
        *) echo "Unknown: $1"; exit 1 ;;
    esac
done

DEPLOY_SUFFIX=$([ "$SELF_CONTAINED" = true ] && echo "" || echo "-fd")

echo "========================================"
echo "  Gate Build Script v$VERSION"
echo "  Target: CLI only (Core as library)"
echo "  Mode: $([ "$SELF_CONTAINED" = true ] && echo "Self-contained" || echo "Framework-dependent")"
echo "========================================"
echo ""

if ! command -v dotnet &>/dev/null; then
    echo "Error: dotnet CLI not found. Please install .NET SDK."
    exit 1
fi

# 0. Clean publish directory for fresh output
if [ -d "$PUBLISH_DIR" ]; then
  echo "Cleaning existing publish directory: $PUBLISH_DIR"
  rm -rf "$PUBLISH_DIR"
fi

mkdir -p "$PUBLISH_DIR"

echo "[1/4] Restoring NuGet packages..."
dotnet restore "$SLN_PATH" -clp:ErrorsOnly

echo "[2/4] Building..."
dotnet build "$SLN_PATH" -c Release --no-restore -p:WarningLevel=0 -clp:ErrorsOnly

echo "[3/4] Publishing CLI..."

publish_cli() {
    local rid=$1
    local out_dir="$PUBLISH_DIR/gate-$rid$DEPLOY_SUFFIX"
    echo "  Publish CLI -> $rid"
    if [ "$SELF_CONTAINED" = true ]; then
        dotnet publish "$CLI_PROJECT" -r "$rid" -c Release -clp:ErrorsOnly \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -p:EnableCompressionInSingleFile=true \
            -p:DebugType=None -p:DebugSymbols=false \
            -o "$out_dir"
    else
        dotnet publish "$CLI_PROJECT" -r "$rid" -c Release -clp:ErrorsOnly \
            --self-contained false \
            -p:PublishSingleFile=true \
            -p:DebugType=None -p:DebugSymbols=false \
            -o "$out_dir"
    fi
    echo "    -> $out_dir"
}

IFS=',' read -ra RIDS <<< "$RUNTIMES"
for rid in "${RIDS[@]}"; do
    rid=$(echo "$rid" | xargs)
    [[ -z "$rid" ]] && continue
    publish_cli "$rid"
done

echo "[4/4] Cleaning build cache..."
dotnet clean "$SLN_PATH" -c Release -v q 2>/dev/null || true
for dir in obj bin; do
    find "$SRC_DIR" -type d -name "$dir" 2>/dev/null | sort -r | while read -r d; do [ -d "$d" ] && rm -rf "$d"; done
done
rm -rf "$SCRIPT_DIR/obj" "$SCRIPT_DIR/bin" 2>/dev/null || true
echo "  Cache cleaned"

echo ""
echo "========================================"
echo "  Build completed!"
echo "  Output: $PUBLISH_DIR"
echo "  Run ./package.sh [version] to create tar.gz per RID"
echo "========================================"
ls -d "$PUBLISH_DIR"/*/ 2>/dev/null || true
