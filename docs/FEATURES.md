# ProxyTool 功能文档

## 1. 产品概述

ProxyTool 是一款跨平台的代理配置管理工具，用于统一管理开发环境中各类工具的 HTTP/HTTPS 代理设置。支持 130+ 种开发工具、包管理器、AI 应用和 IDE 的代理配置，帮助开发者在需要代理访问外网的环境中快速切换和同步代理设置。

### 1.1 核心价值

- **统一管理**：一处配置，多工具生效
- **快速切换**：配置集（Profile）一键切换不同代理场景
- **批量操作**：支持批量设置/清除所有已安装工具的代理
- **代理验证**：设置前可测试代理连通性
- **跨平台**：支持 Windows、Linux、macOS

### 1.2 技术架构

```
┌─────────────────────────────────────────────────────────┐
│                    ProxyTool 架构                        │
├─────────────────────────────────────────────────────────┤
│  CLI (proxy-tool)    │    API (HTTP REST)               │
├─────────────────────┼──────────────────────────────────┤
│  env / tool /       │  /api/v1/proxy                    │
│  profile / test     │  /api/v1/tools                    │
│                     │  /api/v1/profiles                 │
├─────────────────────┴──────────────────────────────────┤
│              ProxyTool.Core (核心层)                     │
│  EnvVarManager │ ToolRegistry │ ProfileManager          │
│  ConfigValidator │ ProxyTester │ ConfigImportExport     │
├─────────────────────────────────────────────────────────┤
│              130+ ToolConfigurator 实现                  │
│  Git/Npm/Go/Docker/Cursor/Ollama/...                    │
└─────────────────────────────────────────────────────────┘
```

---

## 2. 功能模块详解

### 2.1 环境变量管理 (env)

管理当前进程的 HTTP_PROXY、HTTPS_PROXY、NO_PROXY 等环境变量。

| 功能 | 说明 |
|------|------|
| 设置代理 | 为当前进程设置 HTTP/HTTPS 代理，支持 `--verify` 设置前测试 |
| 清除代理 | 清除当前进程的代理环境变量 |
| 查询状态 | 显示当前用户级环境变量中的代理配置 |
| 代理验证 | 支持 http/https/socks4/socks5 协议，验证主机名和端口范围 |

**支持的代理格式**：
- `http://host:port`
- `https://host:port`
- `socks5://host:port`
- `host:port`（自动补全为 http://）

### 2.2 工具代理配置 (tool)

为各类开发工具配置代理，自动检测工具是否已安装，并写入对应配置文件。

| 功能 | 说明 |
|------|------|
| 列出工具 | 按分类列出所有支持的工具及安装/配置状态 |
| 设置代理 | 为指定工具设置代理（写入其配置文件） |
| 清除代理 | 清除指定工具的代理配置 |
| 查询配置 | 查看工具当前代理配置 |

**工具检测逻辑**：
- 通过 PATH 环境变量检测可执行文件
- 部分工具通过配置文件路径存在性判断（如 Wget）
- 未安装的工具会提示，不执行配置操作

### 2.3 配置集管理 (profile)

将当前环境变量 + 所有工具代理配置保存为命名配置集，支持一键加载、切换。

| 功能 | 说明 |
|------|------|
| 保存配置集 | 将当前环境变量和所有工具配置保存为命名 Profile |
| 加载配置集 | 加载指定 Profile，应用环境变量到当前进程 |
| 删除配置集 | 删除已保存的配置集 |
| 默认配置集 | 设置/获取默认配置集 |
| 导入导出 | 支持 JSON/YAML/ENV 格式导入导出 |

**配置集存储**：本地 JSON 文件，路径因平台而异。

### 2.4 代理测试 (test)

测试代理服务器的连通性和响应时间。

| 功能 | 说明 |
|------|------|
| 测试指定代理 | 使用 `--proxy` 指定代理地址进行测试 |
| 测试当前代理 | 不指定时使用环境变量中的代理进行测试 |
| 自定义测试 URL | 使用 `--url` 指定测试目标（默认 httpbin.org） |
| 超时配置 | 支持配置超时时间 |

**返回信息**：成功/失败、响应时间(ms)、错误信息。

---

## 3. 支持的工具分类

### 3.1 版本控制
Git、Svn、Mercurial、GitHub CLI、GitLab CLI、Bitbucket CLI

### 3.2 包管理器
Npm、Pnpm、Yarn、Pip、Conda、Gem、Composer、Cargo、Pub、NuGet、CocoaPods、SwiftPM

### 3.3 构建工具
Maven、Gradle

### 3.4 容器与编排
Docker、Docker Compose、Docker Buildx、Helm、Helmfile、Kind、Minikube、K3s、Podman、Kubectl、Skaffold、Tilt、Kaniko、BuildKit

### 3.5 CI/CD
Jenkins、GitHub Actions、GitLab CI、ArgoCD、Flux、Tekton、CircleCI、Travis CI、Drone CI

### 3.6 基础设施即代码
Ansible、Terraform、Terraform CLI、Vault、Packer、Vagrant

### 3.7 服务网格/云原生
Consul、Nomad、Istio、Crossplane、Kubeflow

### 3.8 网络工具
Wget、Curl、Tailscale、Cloudflared

### 3.9 编程语言
Go、R、Julia

### 3.10 AI 云服务
OpenAI、Anthropic、Azure AI、Google AI、Mistral AI、Cohere、Groq、Replicate、OpenRouter、Vertex AI、AWS Bedrock、Hugging Face、Ollama、Claude CLI 等 50+ 服务

### 3.11 AI 编程助手
Cursor、Windsurf、OpenCode、VS Code、VS Code Insiders、Cline、Goose、Continue、Codeium、Tabby、Aider、Sourcegraph Cody、Augment、BoltNew

### 3.12 本地 LLM
GPT4All、Jan、LlamaCpp、VLLM、Text Generation WebUI、LM Studio

### 3.13 云 CLI
AWS CLI、Gcloud

### 3.14 其他
Homebrew、PowerShell、FTP

> 完整工具列表见 [SUPPORTED_TOOLS.md](./SUPPORTED_TOOLS.md)

---

## 4. 数据模型

### 4.1 ProxyConfig
```csharp
{
  HttpProxy: string?    // HTTP 代理地址
  HttpsProxy: string?   // HTTPS 代理地址
  FtpProxy: string?     // FTP 代理地址
  SocksProxy: string?   // SOCKS 代理地址
  NoProxy: string?      // 排除代理的地址列表
}
```

### 4.2 Profile（配置集）
```csharp
{
  Name: string
  Description: string
  CreatedAt: DateTime
  UpdatedAt: DateTime
  EnvVars: ProxyConfig           // 环境变量配置
  ToolConfigs: Dict<string, ProxyConfig>  // 各工具配置
}
```

### 4.3 ProxyTestResult
```csharp
{
  Success: bool
  ResponseTimeMs: int
  ErrorMessage: string?
  TestUrl: string
}
```

---

## 5. 配置验证规则

- **协议**：支持 http、https、socks4、socks5
- **主机名**：需为有效 IP、localhost 或包含点的域名
- **端口**：1–65535
- **无效示例**：`invalid`、`not-a-valid-url` 等会被拒绝

---

## 6. 部署形态

| 形态 | 说明 |
|------|------|
| CLI 单文件 | 自包含或框架依赖，单可执行文件 |
| API 服务 | ASP.NET Core Web API，默认端口 5000 |
| 多平台 | win-x64、linux-x64、osx-x64 |

---

## 7. 扩展能力

- **插件系统**：支持通过 ProxyTool.PluginCore 开发自定义工具配置器
- **工具注册**：`ToolRegistry.Register()` 动态注册新配置器
- **配置导入导出**：支持 JSON、YAML、ENV 格式

详见 [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md)
