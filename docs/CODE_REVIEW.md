# Gate 代码设计分析与改进建议

> 从软件工程角度对当前代码库的审查与改进建议

---

## 一、当前架构总结

```
Gate.Core (netstandard2.0)
  ├── Managers/         EnvVarManager, ProfileManager, ToolRegistry
  ├── Models/           ProxyConfig, Profile, ProxyTestResult
  ├── Configurators/    ToolConfiguratorBase + 200+ 实现类
  ├── UI/               ConsoleStyle, ErrorHelper
  └── ProxyTester        代理连通性测试

Gate.CLI (net8.0)
  └── Program.cs         ~350行单文件，所有命令定义

Unity GUI
  ├── GatePanelController    主导航控制器
  ├── GlobalPanelController  全局代理面板
  ├── AppPanelController     应用代理面板
  ├── PresetPanelController  预设面板
  ├── StatusPanelController  状态面板
  └── TestPanelController    测试面板
```

---

## 二、核心问题

### 2.1 ToolRegistry 是静态注册表，缺乏扩展性

**现状**：所有 ToolConfigurator 在编译时静态注册，外部无法在运行时动态添加工具。

**问题**：
- 新增工具必须修改源码并重新编译
- 不支持用户自定义工具
- 不支持社区插件热加载

**建议**：
```csharp
// 当前（推测）
public static class ToolRegistry
{
    private static readonly List<ToolConfiguratorBase> _tools = new()
    {
        new GitConfigurator(),
        new NpmConfigurator(),
        // ... 200+ 硬编码
    };
}

// 建议：支持动态注册 + 插件目录扫描
public static class ToolRegistry
{
    public static void Register(ToolConfiguratorBase tool) { ... }
    public static void LoadPluginDirectory(string path) { ... }
    public static void LoadFromJson(string jsonPath) { ... }  // 声明式注册
}
```

### 2.2 工具路径检测不可配置

**现状**：`IsInstalled()` 和 `DetectConfigPath()` 硬编码路径逻辑，用户无法自定义。

**问题**：
- 非标准安装路径的工具会被误判为未安装
- 多版本并存时无法指定版本
- 配置文件路径不可覆盖

**建议**：
```csharp
public abstract class ToolConfiguratorBase
{
    // 新增：可配置路径（优先级高于自动检测）
    public string? CustomExecutablePath { get; set; }
    public string? CustomConfigPath { get; set; }

    public bool IsInstalled()
    {
        if (CustomExecutablePath != null)
            return File.Exists(CustomExecutablePath);
        return DetectIsInstalled();  // 原有逻辑
    }
}
```

### 2.3 Program.cs 单文件过大

**现状**：`Program.cs` 约350行，包含全部9个命令的定义和处理逻辑。

**问题**：
- 难以单独测试各命令
- 代码混杂，新增命令需要在一个大文件中找位置
- lambda 嵌套深，可读性差

**建议**：
```
Gate.CLI/
  Commands/
    GlobalCommand.cs
    AppCommand.cs
    PresetCommand.cs
    InfoCommand.cs
    TestCommand.cs
    SetCommand.cs
    ApplyCommand.cs
    WizardCommand.cs
  Program.cs  // 仅负责组装命令树，<30行
```

### 2.4 Unity GUI 控制器职责不清

**现状**：`AppPanelController` 既负责 UI 绑定，又直接调用 `ToolRegistry` 和业务逻辑。

**问题**：
- UI 和业务逻辑耦合，难以测试
- 多个控制器可能重复相同的数据访问逻辑

**建议**：引入简单 ViewModel 层：
```csharp
// AppViewModel.cs
public class AppViewModel
{
    public List<ToolViewModel> Tools { get; }
    public string SearchFilter { get; set; }
    public string SelectedCategory { get; set; }
    public event Action<List<ToolViewModel>> OnFilterChanged;

    public void SetProxy(string toolName, string proxy) { ... }
    public void ClearProxy(string toolName) { ... }
    public List<ToolViewModel> ApplyFilter() { ... }
}

// AppPanelController.cs  只负责 UI 绑定
public class AppPanelController
{
    private AppViewModel _vm;
    // 监听 ViewModel 事件刷新 UI
}
```

### 2.5 异步处理不一致

**现状**：CLI 中部分命令使用 `async/await`，但 `appHandler` 内使用 `await Task.CompletedTask` 占位（实际同步执行）。Unity GUI 控制器全部同步。

**问题**：
- 代理测试是网络 I/O，同步调用会阻塞 Unity 主线程
- `TestPanelController` 调用 `ProxyTester` 可能卡顿界面

**建议**：
```csharp
// Unity 中使用协程或 UniTask
private async void RunTest(string proxy)
{
    _btnRunTest.SetEnabled(false);
    _btnRunTest.text = "测试中...";

    // 在后台线程运行
    var result = await System.Threading.Tasks.Task.Run(
        () => ProxyTester.TestProxyAsync(proxy)
    );

    // 回到主线程更新 UI
    UpdateResultUI(result);
    _btnRunTest.SetEnabled(true);
    _btnRunTest.text = "开始测试";
}
```

### 2.6 配置持久化缺乏版本管理

**现状**：Profile/配置直接序列化为 JSON，没有版本字段。

**问题**：
- 未来新增字段后旧配置文件无法平滑升级
- 没有配置迁移机制

**建议**：
```json
{
  "$schema": "gate-profile-v1",
  "version": 1,
  "name": "office",
  ...
}
```

### 2.7 错误处理策略不统一

**现状**：CLI 中混用 `ConsoleStyle.Error()` 输出错误后继续执行；部分地方直接 `return` 而不设置退出码。

**问题**：
- 脚本调用 gate 时无法通过退出码判断成功/失败
- 错误信息格式不一致

**建议**：
```csharp
// 定义统一退出码
public static class ExitCode
{
    public const int Success = 0;
    public const int InvalidArgument = 1;
    public const int ToolNotFound = 2;
    public const int ProxyTestFailed = 3;
    public const int ConfigWriteFailed = 4;
}

// 所有命令 handler 返回 int
return ExitCode.ToolNotFound;
```

---

## 三、代码质量建议

### 3.1 缺少单元测试

**现状**：没有发现测试项目。

**建议**：
```
Gate.Tests/
  Unit/
    EnvVarManagerTests.cs
    ConfigValidatorTests.cs
    ProxyConfigTests.cs
  Integration/
    ToolRegistryTests.cs
    ProfileManagerTests.cs
```

核心要测试的场景：
- `ConfigValidator` 各种合法/非法代理地址格式
- `ProfileManager` 保存/加载/删除循环
- `EnvVarManager` 设置和清除代理

### 3.2 日志系统缺失

**现状**：CLI 使用 `ConsoleStyle` 输出，Unity 使用 `Debug.Log`，没有统一日志框架。

**建议**：引入 `Microsoft.Extensions.Logging.Abstractions`，提供统一 `ILogger` 接口：
```csharp
public class ToolRegistry
{
    private static readonly ILogger _log =
        LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ToolRegistry>();
}
```

### 3.3 string 魔法值过多

**现状**：工具名、分类名等使用字符串字面量，可能因拼写错误导致 bug。

**建议**：
```csharp
public static class ToolCategories
{
    public const string VersionControl = "版本控制";
    public const string PackageManager = "包管理器";
    public const string Container = "容器与编排";
    // ...
}
```

### 3.4 Unity GUI 中文硬编码

**现状**：UI 控制器中反馈文字直接硬编码英文（`"Set proxy for {ok} apps."`）。

**建议**：抽取到资源文件或常量类，支持本地化：
```csharp
public static class UIStrings
{
    public static string BatchSetSuccess(int count) => $"已为 {count} 个应用设置代理";
    public static string BatchClearSuccess(int count) => $"已清除 {count} 个应用的代理";
}
```

### 3.5 AppPanelController 事件注销不完整

**现状**：`RegisterCallbacks()` 注册了大量事件，但没有对应的 `UnregisterCallbacks()`。

**问题**：面板被销毁重建时（Unity 热重载等场景）可能产生内存泄漏和重复触发。

**建议**：
```csharp
public void Dispose()
{
    _searchField?.UnregisterValueChangedCallback(OnSearchChanged);
    _categoryFilter?.UnregisterValueChangedCallback(OnCategoryChanged);
    // ...
}
```

---

## 四、需要补充的功能

| 功能 | 优先级 | 说明 |
|------|--------|------|
| 工具路径自定义 | 高 | 用户指定可执行文件路径，解决非标准安装问题 |
| 单元测试项目 | 高 | Gate.Tests，覆盖核心 Manager 和 Validator |
| 异步代理测试（Unity） | 高 | 避免阻塞主线程 |
| 退出码规范 | 中 | 支持脚本集成 |
| 命令文件拆分 | 中 | Program.cs 拆为独立 Command 类 |
| ViewModel 层 | 中 | 解耦 Unity GUI 控制器 |
| 日志系统 | 中 | 统一 ILogger |
| 配置版本字段 | 低 | 支持未来迁移 |
| 字符串常量化 | 低 | 消除魔法值 |
