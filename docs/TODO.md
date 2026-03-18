# Gate TODO

本文档记录已规划但尚未实现的功能，按优先级排列。

---

## P0 — 核心功能补全

### 1. MCP 服务器（Model Context Protocol）

> **状态**: 移出主线，待独立实现
>
> 原 `Gate.Core/Mcp/McpServer.cs` 中已有静态骨架，需重构为可注入实例类，
> 并实现标准 MCP stdio 协议（stdin/stdout JSON-RPC 2.0），
> 使 Claude Desktop、Cursor、Zed 等 MCP 客户端可直接配置使用。

**预期用法**：
```jsonc
// claude_desktop_config.json
{
  "mcpServers": {
    "gate": {
      "command": "gate",
      "args": ["mcp"]
    }
  }
}
```

**MCP 工具列表（规划）**：
| 工具名 | 描述 |
|--------|------|
| `set_proxy` | 设置全局代理 |
| `clear_proxy` | 清除全局代理 |
| `get_status` | 获取当前代理状态 |
| `set_tool_proxy` | 为指定工具设置代理 |
| `list_tools` | 列出所有已安装工具 |
| `save_preset` | 保存当前配置为预设 |
| `load_preset` | 加载预设 |
| `test_proxy` | 测试代理连通性 |

**实现要点**：
- 在 `McpServer.cs` 中实现标准 JSON-RPC 2.0 over stdio
- 工具列表由 `ToolRegistry` 动态驱动
- 支持 SSE 流式响应（`gate mcp --sse`）
- 添加 `gate mcp` 子命令入口
- 在 `Program.cs` 中注册：`root.AddCommand(McpCommand.Build());`

**参考**：
- [MCP 协议规范](https://spec.modelcontextprotocol.io/)
- [Cloudflare Agents SDK MCP 文档](https://developers.cloudflare.com/agents/)

---

## P1 — 功能增强

### 2. PAC 文件生成与托管

```bash
gate pac generate --proxy http://127.0.0.1:7890 --no-proxy "*.company.com,localhost"
gate pac serve --port 8889   # 启动临时 HTTP 服务托管 PAC 文件
```

### 3. 代理有效期 / 自动过期

```bash
gate set http://127.0.0.1:7890 --expires 8h   # 8小时后自动清除
gate set http://127.0.0.1:7890 --expires 1d   # 1天后自动清除
```

- 在 `config.json` 中记录过期时间戳
- `gate` 无参数启动时检查并自动清除过期代理
- 状态总览中显示剩余时间

### 4. 社区插件索引（在线安装）

当前 `gate plugin install` 只支持本地文件。
后续规划：

```bash
gate plugin list                    # 从社区索引拉取可用插件列表
gate plugin search ollama           # 搜索插件
gate plugin install clash           # 从社区仓库下载安装
gate plugin update --all            # 更新所有插件
```

社区索引仓库：`https://github.com/gate-community/gate-tools`
索引格式：`index.json`（参见 `PluginManager.PluginIndex`）

### 5. Shell Tab 补全安装向导

```bash
gate completion bash   > ~/.bash_completion.d/gate
gate completion zsh    > ~/.zsh/completions/_gate
gate completion fish   > ~/.config/fish/completions/gate.fish
gate completion pwsh   > $PROFILE.d/gate.ps1
```

目前 `gate completion` 已输出脚本内容，
后续加 `gate completion install` 自动写入对应路径。

### 6. `gate env --export-script`

```bash
gate env --export-script bash   # 输出 export HTTP_PROXY=... 脚本
gate env --export-script pwsh   # 输出 $env:HTTP_PROXY=...
```

---

## P2 — Unity GUI 增强

### 7. Unity UIToolkit 代理历史下拉

- 在 Unity 编辑器 GUI 的代理地址输入框下方添加历史下拉列表
- 数据源：`ProxyHistory.Load()`
- 最多显示 10 条历史记录

### 8. Unity GUI 插件管理面板

- 在 Unity 编辑器中添加 "插件" 标签页
- 显示已安装插件列表
- 支持从本地文件安装插件

### 9. Unity GUI `gate doctor` 面板

- 将 `gate doctor` 的检查结果嵌入 Unity 编辑器 UI
- 实时显示配置文件读写权限状态
- 高亮显示问题项

---

## P3 — 开源社区建设

### 10. GitHub 仓库治理文件

- [ ] `CONTRIBUTING.md` — 贡献指南
- [ ] `.github/ISSUE_TEMPLATE/bug_report.md`
- [ ] `.github/ISSUE_TEMPLATE/tool_request.md`
- [ ] `.github/ISSUE_TEMPLATE/feature_request.md`
- [ ] `.github/PULL_REQUEST_TEMPLATE.md`
- [ ] `.github/workflows/build.yml` — CI 构建三平台二进制
- [ ] `.github/workflows/release.yml` — 自动发布 Release

### 11. 社区工具仓库

建立独立仓库 `gate-community/gate-tools`：
```
gate-tools/
├── index.json
├── tools/
│   ├── clash/tool.json
│   ├── v2ray/tool.json
│   └── zed-editor/tool.json
└── .github/workflows/validate.yml
```

---

## 已完成 ✓

- [x] 跨平台声明式工具 JSON Schema（`ToolDescriptor` + `PlatformString`）
- [x] `DeclarativeToolConfigurator` 通用配置器
- [x] 内置工具批量迁移为 JSON 声明（40+ 工具）
- [x] `EmbeddedToolDescriptors` 嵌入资源加载器
- [x] `ToolRegistry` 重构：JSON 优先 + C# Configurator 兜底
- [x] `GatePaths` 统一存储路径（修复 Linux XDG 路径问题）
- [x] `ProfileManager` 使用 `GatePaths`
- [x] `ProxyHistory` 代理历史记录（最近 20 条）
- [x] `ConfigMigration` export-all / import-all
- [x] `PluginManager` 插件管理（安装/卸载/校验）
- [x] `OutputSettings` 全局输出模式（--json / --quiet / --no-color / --plain）
- [x] `ConsoleStyle` 集成 `OutputSettings`
- [x] `StatusPrinter` 集中打印逻辑（支持 JSON 输出）
- [x] `PresetCommands` 修复 preset load 同时恢复工具代理
- [x] `WizardCommand` 优化步骤 2 为编号菜单选择
- [x] `DoctorCommand` 增强诊断（配置文件权限 + 持久化 + shell hook）
- [x] `ShellHookCommand` gate install-shell-hook
- [x] `CompletionCommand` gate completion bash/zsh/fish/pwsh
- [x] `HistoryCommands` gate history / gate history clear
- [x] `PluginCommands` gate plugin list/install/remove/validate
- [x] `MigrationCommands` gate export-all / gate import-all
- [x] `Program.cs` 重构精简（集成所有新命令）
