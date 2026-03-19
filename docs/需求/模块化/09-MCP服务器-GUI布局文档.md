# 09 MCP 服务器 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./09-MCP服务器-需求文档.md) | [Unity GUI 规范](./09-Unity-GUI规范.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----||
| 面板名称 | MCP 服务器面板（MCP Panel） |
| 控制器类 | `McpPanelController` |
| UXML 文件 | `Assets/UI/AIGate/McpPanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/McpPanel.uss` |
| 导航项 | `[M] MCP`（侧边栏第九项） |
| 功能 | 管理 MCP 服务器的启停、传输模式配置、工具列表查看、日志监控及客户端配置生成 |

> **优先级**：P0。本面板对应 `gate mcp` 命令，让 Unity 开发者可在编辑器内启动 MCP 服务器并查看实时日志，无需切换终端。

---

## 2. 布局结构（层级关系）

```
McpPanel (VisualElement .mcp-panel)
├── 状态卡 (.status-card)
│   ├── 标题行 (.card-header)
│   │   ├── Label "MCP 服务器" .card-title
│   │   └── Label name=serverStatusBadge .badge  // "运行中" / "已停止"
│   ├── 模式行 (.form-row)
│   │   ├── Label "传输模式" .form-label
│   │   └── RadioButtonGroup name=modeGroup
│   │       ├── RadioButton name=modeStdio text="stdio（本地 AI 工具）" value="stdio"
│   │       └── RadioButton name=modeSse   text="SSE（远程客户端）"   value="sse"
│   ├── SSE 配置行 (VisualElement name=sseConfigRow display=none .form-row)
│   │   ├── Label "端口" .form-label
│   │   ├── IntegerField name=ssePortInput value=3001 .input-sm
│   │   └── Toggle name=sseRemoteToggle text="允许远程连接 (0.0.0.0)" .form-toggle
│   └── 操作行 (.action-row)
│       ├── Button name=startBtn text="启动 MCP" .btn-primary
│       └── Button name=stopBtn  text="停止"     .btn-danger display=none
│
├── 工具列表卡 (.tools-card)
│   ├── Label "可用 MCP 工具（9 个）" .card-title
│   └── ScrollView name=mcpToolsScroll .mcp-tools-list
│       └── [foreach MCP 工具定义]
│           └── 工具行 (.mcp-tool-row)
│               ├── Label name=toolName  .mcp-tool-name
│               ├── Label name=toolDesc  .mcp-tool-desc
│               └── Label name=toolRequired .mcp-tool-required  // "必填: proxy"
│
├── 客户端配置卡 (.config-card)
│   ├── 标题行 (.card-header)
│   │   ├── Label "客户端配置" .card-title
│   │   └── 客户端切换 (VisualElement .client-tabs)
│   │       ├── Button name=clientCursor  text="Cursor"         .client-tab .active
│   │       ├── Button name=clientClaude  text="Claude Desktop" .client-tab
│   │       └── Button name=clientCustom  text="自定义"         .client-tab
│   ├── Label name=configSnippet .config-snippet  // 代码片段
│   └── 操作行 (.action-row)
│       └── Button name=copyConfigBtn text="复制配置" .btn-secondary
│
├── 日志卡 (.log-card)
│   ├── 标题行 (.card-header)
│   │   ├── Label "实时日志" .card-title
│   │   ├── Toggle name=autoScrollToggle text="自动滚动" value=true .form-toggle
│   │   └── Button name=clearLogBtn text="清空" .btn-icon
│   ├── ScrollView name=logScroll .log-scroll
│   │   └── [foreach 日志行]
│   │       └── Label name=logLine .log-line  // 带颜色标记
│   └── Button name=openLogFileBtn text="在文件管理器中打开 mcp.log" .btn-secondary
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性详细说明

### 3.1 状态卡

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `serverStatusBadge` | Label | 停止时：「已停止」灰色 `#64748b`；运行中：「● 运行中」绿色 `#22c55e`，CSS 脉冲动画 |
| `modeGroup` | RadioButtonGroup | 默认选 `stdio`；选 SSE 时 `sseConfigRow` 切换为 `display:flex` |
| `ssePortInput` | IntegerField | 范围 1024–65535；端口占用时红色边框并提示；默认 `3001` |
| `sseRemoteToggle` | Toggle | 仅 SSE 模式可见；选中时启动参数追加 `--host 0.0.0.0`；显示橙色安全警告 |
| `startBtn` | Button | 服务器已运行时禁用；启动成功后 `display:none`，`stopBtn` 切换为 `display:flex` |
| `stopBtn` | Button | 默认 `display:none`；服务器运行时显示；点击终止后台进程，切换回 `startBtn` |

> **B47/B48**：GUI 启动 MCP 按钮仅支持 **SSE 模式**。若用户选择 stdio 模式，`startBtn` 和 `stopBtn` 隐藏，改为显示说明文字：「stdio 模式由 Cursor / Claude Desktop 自动管理，无需手动启动」；客户端配置卡仍正常显示 `mcp.json` 配置片段供用户复制。

**安全警告**（`sseRemoteToggle` 选中时，在其下方插入）：
```
⚠ 允许远程连接将对局域网暴露 MCP 服务，请确保网络安全
```
颜色 `#f59e0b`，11px。

### 3.2 工具列表卡

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `toolName` | Label | 工具名称；蓝色 `#4f8ef7`；等宽字体；12px |
| `toolDesc` | Label | 工具描述；灰色 `#94a3b8`；11px；超长省略 |
| `toolRequired` | Label | 必填参数提示，如「必填: proxy」；橙色 `#f59e0b`；10px |

工具列表静态内容（对应 `09-MCP服务器-需求文档.md §3`）：

| 工具名 | 描述 | 必填参数 |
|--------|------|----------|
| `set_proxy` | 设置全局代理 | `proxy` |
| `clear_proxy` | 清除全局代理 | — |
| `get_status` | 获取当前代理状态 | — |
| `set_tool_proxy` | 设置指定工具代理 | `tool` |
| `list_tools` | 列出所有支持工具 | — |
| `list_presets` | 列出所有预设 | — |
| `save_preset` | 保存当前配置为预设 | `name` |
| `load_preset` | 加载预设 | `name` |
| `test_proxy` | 测试代理连通性 | — |

### 3.3 客户端配置卡

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `clientCursor` / `clientClaude` / `clientCustom` | Button | 互斥单选；激活项加 `.active` 类；切换时 `configSnippet` 更新 |
| `configSnippet` | Label | 等宽字体；显示当前客户端对应的配置 JSON 片段；`white-space: pre` |
| `copyConfigBtn` | Button | 复制 `configSnippet.text` 到剪贴板；3 秒内按钮文字变「已复制」 |

配置片段内容（按模式和客户端动态生成）：

**Cursor — stdio 模式**：
```json
{
  "mcpServers": {
    "gate": { "command": "gate", "args": ["mcp"] }
  }
}
```

**Claude Desktop — SSE 模式（端口 3001）**：
```json
{
  "mcpServers": {
    "gate": { "url": "http://localhost:3001/sse" }
  }
}
```

**自定义** — 仅显示端点地址（SSE）或命令（stdio），供用户手动配置。

### 3.4 日志卡

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `autoScrollToggle` | Toggle | 选中时每次追加日志后自动滚到底部 |
| `clearLogBtn` | Button | 清空 `logScroll` 中所有日志行 Label |
| `logLine` | Label | 等宽字体 11px；`[INFO]` 灰色、`[WARN]` 黄色、`[ERROR]` 红色；格式：`[HH:mm:ss] [LEVEL] 消息` |
| `openLogFileBtn` | Button | 调用 `EditorUtility.RevealInFinder(GatePaths.McpLogFile)`；文件不存在时禁用 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/McpPanel.uxml");
    tree.CloneTree(rootVisualElement);

    _startBtn    = rootVisualElement.Q<Button>("startBtn");
    _stopBtn     = rootVisualElement.Q<Button>("stopBtn");
    _modeGroup   = rootVisualElement.Q<RadioButtonGroup>("modeGroup");
    _sseConfigRow = rootVisualElement.Q<VisualElement>("sseConfigRow");
    _ssePortInput = rootVisualElement.Q<IntegerField>("ssePortInput");
    _sseRemote   = rootVisualElement.Q<Toggle>("sseRemoteToggle");
    _logScroll   = rootVisualElement.Q<ScrollView>("logScroll");
    _statusBadge = rootVisualElement.Q<Label>("serverStatusBadge");
    _configSnippet = rootVisualElement.Q<Label>("configSnippet");

    _modeGroup.RegisterValueChangedCallback(OnModeChanged);
    _sseRemote.RegisterValueChangedCallback(OnSseRemoteChanged);
    _startBtn.RegisterCallback<ClickEvent>(OnStartClicked);
    _stopBtn.RegisterCallback<ClickEvent>(OnStopClicked);

    // 客户端配置切换
    rootVisualElement.Q<Button>("clientCursor") .RegisterCallback<ClickEvent>(_ => SetClient("cursor"));
    rootVisualElement.Q<Button>("clientClaude") .RegisterCallback<ClickEvent>(_ => SetClient("claude"));
    rootVisualElement.Q<Button>("clientCustom") .RegisterCallback<ClickEvent>(_ => SetClient("custom"));
    rootVisualElement.Q<Button>("copyConfigBtn").RegisterCallback<ClickEvent>(OnCopyConfig);
    rootVisualElement.Q<Button>("clearLogBtn")  .RegisterCallback<ClickEvent>(_ => _logScroll.Clear());
    rootVisualElement.Q<Button>("openLogFileBtn").RegisterCallback<ClickEvent>(OnOpenLogFile);

    BuildMcpToolList();
    SetClient("cursor");
    Refresh();
}
```

### 4.2 模式切换

```csharp
private void OnModeChanged(ChangeEvent<string> evt) {
    bool isSse = evt.newValue == "sse";
    _sseConfigRow.style.display = isSse ? DisplayStyle.Flex : DisplayStyle.None;
    UpdateConfigSnippet();
}

private void OnSseRemoteChanged(ChangeEvent<bool> evt) {
    // 显示或移除安全警告 Label
    var warning = rootVisualElement.Q<Label>("sseRemoteWarning");
    if (warning != null) warning.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
}
```

### 4.3 启动 MCP 服务器（异步后台进程）

```csharp
private async void OnStartClicked(ClickEvent evt) {
    _startBtn.SetEnabled(false);
    _startBtn.text = "启动中…";

    try {
        var args = BuildArgs(); // "mcp" 或 "mcp --sse --port 3001 [--host 0.0.0.0]"
        _mcpProcess = await Task.Run(() => GateCliRunner.StartBackground(args));

        // 监听日志文件（tail -f 效果）
        _logWatcher = new McpLogWatcher(GatePaths.McpLogFile, line =>
            rootVisualElement.schedule.Execute(() => AppendLog(line)));
        _logWatcher.Start();

        UpdateServerStatus(running: true);
        ShowFeedback("MCP 服务器已启动", true);
    } catch (Exception ex) {
        ShowFeedback($"启动失败：{ex.Message}", false);
        _startBtn.SetEnabled(true);
        _startBtn.text = "启动 MCP";
    }
}

private string BuildArgs() {
    if (_modeGroup.value == "stdio") return "mcp";
    var port = _ssePortInput.value;
    var host = _sseRemote.value ? " --host 0.0.0.0" : "";
    return $"mcp --sse --port {port}{host}";
}
```

### 4.4 停止 MCP 服务器

```csharp
private async void OnStopClicked(ClickEvent evt) {
    _stopBtn.SetEnabled(false);
    await Task.Run(() => {
        _logWatcher?.Stop();
        _mcpProcess?.Kill(entireProcessTree: true);
        _mcpProcess?.Dispose();
        _mcpProcess = null;
    });
    UpdateServerStatus(running: false);
    ShowFeedback("MCP 服务器已停止", true);
}

private void UpdateServerStatus(bool running) {
    _statusBadge.text  = running ? "● 运行中" : "已停止";
    _statusBadge.style.color = running
        ? new Color(0.13f, 0.77f, 0.37f)   // #22c55e
        : new Color(0.39f, 0.45f, 0.55f);  // #64748b
    _startBtn.style.display = running ? DisplayStyle.None : DisplayStyle.Flex;
    _stopBtn .style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
    _startBtn.SetEnabled(!running);
    _startBtn.text = "启动 MCP";
}

// B45：定时检测进程存活，防止意外崩溃后 UI 状态不更新
private void StartProcessMonitor() {
    rootVisualElement.schedule.Execute(() => {
        if (_mcpProcess != null && _mcpProcess.HasExited) {
            AppendLog("[ERROR] MCP 进程意外退出");
            UpdateServerStatus(running: false);
            _logWatcher?.Stop();
            _mcpProcess = null;
        }
    }).Every(5000); // 每 5 秒检查一次
}
```

### 4.5 日志追加

```csharp
private void AppendLog(string line) {
    // 解析日志级别确定颜色
    Color color;
    if (line.Contains("[ERROR]"))      color = new Color(0.97f, 0.44f, 0.44f); // #f87171
    else if (line.Contains("[WARN]"))  color = new Color(0.96f, 0.62f, 0.04f); // #f59e0b
    else                               color = new Color(0.58f, 0.64f, 0.73f); // #94a3b8

    var lbl = new Label(line);
    lbl.AddToClassList("log-line");
    lbl.style.color = color;
    _logScroll.Add(lbl);

    // 限制日志行数，防止内存膨胀（保留最新 500 行）
    while (_logScroll.childCount > 500)
        _logScroll.RemoveAt(0);

    if (_autoScrollToggle.value)
        _logScroll.scrollOffset = new Vector2(0, float.MaxValue);
}
```

### 4.6 客户端配置片段生成

```csharp
private void UpdateConfigSnippet() {
    bool isSse = _modeGroup.value == "sse";
    int  port  = _ssePortInput.value;

    _configSnippet.text = _currentClient switch {
        "cursor" when !isSse =>
            "{\n  \"mcpServers\": {\n    \"gate\": { \"command\": \"gate\", \"args\": [\"mcp\"] }\n  }\n}",
        "cursor" when isSse =>
            $"{{\n  \"mcpServers\": {{\n    \"gate\": {{ \"url\": \"http://localhost:{port}/sse\" }}\n  }}\n}}",
        "claude" when !isSse =>
            "{\n  \"mcpServers\": {\n    \"gate\": { \"command\": \"gate\", \"args\": [\"mcp\"] }\n  }\n}",
        "claude" when isSse =>
            $"{{\n  \"mcpServers\": {{\n    \"gate\": {{ \"url\": \"http://localhost:{port}/sse\" }}\n  }}\n}}",
        _ => isSse
            ? $"SSE 端点：http://localhost:{port}/sse"
            : "命令：gate mcp"
    };
}
```

### 4.7 Dispose

```csharp
public override void Dispose() {
    _logWatcher?.Stop();
    _mcpProcess?.Kill(entireProcessTree: true);
    _mcpProcess?.Dispose();
    _modeGroup?.UnregisterValueChangedCallback(OnModeChanged);
    _sseRemote?.UnregisterValueChangedCallback(OnSseRemoteChanged);
    _startBtn ?.UnregisterCallback<ClickEvent>(OnStartClicked);
    _stopBtn  ?.UnregisterCallback<ClickEvent>(OnStopClicked);
    foreach (var name in new[]{ "clientCursor","clientClaude","clientCustom","copyConfigBtn","clearLogBtn","openLogFileBtn" })
        rootVisualElement.Q<Button>(name)?.UnregisterCallback<ClickEvent>(null);
}
```

---

## 5. 响应式适配

| 窗口宽度 | 布局行为 |
|----------|---------|
| ≥ 600px | 状态卡、工具列表、客户端配置、日志并排可见 |
| 400–599px | 工具列表折叠为「展开工具列表」按钮；日志卡高度压缩 |
| < 400px | 客户端切换按钮换行；日志最大高度 120px；配置片段横向滚动 |

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|---------|
| `.mcp-panel` | `padding: 16px; flex-direction: column; gap: 12px` |
| `.status-card` | `background: #111827; border-radius: 10px; padding: 16px 20px; border: 1px solid #1e293b` |
| `.tools-card` | `background: #111827; border-radius: 10px; padding: 16px 20px; max-height: 200px` |
| `.mcp-tools-list` | `flex-direction: column` |
| `.mcp-tool-row` | `flex-direction: row; align-items: baseline; padding: 5px 0; border-bottom: 1px solid #1e293b; gap: 10px` |
| `.mcp-tool-name` | `color: #4f8ef7; font-size: 11px; font-family: JetBrainsMono; width: 120px; flex-shrink: 0` |
| `.mcp-tool-desc` | `color: #94a3b8; font-size: 11px; flex-grow: 1` |
| `.mcp-tool-required` | `color: #f59e0b; font-size: 10px; flex-shrink: 0` |
| `.config-card` | `background: #111827; border-radius: 10px; padding: 16px 20px` |
| `.client-tabs` | `flex-direction: row; gap: 4px; margin-bottom: 10px` |
| `.client-tab` | `background: #1e293b; color: #64748b; border-radius: 4px; padding: 4px 10px; font-size: 11px` |
| `.client-tab.active` | `background: #1e3a5f; color: #4f8ef7` |
| `.config-snippet` | `font-family: JetBrainsMono; font-size: 11px; color: #e2e8f0; background: #0a0e18; padding: 10px; border-radius: 6px; white-space: pre` |
| `.log-card` | `background: #0a0e18; border-radius: 10px; padding: 12px 16px; flex-grow: 1` |
| `.log-scroll` | `flex-grow: 1; max-height: 250px` |
| `.log-line` | `font-family: JetBrainsMono; font-size: 10px; white-space: pre-wrap; margin-bottom: 1px` |
| `.badge` (运行中) | `background: #0d1f17; color: #22c55e; padding: 2px 8px; border-radius: 10px; font-size: 10px` |
| `.badge` (已停止) | `color: #64748b; background: transparent; font-size: 10px` |

**字体**：工具名、配置片段、日志使用 `JetBrainsMono-Regular SDF.asset`；其余使用 `SourceHanSansSC-Regular SDF.asset`。

---

## 7. 辅助类说明

### McpLogWatcher（日志文件监听）

```csharp
// Gate.Editor/UI/Utils/McpLogWatcher.cs
/// <summary>
/// 监听 mcp.log 文件的新增行，通过回调推送到 UI。
/// 使用 FileSystemWatcher + 记录文件偏移量实现 tail -f 效果。
/// </summary>
public class McpLogWatcher : IDisposable {
    private readonly string _logPath;
    private readonly Action<string> _onNewLine;
    private FileSystemWatcher _watcher;
    private long _lastOffset = 0;

    public McpLogWatcher(string logPath, Action<string> onNewLine) {
        _logPath   = logPath;
        _onNewLine = onNewLine;
    }

    public void Start() {
        // 初始读取已有内容（最新 100 行）
        if (File.Exists(_logPath)) {
            var lines = File.ReadAllLines(_logPath);
            foreach (var line in lines.TakeLast(100))
                _onNewLine(line);
            _lastOffset = new FileInfo(_logPath).Length;
        }

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_logPath)!,
            Path.GetFileName(_logPath)) {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e) {
        try {
            using var fs     = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            fs.Seek(_lastOffset, SeekOrigin.Begin);
            string? line;
            while ((line = reader.ReadLine()) != null)
                _onNewLine(line);
            _lastOffset = fs.Position;
        } catch { /* 文件被轮转时忽略，等待下次 Changed 事件 */ }
    }

    public void Stop()  => _watcher?.Dispose();
    public void Dispose() => Stop();
}
```

### GateCliRunner.StartBackground

```csharp
// Gate.Editor/UI/Utils/GateCliRunner.cs
public static class GateCliRunner {
    /// <summary>
    /// 在后台启动 gate 进程（不等待退出），返回 Process 句柄供后续 Kill。
    /// stdout/stderr 重定向到 mcp.log（由 gate 自身负责写入）。
    /// </summary>
    public static Process StartBackground(string args) {
        var psi = new ProcessStartInfo("gate", args) {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardInput  = true,
            RedirectStandardOutput = false, // gate mcp 自行写日志
            RedirectStandardError  = false,
        };
        return Process.Start(psi) ?? throw new Exception("无法启动 gate 进程");
    }
}
```
