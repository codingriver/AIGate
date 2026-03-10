#!/bin/bash
# 将 build.sh / build.ps1 生成的 publish 子目录分别打成发布压缩包
# 用法: ./package.sh [版本号]
# 版本号默认 1.0.0；压缩包输出到 releases/

set -e

VERSION="${1:-1.0.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RELEASES_DIR="$SCRIPT_DIR/releases"
PUBLISH_DIR="$SCRIPT_DIR/publish"

mkdir -p "$RELEASES_DIR"

# 是否存在 gate-* 子目录（build 脚本的标准输出）
shopt -s nullglob
DIRS=("$PUBLISH_DIR"/gate-*)

if [ ${#DIRS[@]} -gt 0 ]; then
    echo "从 $PUBLISH_DIR 下各平台目录分别打包..."
    for dir in "${DIRS[@]}"; do
        [ -d "$dir" ] || continue
        name=$(basename "$dir")
        # name 例如 gate-linux-x64 或 gate-linux-x64-fd
        suffix="${name#gate-}"
        ARCHIVE_NAME="gate-v${VERSION}-${suffix}.tar.gz"
        echo "  $name -> releases/$ARCHIVE_NAME"
        tar -czf "$RELEASES_DIR/$ARCHIVE_NAME" -C "$dir" .
    done
    echo "完成，输出目录: $RELEASES_DIR"
    ls -lh "$RELEASES_DIR"/gate-v${VERSION}-*.tar.gz 2>/dev/null || ls -lh "$RELEASES_DIR"
    exit 0
fi

# 兼容旧布局：publish 根目录即为单平台输出（扁平）
if [ -d "$PUBLISH_DIR" ] && [ -n "$(ls -A "$PUBLISH_DIR" 2>/dev/null)" ]; then
    echo "从扁平 $PUBLISH_DIR 打包（未检测到 gate-* 子目录）..."
    ARCHIVE_NAME="gate-v${VERSION}-bundle.tar.gz"
    tar -czf "$RELEASES_DIR/$ARCHIVE_NAME" -C "$PUBLISH_DIR" .
    echo "完成: $RELEASES_DIR/$ARCHIVE_NAME"
    ls -lh "$RELEASES_DIR/$ARCHIVE_NAME"
    exit 0
fi

echo "提示: 未找到 $PUBLISH_DIR 或目录为空"
echo "请先运行:"
echo "  ./build.sh"
echo "或:"
echo "  dotnet publish src/ProxyTool.CLI/ProxyTool.CLI.csproj -r linux-x64 -c Release -o publish/gate-linux-x64"
exit 1
