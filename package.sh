#!/bin/bash
# 将已发布内容打包为发布包
# 用法: ./package.sh [版本号]
# 若未运行 build.sh，可先手动 publish 到 ./publish，再运行此脚本

set -e

VERSION="${1:-1.0.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RELEASES_DIR="$SCRIPT_DIR/releases"
PUBLISH_DIR="$SCRIPT_DIR/publish"

mkdir -p "$RELEASES_DIR"

# 优先从 publish 目录打包（兼容手动 publish 场景）
if [ -d "$PUBLISH_DIR" ] && [ -n "$(ls -A "$PUBLISH_DIR" 2>/dev/null)" ]; then
    echo "从 $PUBLISH_DIR 打包..."
    ARCHIVE_NAME="proxy-tool-v${VERSION}-linux-x64.tar.gz"
    tar -czf "$RELEASES_DIR/$ARCHIVE_NAME" -C "$PUBLISH_DIR" .
    echo "完成: $RELEASES_DIR/$ARCHIVE_NAME"
    ls -lh "$RELEASES_DIR/$ARCHIVE_NAME"
else
    echo "提示: 未找到 $PUBLISH_DIR 目录或目录为空"
    echo "请先运行 ./build.sh 进行完整构建，或手动执行:"
    echo "  dotnet publish src/ProxyTool.CLI/ProxyTool.CLI.csproj -r linux-x64 -c Release -o publish"
    exit 1
fi
