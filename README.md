# Gate

> 跨平台命令行代理配置管理工具（Gate.CLI + Gate.Core + Unity GUI）

## 功能概览

- **全局代理管理**：快速设置/清除 `HTTP_PROXY`、`HTTPS_PROXY`、`NO_PROXY`
- **应用代理配置**：一条命令为 `git`、`npm`、`pip`、`docker`、`cursor` 等 146+ 工具设置代理，支持逗号分隔批量操作
- **预设管理**：保存/加载不同场景的代理配置（公司/家/项目），一键 `gate preset load <name>` 切换
- **代理连通性测试**：一键测试代理是否可用及延迟
- **状态总览**：直接运行 `gate` 一次性查看全局代理 + 工具配置 + 预设列表
- **一站式设置**：`gate set <proxy> [tools]` 同时配置全局和应用代理
- **交互式向导**：`gate wizard` 四步引导新手完成首次配置
- **Unity GUI**：UIToolkit 图形界面，嵌入 Unity 编辑器或运行时

## 目录结构

```
AIGate/
├── src/
│   ├── Gate.Core/      核心库（netstandard2.0 DLL），同时发布到 Unity
│   ├── Gate.CLI/       命令行程序入口（net8.0）
│   └── Gate.Tests/     自动化测试（net9.0）
├── docs/
│   ├── USAGE.md        完整使用教程（命令详解 + 典型工作流）
│   ├── FEATURES.md     功能与架构文档
│   └── UI-WIREFRAME.md Unity GUI 界面布局文档
├── UnityPackage/       编译后的 Gate.Core.dll 及 Unity 包
├── build.ps1 / build.sh   构建发布脚本
Assets/
├── Scripts/AIGate/     Unity C# 控制器
└── UI/AIGate/          UIToolkit UXML + USS 文件
```

## 环境要求

- [.NET SDK 8.0+](https://dotnet.microsoft.com/)
- Unity 2021.2+（UIToolkit 支持）

## 构建

```powershell
# Windows
.\build.ps1

# Linux / macOS
chmod +x build.sh && ./build.sh
```

构建产物位于 `publish/gate-{platform}/`，Gate.Core.dll 自动复制到 `Assets/Plugins/AIGate/`。

## CLI 命令速查

```bash
gate                              # 无参数 → 当前状态总览
gate set <proxy> [tools]          # 设置全局代理，可同时配置工具
gate clear [tools]                # 清除全局代理或工具代理
gate app <name> [<proxy>]         # 查看/设置工具代理（支持批量）
gate apps [--installed]           # 列出所有支持工具
gate env                          # 查看三层环境变量代理详情
gate preset save|load|del <name>  # 预设管理
gate test [<proxy>]               # 测试代理连通性
gate list [apps|presets]          # 列出工具或预设
gate path [-n <tool>]             # 工具自定义路径
gate wizard                       # 交互式配置向导（新手推荐）
gate info                         # 状态总览别名
```

运行 `gate <命令> -h` 查看任意命令的详细帮助。

## 快速上手

```bash
# 首次配置（推荐）
gate wizard

# 或手动配置
gate set http://127.0.0.1:7890 git,npm,pip --verify

# 保存为预设
gate preset save office

# 查看当前状态
gate

# 切换预设
gate preset load office
```

## 向后兼容

旧命令均保留为隐藏别名，现有脚本无需修改：

| 旧命令 | 新命令 |
|--------|--------|
| `gate global -p <proxy>` | `gate set <proxy>` |
| `gate app -n git -p <proxy>` | `gate app git <proxy>` |
| `gate preset -n office --save` | `gate preset save office` |
| `gate apply office` | `gate preset load office` |
| `gate list apps` | `gate apps` |

## 文档

- **[docs/USAGE.md](docs/USAGE.md)** — 完整使用教程（命令详解 + 典型工作流 + 常见问题）
- [docs/FEATURES.md](docs/FEATURES.md) — 功能与架构文档
- [docs/UI-WIREFRAME.md](docs/UI-WIREFRAME.md) — Unity GUI 界面布局

## 开发与测试

```bash
dotnet test src/Gate.Tests/Gate.Tests.csproj
```

## TODO

以下功能已规划，优先级低，待后续实现：

### 跨平台系统代理读取（`gate env` 显示）

当前 `gate env` 仅在 Windows 上显示注册表系统代理。其他平台尚未实现：

- **macOS**：`scutil --proxy`
- **Linux GNOME**：`gsettings get org.gnome.system.proxy mode`
- **Linux KDE**：`~/.config/kioslaverc`
