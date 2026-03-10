# Gate 功能文档

## 1. 产品概述

Gate 是一款跨平台代理配置管理工具，统一管理开发环境中各类工具的 HTTP/HTTPS 代理。支持 130+ 种工具，提供 CLI 和 Unity GUI 两种使用方式。

### 1.1 核心价值

- **统一管理**：一处配置，多工具生效
- **快速切换**：预设（Preset）一键切换代理场景
- **批量操作**：逗号分隔同时配置多个应用
- **代理验证**：设置前测试连通性（`--verify`）
- **交互向导**：`gate wizard` 四步引导新手完成首次配置
- **跨平台**：Windows / Linux / macOS
- **Unity GUI**：UIToolkit 图形界面，嵌入 Unity 编辑器

### 1.2 技术架构

```
+--------------------------------------------------+
|  CLI (gate.exe / gate)                           |
|  global/env  app/tool  preset/profile            |
|  info/status/show  test/check                    |
|  set  apply  list  wizard                        |
+--------------------------------------------------+
|  Gate.Core (netstandard2.0 · Unity DLL)          |
|  EnvVarManager   ToolRegistry                    |
|  ProfileManager  ConfigValidator                 |
|  ProxyTester     ConfigImportExport              |
+--------------------------------------------------+
|  130+ ToolConfigurator 实现                       |
|  Git/Npm/Go/Docker/Cursor/Ollama/OpenAI/...      |
+--------------------------------------------------+
|  Unity GUI (UIToolkit)                           |
|  GatePanelController  GlobalPanelController      |
|  AppPanelController   PresetPanelController      |
|  StatusPanelController  TestPanelController      |
+--------------------------------------------------+
```

---

## 2. CLI 命令功能

### 2.1 global / env — 全局代理

| 功能 | 说明 |
|------|------|
| 设置代理 | `-p` 同时设置 HTTP/HTTPS；`-H`/`-S` 分别设置 |
| 清除代理 | `-c` / `--clear` |
| 查询状态 | 无参数显示当前配置 |
| 代理验证 | `-v` / `--verify` 设置前测试 |
| 排除列表 | `--no-proxy` 设置 NO_PROXY |

### 2.2 app / tool — 应用代理（支持批量）

| 功能 | 说明 |
|------|------|
| 列出应用 | `-l` 按分类列出所有支持工具及安装状态 |
| 批量设置 | `-n git,npm,yarn -p http://...` |
| 清除代理 | `-n git -c` |
| 查询配置 | `-n git`（无代理参数则显示当前配置） |

### 2.3 preset / profile — 预设配置集

| 功能 | 说明 |
|------|------|
| 列出预设 | `--list` 或无参数 |
| 保存 | `--name office --save` |
| 加载 | `--name office --load` |
| 删除 | `--name old --delete` |
| 设为默认 | `--name office --set-default` |

### 2.4 info / status / show — 状态总览

一次性查看：全局代理 + 已配置工具（按分类）+ 预设列表。

### 2.5 test / check — 代理连通性测试

| 功能 | 说明 |
|------|------|
| 测试指定代理 | `-p http://...` |
| 测试当前代理 | 无参数，使用环境变量 |
| 自定义 URL | `--url https://...` |

### 2.6 set — 一站式配置

```bash
gate set --global http://proxy:8080 --app git,npm --verify
```

### 2.7 apply — 直接应用预设

```bash
gate apply office   # 等价于 gate preset --name office --load
```

### 2.8 list — 统一列出资源

```bash
gate list           # 预设（默认）
gate list apps      # 应用
gate list presets   # 预设
```

### 2.9 wizard — 交互式向导

四步引导：1 输入代理地址  2 选择应用  3 设置 NO_PROXY  4 保存预设。

---

## 3. 命令别名

| 主命令 | 别名 | 说明 |
|--------|------|------|
| `global` | `env` | 原命令继续可用 |
| `app` | `tool` | 原命令继续可用 |
| `preset` | `profile` | 原命令继续可用 |
| `info` | `status`, `show` | 原命令继续可用 |
| `test` | `check` | 新增别名 |

---

## 4. 支持的工具分类

- **版本控制**：Git, Svn, Mercurial, GitHub CLI, GitLab CLI
- **包管理器**：Npm, Pnpm, Yarn, Pip, Conda, Gem, Composer, Cargo, Pub, NuGet
- **构建工具**：Maven, Gradle
- **容器编排**：Docker, Docker Compose, Helm, Kind, Minikube, Kubectl, Podman
- **CI/CD**：Jenkins, GitHub Actions, GitLab CI, ArgoCD, CircleCI
- **基础设施**：Ansible, Terraform, Vault, Packer
- **网络工具**：Wget, Curl, Tailscale, Cloudflared
- **AI 云服务（50+）**：OpenAI, Anthropic, Azure AI, Google AI, Mistral, Groq, Ollama...
- **AI IDE**：Cursor, Windsurf, VS Code, Cline, Goose, Aider, Continue, Codeium
- **本地 LLM**：GPT4All, Jan, LlamaCpp, VLLM, LM Studio
- **云 CLI**：AWS CLI, Gcloud

---

## 5. 数据模型

```csharp
ProxyConfig     { HttpProxy, HttpsProxy, FtpProxy, SocksProxy, NoProxy }
Profile         { Name, Description, CreatedAt, UpdatedAt, EnvVars, ToolConfigs }
ProxyTestResult { Success, ResponseTimeMs, ErrorMessage, TestUrl }
```

---

## 6. 配置验证规则

- **协议**：http, https, socks4, socks5
- **主机名**：有效 IP、localhost 或包含点的域名
- **端口**：1–65535

---

## 7. 部署形态

| 形态 | 说明 |
|------|------|
| CLI 单文件 | 自包含可执行文件，win-x64 / linux-x64 / osx-x64 |
| Unity DLL | Gate.Core.dll 发布到 `Assets/Plugins/AIGate/` |
| Unity GUI | UIToolkit 图形界面，UXML + USS + C# 控制器 |

---

## 8. 扩展能力

- **工具注册**：`ToolRegistry.Register()` 动态注册自定义工具
- **配置导入导出**：JSON / ENV 格式（ConfigImportExport）
- **备份回滚**：BackupManager / AutoRollbackManager
- **审计日志**：AuditLogger 记录所有代理变更
