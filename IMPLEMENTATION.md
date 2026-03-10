# Proxy-Tool 分阶段开发实施文档

## 项目总览

- **项目名称**: Proxy-Tool
- **开发周期**: 10-13周（约3个月）
- **技术栈**: C# (.NET Standard 2.0 / .NET 8.0)
- **目标**: 跨平台代理配置管理工具

---

## 第一阶段：核心功能完善（第1-3周）

### 目标
建立稳定的基础功能，实现核心架构，支持8-10个常用工具。

### 1.1 基础架构修复（Week 1）

#### Day 1-2: 项目初始化与编译修复
**实施内容:**
```
□ 创建项目结构
  - ProxyTool.Core (.NET Standard 2.0)
  - ProxyTool.CLI (.NET 8.0)
  - ProxyTool.Tests (xUnit)

□ 修复编译错误
  - 添加缺失的 using 语句
  - 解决 .NET Standard 2.0 兼容性问题
  - 确保所有文件能编译通过

□ 配置 NuGet 包
  - System.CommandLine (CLI)
  - System.Text.Json (Core)
  - xUnit (Tests)
```

**输出文件:**
- `ProxyTool.Core.csproj`
- `ProxyTool.CLI.csproj`
- `ProxyTool.Tests.csproj`

#### Day 3-4: Models 层完善
**实施内容:**
```
□ ProxyConfig 类
  - HttpProxy, HttpsProxy, FtpProxy, SocksProxy, NoProxy 属性
  - IsEmpty 检查
  - ToString() 格式化输出
  - 验证方法 Validate()

□ Profile 类
  - Name, CreatedAt, UpdatedAt
  - EnvVars (ProxyConfig)
  - ToolConfigs (Dictionary<string, ProxyConfig>)
  - 序列化/反序列化支持

□ ToolProxyConfig 类
  - ToolName, Category, Status
  - ConfigPath, CustomProxy
  - InheritedFrom

□ ProxyTestResult 类
  - Success, ResponseTimeMs
  - ErrorMessage, TestUrl
```

**输出文件:**
- `Models/ProxyModels.cs`

#### Day 5-7: Managers 层重构
**实施内容:**
```
□ EnvVarManager
  - GetProxyConfig(EnvLevel) - 读取环境变量
  - SetProxyForCurrentProcess(ProxyConfig) - 设置当前进程
  - ParseProxyUrl(string) - 解析代理地址
  
□ ProfileManager
  - Save(Profile) - 保存配置集
  - Load(string) - 加载配置集
  - List() - 列出所有配置集
  - Delete(string) - 删除配置集
  - GetDefaultProfile() / SetDefaultProfile()

□ ProxyTester
  - TestHttpProxyAsync() - 测试HTTP代理
  - TestSocksProxyAsync() - 测试SOCKS代理
  - TestProxyAsync() - 自动检测并测试

□ ToolRegistry
  - GetAllTools() - 获取所有工具
  - GetByCategory(string) - 按分类获取
  - GetByName(string) - 按名称查找
  - Register(ToolConfiguratorBase) - 注册新工具
```

**输出文件:**
- `Managers/EnvVarManager.cs`
- `Managers/ProfileManager.cs`
- `Managers/ProxyTester.cs`
- `Managers/ToolRegistry.cs`

### 1.2 安全机制（Week 2）

#### Day 8-9: 配置备份系统
**实施内容:**
```
□ 创建 ConfigBackup 类
  namespace ProxyTool.Managers
  {
      public static class ConfigBackup
      {
          // 备份目录: ~/.proxy-tool/backups/
          private static readonly string BackupDir = Path.Combine(
              Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
              ".proxy-tool", "backups"
          );
          
          // 创建备份
          public static string CreateBackup(string toolName, string configContent)
          
          // 恢复备份
          public static bool RestoreBackup(string backupId)
          
          // 列出备份
          public static List<BackupInfo> ListBackups()
          
          // 清理旧备份（保留最近30天）
          public static void CleanupOldBackups()
      }
      
      public class BackupInfo
      {
          public string Id { get; set; }
          public string ToolName { get; set; }
          public DateTime CreatedAt { get; set; }
          public string Content { get; set; }
      }
  }

□ 备份存储格式
  {
    "id": "uuid",
    "toolName": "git",
    "createdAt": "2026-03-07T10:00:00Z",
    "originalContent": "...",
    "metadata": {
      "filePath": "~/.gitconfig",
      "fileHash": "sha256"
    }
  }
```

**输出文件:**
- `Managers/ConfigBackup.cs`

#### Day 10-11: 备份存储实现
**实施内容:**
```
□ 目录结构创建
  ~/.proxy-tool/
  ├── backups/
  │   ├── git_20260307_100000.json
  │   ├── npm_20260307_100005.json
  │   └── ...
  ├── profiles/
  │   ├── company.json
  │   └── home.json
  └── config.json

□ 文件操作
  - 自动创建目录
  - JSON 序列化/反序列化
  - 文件锁（防止并发写入）
  - 错误处理

□ 备份元数据
  - 时间戳
  - 原始文件路径
  - 文件哈希（完整性校验）
```

#### Day 12-14: 预览模式 (--dry-run)
**实施内容:**
```
□ 修改所有 SetProxy 方法
  - 添加 bool dryRun = false 参数
  - dryRun=true 时只返回变更描述，不写入文件

□ CLI 参数支持
  proxy-tool env --http http://proxy:8080 --dry-run
  proxy-tool tool --name git --proxy http://proxy:8080 --dry-run

□ 输出格式
  [DRY-RUN] 将执行以下变更：
  ┌─────────┬────────────────────┬────────────────────┐
  │ 目标    │ 当前值             │ 新值               │
  ├─────────┼────────────────────┼────────────────────┤
  │ HTTP_PROXY │ (未设置)        │ http://proxy:8080  │
  │ Git     │ http://old:8080    │ http://proxy:8080  │
  └─────────┴────────────────────┴────────────────────┘
  使用 --apply 确认执行

□ 实现 IChangePreview 接口
  public interface IChangePreview
  {
      List<ChangeItem> PreviewChanges();
  }
```

**输出文件:**
- `Models/ChangePreview.cs`
- 更新所有 Configurator 类

### 1.3 工具扩展+测试（Week 3）

#### Day 15-17: 扩展工具配置器
**实施内容:**
```
□ 创建 PipConfigurator (Python pip)
  - 配置文件: ~/.config/pip/pip.conf (Linux/Mac)
  - 配置文件: %APPDATA%\pip\pip.ini (Windows)
  - 格式: INI
  - 配置项: [global] proxy = http://...

□ 创建 CondaConfigurator (Anaconda)
  - 配置文件: ~/.condarc
  - 格式: YAML
  - 配置项: proxy_servers: http: ...

□ 创建 YarnConfigurator (Node.js)
  - 配置文件: ~/.yarnrc
  - 格式: YAML/INI混合
  - 配置项: proxy: "..."

□ 创建 CurlConfigurator
  - 配置文件: ~/.curlrc
  - 格式: 命令行参数
  - 配置项: proxy = "..."

□ 创建 WgetConfigurator
  - 配置文件: ~/.wgetrc
  - 格式: 配置项
  - 配置项: http_proxy = ...
```

**输出文件:**
- `Configurators/PipConfigurator.cs`
- `Configurators/CondaConfigurator.cs`
- `Configurators/YarnConfigurator.cs`
- `Configurators/CurlConfigurator.cs`
- `Configurators/WgetConfigurator.cs`

#### Day 18-19: 错误处理优化
**实施内容:**
```
□ 定义错误类型枚举
  public enum ProxyToolError
  {
      ToolNotInstalled,
      ConfigFileNotFound,
      ConfigFileLocked,
      PermissionDenied,
      InvalidProxyUrl,
      BackupFailed,
      RestoreFailed,
      NetworkError
  }

□ 创建 ProxyToolException 类
  public class ProxyToolException : Exception
  {
      public ProxyToolError ErrorCode { get; }
      public string ToolName { get; }
      public string SuggestedFix { get; }
  }

□ 错误恢复建议
  - "PermissionDenied" → "请使用 sudo 或管理员权限运行"
  - "ConfigFileLocked" → "请关闭占用该文件的程序"
  - "ToolNotInstalled" → "请先安装工具: apt install git"
```

**输出文件:**
- `Exceptions/ProxyToolException.cs`

#### Day 20-21: 单元测试
**实施内容:**
```
□ 测试项目结构
  ProxyTool.Tests/
  ├── Managers/
  │   ├── EnvVarManagerTests.cs
  │   ├── ProfileManagerTests.cs
  │   └── ProxyTesterTests.cs
  ├── Configurators/
  │   ├── GitConfiguratorTests.cs
  │   └── NpmConfiguratorTests.cs
  └── Models/
      └── ProxyModelsTests.cs

□ 核心测试用例
  - EnvVarManager: 读取/设置环境变量
  - ProfileManager: 保存/加载/删除配置集
  - ProxyTester: 测试代理连通性（Mock）
  - GitConfigurator: 解析/修改 .gitconfig
  - 覆盖率目标: >60%
```

**输出文件:**
- `ProxyTool.Tests/` 完整测试项目

### 第一阶段交付物

```
proxy-tool/
├── src/
│   ├── ProxyTool.Core/
│   │   ├── Models/
│   │   ├── Managers/
│   │   ├── Configurators/
│   │   └── Exceptions/
│   ├── ProxyTool.CLI/
│   │   └── Program.cs
│   └── ProxyTool.Tests/
├── DEVELOPMENT.md
└── README.md
```

**功能清单:**
- ✅ 支持 8 个工具: Git, npm, Docker, pip, conda, yarn, curl, wget
- ✅ 配置备份系统
- ✅ 预览模式 (--dry-run)
- ✅ 配置集管理 (Profile)
- ✅ 代理测试
- ✅ 单元测试 (>60% 覆盖率)

---

## 第二阶段：安全与验证（第4-5周）

### 目标
防止配置错误导致的问题，实现安全的配置流程。

### 2.1 自动回滚机制（Week 4）

#### Day 22-23: 自动回滚核心
**实施内容:**
```
□ 创建 AutoRollback 类
  public static class AutoRollback
  {
      // 超时时间（默认30秒）
      private const int DefaultTimeoutSeconds = 30;
      
      // 执行带自动回滚的操作
      public static async Task<bool> ExecuteWithRollbackAsync(
          Func<Task<bool>> operation,
          Func<Task> rollback,
          int timeoutSeconds = DefaultTimeoutSeconds
      )
      
      // 用户确认
      public static async Task<bool> WaitForUserConfirmationAsync(
          int timeoutSeconds,
          CancellationToken ct
      )
  }

□ 实现逻辑
  1. 执行操作前创建备份
  2. 执行配置变更
  3. 启动倒计时（30秒）
  4. 等待用户输入 "y" 确认
  5. 超时或用户输入 "n" 则自动回滚

□ CLI 交互
  代理已设置，请验证网络连接...
  [30] 秒后自动回滚... 输入 'y' 确认保留，'n' 立即回滚: 
  
  倒计时显示: [29] [28] [27] ...
```

**输出文件:**
- `Managers/AutoRollback.cs`

#### Day 24-25: 用户确认交互
**实施内容:**
```
□ 交互式提示
  - 倒计时显示（实时更新）
  - 支持 'y'/'n'/'q' 输入
  - Ctrl+C 触发回滚

□ 配置选项
  proxy-tool env --http ... --timeout 60  # 自定义超时
  proxy-tool env --http ... --no-confirm  # 禁用确认（危险）

□ 状态记录
  - 记录用户选择（用于审计）
  - 回滚原因记录
```

#### Day 26-28: 代理连通性预检
**实施内容:**
```
□ 修改 CLI 参数
  proxy-tool env --http http://proxy:8080 --verify
  proxy-tool tool --name git --proxy ... --verify

□ 预检流程
  1. 解析代理地址
  2. 尝试连接代理服务器
  3. 通过代理访问测试 URL
  4. 显示结果

□ 输出格式
  测试代理: http://proxy:8080
  测试目标: http://www.google.com
  
  ✅ 连接成功!
     响应时间: 125ms
     状态码: 200
  
  或
  
  ❌ 连接失败
     错误: Connection refused
     建议: 检查代理地址和端口
```

### 2.2 事务模式（Week 5）

#### Day 29-30: 事务系统
**实施内容:**
```
□ 创建 ProxyTransaction 类
  public class ProxyTransaction
  {
      private List<TransactionItem> _items = new();
      
      // 添加操作
      public void Add(Func<Task<bool>> operation, Func<Task> rollback)
      
      // 提交事务
      public async Task<TransactionResult> CommitAsync()
      
      // 回滚事务
      public async Task RollbackAsync()
  }

□ 事务项
  public class TransactionItem
  {
      public string ToolName { get; set; }
      public Func<Task<bool>> Operation { get; set; }
      public Func<Task> Rollback { get; set; }
      public bool Executed { get; set; }
      public bool Success { get; set; }
  }

□ 使用示例
  var tx = new ProxyTransaction();
  
  tx.Add(
      async () => await git.SetProxy(url),
      async () => await git.ClearProxy()
  );
  
  tx.Add(
      async () => await npm.SetProxy(url),
      async () => await npm.ClearProxy()
  );
  
  var result = await tx.CommitAsync();
  
  if (!result.Success) {
      Console.WriteLine($"失败: {result.FailedTool}");
      await tx.RollbackAsync();
  }
```

**输出文件:**
- `Managers/ProxyTransaction.cs`

#### Day 31-32: 部分失败处理
**实施内容:**
```
□ 失败报告
  public class TransactionResult
  {
      public bool Success { get; set; }
      public List<ToolResult> Results { get; set; }
      public string FailedTool { get; set; }
      public string ErrorMessage { get; set; }
  }

□ CLI 参数
  proxy-tool tool --all --proxy ... --continue-on-error
  
□ 输出格式
  批量配置结果:
  ┌─────────┬─────────┬────────────────────┐
  │ 工具    │ 状态    │ 信息               │
  ├─────────┼─────────┼────────────────────┤
  │ Git     │ ✅ 成功 │                    │
  │ npm     │ ✅ 成功 │                    │
  │ Docker  │ ❌ 失败 │ 权限不足           │
  │ pip     │ ⏭️ 跳过 │ 未安装             │
  └─────────┴─────────┴────────────────────┘
  
  2/4 成功，1 失败，1 跳过
```

#### Day 33-35: 配置格式验证
**实施内容:**
```
□ 创建 ConfigValidator 类
  public static class ConfigValidator
  {
      // 验证代理 URL
      public static ValidationResult ValidateProxyUrl(string url)
      
      // 验证端口
      public static ValidationResult ValidatePort(int port)
      
      // 验证 IP 地址
      public static ValidationResult ValidateIpAddress(string ip)
      
      // 完整配置验证
      public static ValidationResult ValidateProxyConfig(ProxyConfig config)
  }

□ 验证规则
  - URL 格式: http://host:port 或 socks5://host:port
  - 端口范围: 1-65535
  - IP 格式: IPv4 / IPv6
  - 排除列表: 逗号分隔的域名/IP

□ 错误提示
  ❌ 无效的代理地址: "abc"
     正确格式: http://host:port 或 socks5://host:port
  
  ❌ 端口超出范围: 99999
     有效范围: 1-65535
```

**输出文件:**
- `Managers/ConfigValidator.cs`

### 第二阶段交付物

**功能清单:**
- ✅ 自动回滚机制（30秒超时）
- ✅ 用户确认交互
- ✅ 代理连通性预检 (--verify)
- ✅ 事务模式（原子操作）
- ✅ 部分失败处理
- ✅ 配置格式验证

---

## 第三阶段：功能扩展（第6-9周）

### 目标
扩展工具支持到30+，实现系统级环境变量管理。

### 3.1 系统环境变量（Week 6）

#### Day 36-37: Windows UAC 提权
**实施内容:**
```
□ 应用程序清单 (app.manifest)
  <?xml version="1.0" encoding="utf-8"?>
  <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
      <security>
        <requestedPrivileges>
          <requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>
        </requestedPrivileges>
      </security>
    </trustInfo>
  </assembly>

□ 注册表操作
  - HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
  - 读写系统环境变量
  - 广播 WM_SETTINGCHANGE 消息

□ 检测管理员权限
  public static bool IsAdministrator()
  {
      var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }
```

#### Day 38-39: Linux sudo 支持
**实施内容:**
```
□ 检测 root 权限
  public static bool IsRoot() => getuid() == 0;
  
  [DllImport("libc")]
  private static extern uint getuid();

□ sudo 提示
  if (!IsRoot()) {
      Console.WriteLine("修改系统环境变量需要 root 权限");
      Console.WriteLine("请使用: sudo proxy-tool env --system ...");
      return false;
  }

□ /etc/environment 操作
  - 读取/写入 /etc/environment
  - 备份原文件
  - 格式: KEY="value"
```

#### Day 40-42: 系统级环境变量实现
**实施内容:**
```
□ 扩展 EnvVarManager
  public static class EnvVarManager
  {
      // 设置系统环境变量（需要提权）
      public static bool SetSystemProxy(ProxyConfig config)
      
      // 设置用户环境变量
      public static bool SetUserProxy(ProxyConfig config)
      
      // 清除系统代理
      public static bool ClearSystemProxy()
  }

□ CLI 参数
  proxy-tool env --http ... --system    # 系统级
  proxy-tool env --http ... --user      # 用户级（默认）

□ 跨平台抽象
  interface ISystemEnvProvider
  {
      bool SetProxy(ProxyConfig config);
      ProxyConfig GetProxy();
      bool ClearProxy();
  }
  
  class WindowsEnvProvider : ISystemEnvProvider
  class LinuxEnvProvider : ISystemEnvProvider
  class MacEnvProvider : ISystemEnvProvider
```

**输出文件:**
- `Platforms/WindowsEnvProvider.cs`
- `Platforms/LinuxEnvProvider.cs`
- `Platforms/MacEnvProvider.cs`

### 3.2 批量操作+导入导出（Week 7）

#### Day 43-44: 批量工具配置
**实施内容:**
```
□ CLI 命令
  proxy-tool tool --all --proxy http://proxy:8080
  proxy-tool tool --category "包管理器" --proxy ...
  proxy-tool tool --tools git,npm,pip --proxy ...

□ 实现逻辑
  var tools = ToolRegistry.GetAllTools();
  foreach (var tool in tools.Where(t => t.IsInstalled())) {
      await tool.SetProxy(url);
  }

□ 进度显示
  正在配置 [8/30] npm...
  [████████████████████░░░░░░░░░░] 60%
```

#### Day 45-47: 导入功能
**实施内容:**
```
□ 支持格式
  - JSON: proxy-tool import config.json
  - YAML: proxy-tool import config.yaml

□ JSON 格式
  {
    "name": "company",
    "envVars": {
      "httpProxy": "http://proxy:8080",
      "httpsProxy": "http://proxy:8080",
      "noProxy": "localhost,.company.com"
    },
    "toolConfigs": {
      "git": { "httpProxy": "http://proxy:8080" },
      "npm": { "httpProxy": "http://proxy:8080" }
    }
  }

□ 导入流程
  1. 读取文件
  2. 验证格式
  3. 预览变更（--dry-run）
  4. 应用配置
  5. 验证结果
```

#### Day 48-49: 导出功能
**实施内容:**
```
□ CLI 命令
  proxy-tool export --format json > config.json
  proxy-tool export --format yaml > config.yaml
  proxy-tool export --name company --format json

□ 导出内容
  - 当前环境变量
  - 所有工具配置
  - 时间戳和元数据
```

**输出文件:**
- `Managers/ConfigImporter.cs`
- `Managers/ConfigExporter.cs`

### 3.3 工具大扩展（Week 8-9）

#### Day 50-52: 版本控制工具
**实施内容:**
```
□ SVNConfigurator
  - 配置文件: ~/.subversion/servers
  - 配置项: http-proxy-host, http-proxy-port

□ HgConfigurator (Mercurial)
  - 配置文件: ~/.hgrc
  - 配置项: [http_proxy] host, port

□ RepoConfigurator
  - 配置文件: ~/.repoconfig
  - 环境变量: REPO_HTTP_PROXY

□ GhConfigurator (GitHub CLI)
  - 配置文件: ~/.config/gh/config.yml
  - 配置项: http_unix_socket
```

#### Day 53-55: 系统包管理器
**实施内容:**
```
□ AptConfigurator (Debian/Ubuntu)
  - 配置文件: /etc/apt/apt.conf
  - 配置项: Acquire::http::Proxy

□ YumConfigurator (RHEL/CentOS)
  - 配置文件: /etc/yum.conf
  - 配置项: proxy

□ DnfConfigurator (Fedora)
  - 配置文件: /etc/dnf/dnf.conf
  - 配置项: proxy

□ ZypperConfigurator (openSUSE)
  - 配置文件: /etc/zypp/zypp.conf

□ PacmanConfigurator (Arch)
  - 配置文件: /etc/pacman.conf

□ PortageConfigurator (Gentoo)
  - 配置文件: /etc/portage/make.conf
```

#### Day 56-58: IDE/编辑器
**实施内容:**
```
□ VSCodeConfigurator
  - 配置文件: ~/.vscode/settings.json
  - 配置项: http.proxy

□ CursorConfigurator
  - 配置文件: ~/.cursor/settings.json

□ IntelliJConfigurator
  - 配置文件: ~/.config/JetBrains/.../idea.properties

□ VimConfigurator
  - 配置文件: ~/.vimrc
  - 插件: httpcmd
```

#### Day 59-63: AI工具链
**实施内容:**
```
□ ClineConfigurator
  - VS Code 插件配置
  
□ ContinueConfigurator
  - 配置文件: ~/.continue/config.json

□ WindsurfConfigurator
  - 配置文件: ~/.windsurf/settings.json

□ ClaudeCodeConfigurator
  - 环境变量: CLAUDE_PROXY

□ OpenCodeConfigurator

□ OpenAiSdkConfigurator
  - 环境变量: OPENAI_PROXY

□ AnthropicSdkConfigurator
  - 环境变量: ANTHROPIC_PROXY
```

### 第三阶段交付物

**功能清单:**
- ✅ 系统级环境变量（Windows UAC / Linux sudo）
- ✅ 批量操作（--all, --category, --tools）
- ✅ 导入/导出（JSON/YAML）
- ✅ 30+ 工具支持

**工具列表（30个）:**
- 版本控制: Git, SVN, hg, repo, gh (5)
- 包管理器: npm, yarn, pnpm, pip, conda, poetry, gem, composer (8)
- 系统包管理器: apt, yum, dnf, zypper, pacman, portage (6)
- 容器工具: Docker, Podman, kubectl, helm (4)
- IDE/编辑器: VS Code, Cursor, IntelliJ, Vim (4)
- AI工具链: Cline, Continue, Windsurf, Claude Code, OpenCode, OpenAI SDK, Anthropic SDK (7)

---

## 第四阶段：高级功能（第10-13周）

### 目标
企业级功能，日志审计，MCP服务器，可选GUI。

### 4.1 日志与审计（Week 10）

#### Day 64-66: 日志系统
**实施内容:**
```
□ 创建 Logger 类
  public static class Logger
  {
      public static void Debug(string message);
      public static void Info(string message);
      public static void Warning(string message);
      public static void Error(string message, Exception? ex = null);
      
      // 日志级别
      public static LogLevel MinimumLevel { get; set; }
  }

□ 日志存储
  ~/.proxy-tool/logs/
  ├── proxy-tool_20260307.log
  └── proxy-tool_20260308.log

□ 日志格式
  [2026-03-07 10:30:15] [INFO] 设置 Git 代理: http://proxy:8080
  [2026-03-07 10:30:16] [DEBUG] 备份文件: ~/.gitconfig
  [2026-03-07 10:30:16] [INFO] Git 代理设置成功
```

**输出文件:**
- `Managers/Logger.cs`

#### Day 67-69: 审计日志
**实施内容:**
```
□ 审计记录
  public class AuditLog
  {
      public DateTime Timestamp { get; set; }
      public string Operation { get; set; }  // SET_PROXY, CLEAR_PROXY
      public string ToolName { get; set; }
      public string? OldValue { get; set; }
      public string? NewValue { get; set; }
      public bool Success { get; set; }
      public string? ErrorMessage { get; set; }
      public string Username { get; set; }
  }

□ 审计存储
  ~/.proxy-tool/audit/
  └── audit_202603.json

□ 查看命令
  proxy-tool audit --list
  proxy-tool audit --tool git
  proxy-tool audit --since 2026-03-01
```

**输出文件:**
- `Managers/AuditLogger.cs`

#### Day 70: 日志查看
**实施内容:**
```
□ CLI 命令
  proxy-tool log --tail 50
  proxy-tool log --level error
  proxy-tool log --since yesterday
```

### 4.2 安全+同步（Week 11）

#### Day 71-73: 配置加密
**实施内容:**
```
□ AES 加密
  public static class ConfigEncryption
  {
      // 加密敏感字段
      public static string Encrypt(string plainText, string password);
      
      // 解密
      public static string Decrypt(string cipherText, string password);
  }

□ 加密存储
  {
    "httpProxy": "ENC:AES256:base64encoded...",
    "httpsProxy": "ENC:AES256:base64encoded..."
  }

□ 密码输入
  proxy-tool profile save company --encrypt
  请输入加密密码: ********
  确认密码: ********
```

**输出文件:**
- `Security/ConfigEncryption.cs`

#### Day 74-76: 远程配置
**实施内容:**
```
□ 从 URL 加载
  proxy-tool import --url https://company.com/proxy-config.json
  
□ 自动同步
  proxy-tool sync --url https://company.com/proxy-config.json --interval 1h

□ 缓存机制
  - 本地缓存远程配置
  - 离线时使用缓存
  - 检测变更自动更新
```

#### Day 77: 配置模板
**实施内容:**
```
□ 内置模板
  proxy-tool template list
  proxy-tool template apply company
  proxy-tool template apply home
  proxy-tool template apply airport

□ 模板定义
  Templates/
  ├── company.json    # 公司代理
  ├── home.json       # 家庭网络（无代理）
  └── airport.json    # 机场代理
```

### 4.3 GUI+MCP（Week 12）

#### Day 78-80: Unity GUI 原型
**实施内容:**
```
□ 界面布局
  - 顶部: 环境变量面板
  - 中部: 工具列表（分类折叠）
  - 底部: 全局操作栏

□ 关键组件
  - ToolListItem: 工具行（名称、状态、开关）
  - ProxyInputPanel: 代理输入框
  - ProfileSelector: 配置集选择器
  - SearchBox: 搜索过滤

□ 数据绑定
  - 绑定到 Core DLL
  - 实时状态更新
```

**输出项目:**
- `ProxyTool.GUI/` (Unity 项目)

#### Day 81-83: MCP 服务器
**实施内容:**
```
□ MCP 协议实现
  public class ProxyToolMcpServer
  {
      // MCP 工具定义
      public List<McpTool> GetTools()
      
      // 处理调用
      public Task<object> InvokeTool(string name, Dictionary<string, object> args)
  }

□ 暴露的工具
  {
    "tools": [
      {
        "name": "set_proxy",
        "description": "设置系统代理",
        "parameters": {
          "http_proxy": { "type": "string" },
          "https_proxy": { "type": "string" },
          "tools": { "type": "array", "items": { "type": "string" } }
        }
      },
      {
        "name": "test_proxy",
        "description": "测试代理连通性",
        "parameters": {
          "proxy_url": { "type": "string" }
        }
      },
      {
        "name": "get_current_config",
        "description": "获取当前代理配置",
        "parameters": {}
      }
    ]
  }
```

**输出文件:**
- `Mcp/ProxyToolMcpServer.cs`

#### Day 84: MCP 工具定义
**实施内容:**
```
□ 工具实现
  - set_proxy: 设置代理
  - test_proxy: 测试代理
  - get_current_config: 获取配置
  - list_tools: 列出支持的工具
  - apply_profile: 应用配置集

□ 启动命令
  proxy-tool mcp --port 8080
```

### 4.4 发布准备（Week 13）

#### Day 85-87: 自动更新
**实施内容:**
```
□ 版本检查
  - 查询 GitHub Releases API
  - 比较本地版本和远程版本

□ 自动下载
  - 下载最新版本到临时目录
  - 验证文件哈希
  - 替换可执行文件

□ CLI 命令
  proxy-tool update check
  proxy-tool update install
```

**输出文件:**
- `Managers/UpdateManager.cs`

#### Day 88-89: 文档完善
**实施内容:**
```
□ README.md
  - 项目介绍
  - 安装指南
  - 快速开始
  - 完整命令参考

□ 使用手册
  - 详细教程
  - 常见问题
  - 故障排除

□ API 文档
  - Core DLL API
  - MCP 工具文档
```

#### Day 90-91: 打包发布
**实施内容:**
```
□ 多平台打包
  - Windows: proxy-tool-win-x64.zip
  - Linux: proxy-tool-linux-x64.tar.gz
  - macOS: proxy-tool-osx-x64.tar.gz

□ 包管理器
  - Chocolatey (Windows)
  - Homebrew (macOS)
  - APT/YUM (Linux)

□ GitHub Releases
  - 自动发布流程
  - 更新日志
  - 二进制附件
```

### 第四阶段交付物

**功能清单:**
- ✅ 日志系统（Debug/Info/Warning/Error）
- ✅ 审计日志（变更历史）
- ✅ 配置加密（AES256）
- ✅ 远程配置同步
- ✅ 配置模板
- ✅ MCP 服务器
- ✅ Unity GUI（可选）
- ✅ 自动更新
- ✅ 完整文档
- ✅ 多平台发布

---

## 项目总时间线

```
Week 1-3:   第一阶段 - 核心功能完善
Week 4-5:   第二阶段 - 安全与验证
Week 6-9:   第三阶段 - 功能扩展
Week 10-13: 第四阶段 - 高级功能

总计: 13周（约3个月）
```

## 里程碑

| 里程碑 | 时间 | 交付物 |
|--------|------|--------|
| v0.1.0 Alpha | Week 3 | 可运行CLI，8个工具 |
| v0.2.0 Beta | Week 5 | 安全机制完整 |
| v0.3.0 RC | Week 9 | 30+工具，系统env |
| v1.0.0 GA | Week 13 | 企业功能，GUI，MCP |

---

*文档版本: 1.0*
*最后更新: 2026-03-07*