# Gate

> 跨平台命令行代理配置管理工具（Gate.CLI + Gate.Core + Unity GUI）

## 功能概览

- **全局代理管理**：快速设置/清除 `HTTP_PROXY`、`HTTPS_PROXY`、`NO_PROXY`
- **应用代理配置**：一条命令为 `git`、`npm`、`pip`、`docker`、`cursor` 等 130+ 工具设置代理，支持逗号分隔批量操作
- **预设管理**：保存/加载不同场景的代理配置（公司/家/项目），一键 `gate apply <name>` 切换
- **代理连通性测试**：一键测试代理是否可用及延迟
- **状态总览**：`gate info` 一次性查看全局代理 + 工具配置 + 预设列表
- **一站式设置**：`gate set` 同时配置全局和应用代理
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
│   ├── FEATURES.md     功能文档
│   ├── USAGE.md        CLI 命令详细文档
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

| 命令 | 别名 | 说明 |
|------|------|------|
| `gate global` | `env` | 全局代理环境变量管理 |
| `gate app` | `tool` | 应用代理配置（支持批量） |
| `gate preset` | `profile` | 预设配置集管理 |
| `gate info` | `status`, `show` | 当前代理状态总览 |
| `gate test` | `check` | 代理连通性测试 |
| `gate set` | — | 一站式快速配置 |
| `gate apply <name>` | — | 直接应用预设 |
| `gate list [apps\|presets]` | — | 统一列出资源 |
| `gate wizard` | — | 交互式向导（新手推荐） |

### 常用短选项

| 短选项 | 长选项 | 用途 |
|--------|--------|------|
| `-p` | `--proxy` | 代理地址（同时设置 HTTP/HTTPS） |
| `-H` | — | 单独指定 HTTP 代理 |
| `-S` | — | 单独指定 HTTPS 代理 |
| `-n` | `--name` | 应用/预设名称 |
| `-l` | `--list` | 列出信息 |
| `-c` | `--clear` | 清除配置 |
| `-v` | `--verify` | 测试代理连通性 |
| `-g` | `--global` | 设置全局代理（set 命令） |
| `-a` | `--app` | 指定应用（set 命令） |

## 典型使用场景

```bash
# 一站式：设置全局代理并同时配置 git、npm
gate set --global http://proxy:8080 --app git,npm --verify

# 为多个工具批量配置代理
gate app -n git,npm,yarn,pip -p http://proxy:8080

# 查看当前所有代理状态
gate info

# 保存为预设
gate preset --name office --save

# 一键应用预设
gate apply office

# 统一列出所有应用
gate list apps

# 交互式向导（新手推荐）
gate wizard
```

## 完整命令参考

请参阅 [docs/USAGE.md](docs/USAGE.md) 获取完整命令说明。

## 开发与测试

```bash
dotnet test src/Gate.Tests/Gate.Tests.csproj
```

更多文档：
- [docs/USAGE.md](docs/USAGE.md) — CLI 完整参考
- [docs/FEATURES.md](docs/FEATURES.md) — 功能与架构文档
- [docs/UI-WIREFRAME.md](docs/UI-WIREFRAME.md) — Unity GUI 界面布局
