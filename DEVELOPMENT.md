# Proxy-Tool 开发文档

## 项目概述

Proxy-Tool 是一款开源免费的代理配置管理工具，用于统一管理系统环境变量和各类开发工具的代理设置。

---

## 当前架构

```
┌─────────────────────────────────────────────────────────────┐
│  CLI 前端 (ProxyTool.CLI)                                   │
│  - System.CommandLine 命令解析                              │
│  - 交互式命令行界面                                         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  核心 DLL (ProxyTool.Core) - .NET Standard 2.0              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ Models      │  │ Managers    │  │ Configurators│         │
│  │ ProxyConfig │  │ EnvVarMgr   │  │ GitConfig   │         │
│  │ Profile     │  │ ProfileMgr  │  │ NpmConfig   │         │
│  │ ToolProxy   │  │ ProxyTester │  │ DockerConfig│         │
│  │ TestResult  │  │ ToolRegistry│  │ ...         │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

---

## 改进方案（1234步）

### 第一步：安全机制（防断网）

**目标：** 防止配置错误导致服务器断网

**实现内容：**
1. **配置备份系统**
   - 修改前自动备份当前配置
   - 支持按工具/环境变量分别备份
   - 备份存储在 `~/.proxy-tool/backups/`

2. **预览模式 (--dry-run)**
   ```bash
   proxy-tool env --http http://proxy:8080 --dry-run
   # 输出：将设置 HTTP_PROXY=http://proxy:8080
   #       当前值：HTTP_PROXY=(未设置)
   ```

3. **自动回滚机制**
   - 设置代理后启动计时器（默认30秒）
   - 用户需确认"配置有效"
   - 超时未确认自动恢复备份

### 第二步：原子操作与事务

**目标：** 确保多工具配置的一致性

**实现内容：**
1. **事务模式**
   ```csharp
   var tx = new ProxyTransaction();
   tx.Add(git.SetProxy(url));
   tx.Add(npm.SetProxy(url));
   tx.Add(docker.SetProxy(url));
   
   if (await tx.CommitAsync()) {
       Console.WriteLine("全部成功");
   } else {
       await tx.RollbackAsync();
       Console.WriteLine("已回滚");
   }
   ```

2. **部分失败处理**
   - 记录哪些工具成功/失败
   - 提供 `--continue-on-error` 选项
   - 失败报告生成

### 第三步：配置验证

**目标：** 设置前验证代理可用性

**实现内容：**
1. **连通性预检**
   ```bash
   proxy-tool env --http http://proxy:8080 --verify
   # 先测试代理可用，再写入配置
   ```

2. **配置格式验证**
   - 验证代理 URL 格式
   - 检查端口范围
   - 验证 IP 地址有效性

3. **冲突检测**
   - 检测工具配置与环境变量冲突
   - 提示用户选择优先级

### 第四步：扩展与完善

**目标：** 提升易用性和覆盖范围

**实现内容：**
1. **批量工具支持**
   - 目标：50+ 常用工具
   - 分类：版本控制、包管理器、IDE、AI工具等

2. **导入/导出**
   ```bash
   proxy-tool profile export --format json > my-proxy.json
   proxy-tool profile import my-proxy.json
   ```

3. **环境检测**
   - 自动检测当前 shell 类型
   - 提示生效命令（source ~/.bashrc 等）

4. **日志与审计**
   - 操作日志记录
   - 配置变更历史

---

## 开发阶段规划

### 第一阶段：核心功能完善（2-3周）

**目标：** 建立稳定的基础功能

**任务清单：**

| 优先级 | 任务 | 说明 |
|--------|------|------|
| 🔴 P0 | 配置备份系统 | 修改前自动备份，支持回滚 |
| 🔴 P0 | 预览模式 (--dry-run) | 只显示变更不执行 |
| 🔴 P0 | 修复编译问题 | 解决 .NET Standard 2.0 兼容性 |
| 🟡 P1 | 扩展工具配置器 | 添加 pip、conda、yarn、curl、wget |
| 🟡 P1 | 完善错误处理 | 添加详细的错误信息和恢复建议 |
| 🟢 P2 | 单元测试 | 为核心类添加 xUnit 测试 |

**交付物：**
- 可编译运行的 CLI 工具
- 支持 8-10 个常用工具
- 基础安全机制（备份+预览）

---

### 第二阶段：安全与验证（2周）

**目标：** 防止配置错误导致的问题

**任务清单：**

| 优先级 | 任务 | 说明 |
|--------|------|------|
| 🔴 P0 | 自动回滚机制 | 30秒超时自动恢复 |
| 🔴 P0 | 代理连通性预检 | 设置前测试代理可用 |
| 🟡 P1 | 事务模式 | 多工具配置原子操作 |
| 🟡 P1 | 配置格式验证 | URL、端口、IP 验证 |
| 🟢 P2 | 冲突检测 | 环境变量与工具配置冲突提示 |

**交付物：**
- 安全的配置流程
- 完整的验证机制
- 用户确认交互

---

### 第三阶段：功能扩展（3-4周）

**目标：** 覆盖更多场景和工具

**任务清单：**

| 优先级 | 任务 | 说明 |
|--------|------|------|
| 🔴 P0 | 系统环境变量支持 | Windows UAC / Linux sudo |
| 🟡 P1 | 批量工具配置 | 一键配置多个工具 |
| 🟡 P1 | 导入/导出功能 | JSON/YAML 格式配置 |
| 🟡 P1 | 扩展工具列表 | 达到 30+ 工具支持 |
| 🟢 P2 | Shell 环境检测 | 自动提示生效命令 |
| 🟢 P2 | 配置模板 | 提供常用模板（公司/家庭/机场） |

**工具扩展列表：**

**版本控制（5个）：**
- ✅ Git
- SVN
- Mercurial (hg)
- repo
- gh (GitHub CLI)

**包管理器（8个）：**
- ✅ npm
- ✅ yarn
- pnpm
- pip
- conda
- poetry
- gem (Ruby)
- composer (PHP)

**系统包管理器（6个）：**
- apt
- yum
- dnf
- zypper
- pacman
- portage

**容器工具（4个）：**
- ✅ Docker
- Podman
- kubectl
- helm

**IDE/编辑器（5个）：**
- VS Code
- Cursor
- IntelliJ IDEA
- Android Studio
- Vim/Neovim

**AI工具链（8个）：**
- Cline
- Continue
- Windsurf
- Claude Code
- OpenCode
- Blackbox AI
- OpenAI SDK
- Anthropic SDK

**交付物：**
- 支持 30+ 工具
- 系统级环境变量管理
- 配置导入导出

---

### 第四阶段：高级功能（3-4周）

**目标：** 企业级功能和用户体验

**任务清单：**

| 优先级 | 任务 | 说明 |
|--------|------|------|
| 🔴 P0 | 日志与审计 | 操作记录、变更历史 |
| 🟡 P1 | 配置集加密 | 敏感信息（密码）加密存储 |
| 🟡 P1 | 远程配置同步 | 支持从 URL 加载配置 |
| 🟡 P1 | 自动更新检查 | 版本检测、自动下载 |
| 🟢 P2 | Unity GUI 开发 | 图形界面（可选） |
| 🟢 P2 | MCP 服务器 | AI 工具集成 |

**MCP 服务器功能：**
```json
{
  "tools": [
    {
      "name": "set_proxy",
      "description": "设置系统代理",
      "parameters": {
        "http_proxy": "string",
        "https_proxy": "string"
      }
    },
    {
      "name": "test_proxy",
      "description": "测试代理连通性"
    }
  ]
}
```

**交付物：**
- 完整的日志系统
- MCP 服务器
- 可选的 GUI 界面
- 自动更新机制

---

## 技术债务与优化

### 待解决问题

1. **.NET Standard 2.0 兼容性**
   - 部分 API 需要 polyfill
   - 建议使用 .NET 6+ 作为最低版本

2. **异步处理**
   - 当前 CLI 是同步阻塞的
   - 建议改为 async/await 模式

3. **配置文件解析**
   - Git 配置解析过于简单
   - 建议使用专门库（如 LibGit2Sharp）

4. **权限管理**
   - Windows UAC 提权未实现
   - Linux sudo 需要交互式处理

---

## 发布计划

| 版本 | 内容 | 时间 |
|------|------|------|
| v0.1.0 | 第一阶段核心功能 | 第3周末 |
| v0.2.0 | 安全与验证 | 第5周末 |
| v0.3.0 | 功能扩展（30+工具） | 第9周末 |
| v1.0.0 | 高级功能 + GUI | 第13周末 |

---

## 附录

### 常用代理配置示例

```bash
# 公司代理
proxy-tool profile save company --http http://proxy.company.com:8080

# 机场代理
proxy-tool profile save airport --http http://127.0.0.1:7890

# 清除所有
proxy-tool env --clear
proxy-tool tool --all --clear
```

### 配置文件格式

```json
{
  "name": "company",
  "envVars": {
    "httpProxy": "http://proxy.company.com:8080",
    "httpsProxy": "http://proxy.company.com:8080",
    "noProxy": "localhost,127.0.0.1,.company.com"
  },
  "toolConfigs": {
    "git": {
      "httpProxy": "http://proxy.company.com:8080"
    },
    "npm": {
      "httpProxy": "http://proxy.company.com:8080"
    }
  }
}
```

---

## 测试

### 运行测试

```bash
cd src
dotnet test ProxyTool.Tests/ProxyTool.Tests.csproj
```

默认运行**不修改**真实环境变量与代理配置：参数组合测试仅校验 `ConfigValidator`、`EnvVarManager.ParseProxyUrl`、`ProxyConfig` 等逻辑；会写入工具配置的 `SetProxyAll` / `ClearProxyAll` 在默认情况下只做模拟校验（断言注册表非空），不执行实际写入。

### 集成测试（会修改真实配置）

若需执行会真实调用 `SetProxyAll` / `ClearProxyAll` 的集成测试（会写入已安装工具的配置文件），请设置环境变量后运行：

```bash
# Windows PowerShell
$env:PROXYTOOL_RUN_INTEGRATION="1"; dotnet test ProxyTool.Tests/ProxyTool.Tests.csproj

# Linux/macOS
PROXYTOOL_RUN_INTEGRATION=1 dotnet test ProxyTool.Tests/ProxyTool.Tests.csproj
```

### 测试结构

- **ParameterCombination**：多参数组合与边界测试（纯逻辑，无副作用）
  - `ConfigValidatorParameterTests`：代理 URL、端口、IP、完整配置校验
  - `EnvVarManagerParameterTests`：`ParseProxyUrl` 各种 URL 与默认端口
  - `ProxyConfigModelTests`：`ProxyConfig` 的 `IsEmpty`、`ToString`
- **Managers**：`ConfigValidator`、`EnvVarManager`、`ToolRegistry` 等
- **Configurators**：Git/Npm/Wget 等配置器（使用临时目录，不写真实配置）

---

*文档版本: 1.0*
*最后更新: 2026-03-09*