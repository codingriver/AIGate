# 06 CLI 命令系统 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./06-CLI命令-需求文档.md) | [Unity GUI 规范](./09-Unity-GUI规范.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----||
| 面板名称 | 命令控制台面板（Console Panel） |
| 控制器类 | `ConsolePanelController` |
| UXML 文件 | `Assets/UI/AIGate/ConsolePanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/ConsolePanel.uss` |
| 导航项 | `[>_] 命令`（侧边栏第六项，可选显示） |
| 功能 | 在 Unity 编辑器内执行 Gate CLI 命令、查看诊断结果、管理全局配置 |

> **设计说明**：CLI 命令系统本身是终端工具，GUI 面板以「嵌入式命令控制台 + 诊断面板 + 配置管理」三合一形式呈现，让 Unity 开发者无需切换终端窗口即可完成所有 Gate 操作。

---

## 2. 布局结构（层级关系）

```
ConsolePanel (VisualElement .console-panel)
├── 标签页导航 (VisualElement .tab-bar)
│   ├── Button name=tabConsole  text="命令控制台" .tab-btn .active
│   ├── Button name=tabDoctor   text="诊断"       .tab-btn
│   └── Button name=tabConfig   text="全局配置"   .tab-btn
│
├── [Tab 0: 命令控制台] (VisualElement name=panelConsole)
│   ├── 快捷命令区 (.quick-cmds-card)
│   │   ├── Label "快捷操作" .card-title
│   │   └── 按钮网格 (VisualElement .quick-grid)
│   │       ├── Button name=btnInfo       text="gate info"       .quick-btn
│   │       ├── Button name=btnEnv        text="gate env"        .quick-btn
│   │       ├── Button name=btnHistory    text="gate history"    .quick-btn
│   │       ├── Button name=btnWizard     text="gate wizard"     .quick-btn
│   │       ├── Button name=btnDoctor     text="gate doctor"     .quick-btn
│   │       └── Button name=btnCompletion text="gate completion" .quick-btn
│   ├── 命令输入区 (.cmd-input-card)
│   │   ├── Label "执行命令" .card-title
│   │   ├── 输入行 (VisualElement .cmd-input-row)
│   │   │   ├── Label "gate" .cmd-prefix
│   │   │   ├── TextField name=cmdInput placeholder="子命令及参数，如 set http://127.0.0.1:7890" .cmd-field
│   │   │   └── Button name=runBtn text="执行" .btn-primary
│   │   └── 输出格式行 (VisualElement .format-row)
│   │       ├── Toggle name=jsonToggle  text="--json"  .format-toggle
│   │       ├── Toggle name=quietToggle text="--quiet" .format-toggle
│   │       └── Toggle name=plainToggle text="--plain" .format-toggle
│   └── 输出区 (.output-card)
│       ├── 输出标题行 (VisualElement .card-header)
│       │   ├── Label "命令输出" .card-title
│       │   └── Button name=clearOutputBtn text="清空" .btn-icon
│       └── ScrollView name=outputScroll .output-scroll
│           └── Label name=outputLabel .output-text
│
├── [Tab 1: 诊断] (VisualElement name=panelDoctor display=none)
│   ├── 操作行 (.action-row)
│   │   ├── Button name=runDoctorBtn text="重新检查" .btn-primary
│   │   └── Label name=lastCheckTime .check-time
│   └── ScrollView name=doctorScroll .doctor-list
│       └── [foreach 8 项检查]
│           └── 诊断行 (.doctor-row)
│               ├── Label name=checkIcon .check-icon   // ✓ / ✗ / ⚠
│               ├── VisualElement .check-content
│               │   ├── Label name=checkName .check-name
│               │   └── Label name=checkDesc .check-desc
│               └── Button name=fixBtn text="修复" .btn-xs-primary display=none
│
└── [Tab 2: 全局配置] (VisualElement name=panelConfig display=none)
    ├── Label "全局配置项 (gate config)" .card-title
    ├── ScrollView name=configScroll .config-list
    │   └── [foreach GlobalConfig 配置项]
    │       └── 配置行 (.config-row)
    │           ├── VisualElement .config-info
    │           │   ├── Label name=configKey  .config-key
    │           │   └── Label name=configDesc .config-desc
    │           └── [按字段类型]
    │               ├── [bool]   Toggle       name=configToggle
    │               ├── [int]    IntegerField name=configInt
    │               └── [string] TextField    name=configStr
    └── 操作行 (.action-row)
        ├── Button name=saveConfigBtn  text="保存配置" .btn-primary
        └── Button name=resetConfigBtn text="恢复默认" .btn-danger
```

---

## 3. 控件属性详细说明

### 3.1 命令控制台 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `tabConsole` / `tabDoctor` / `tabConfig` | Button | 互斥单选；激活项加 `.active` 类；切换时对应面板 `display` 切换 |
| `btnInfo` … `btnCompletion` | Button | 点击将对应命令填入 `cmdInput` 并自动触发执行 |
| `cmdInput` | TextField | 回车键触发执行（`KeyDownEvent` 检测 `KeyCode.Return`）；上下方向键导航最近 20 条历史 |
| `runBtn` | Button | 输入为空时禁用；执行中变为「执行中…」并禁用；完成后恢复 |
| `jsonToggle` | Toggle | 追加 `--json` 到执行命令；与 `--quiet`/`--plain` 可共存 |
| `quietToggle` | Toggle | 追加 `--quiet`；选中时输出区仅显示错误 |
| `plainToggle` | Toggle | 追加 `--plain`；选中时输出无表格框线 |
| `outputLabel` | Label | 等宽字体（`JetBrainsMono-Regular SDF`）；ANSI 颜色码解析为内联色彩标签；不可编辑 |
| `clearOutputBtn` | Button | 清空 `outputLabel.text` 并将 `outputScroll` 滚到顶部 |

### 3.2 诊断 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `runDoctorBtn` | Button | 执行 `gate doctor` 的等效逻辑；执行中禁用并显示「检查中…」 |
| `lastCheckTime` | Label | 格式：`上次检查：HH:mm:ss`；灰色 `#64748b`；11px |
| `checkIcon` | Label | `✓`（绿 `#22c55e`）/ `✗`（红 `#f87171`）/ `⚠`（黄 `#f59e0b`）；16px 等宽字体 |
| `checkName` | Label | 检查项名称；白色 `#e2e8f0`；12px 粗体 |
| `checkDesc` | Label | 错误原因或通过说明；灰色 `#94a3b8`；11px |
| `fixBtn` | Button | 仅可自动修复的项显示（如「安装 Shell Hook」、「创建数据目录」）；点击后执行修复并刷新 |

诊断项与 `gate doctor` 8 项检查一一对应：

| # | `checkName` 文本 | 可自动修复 | 修复动作 |
|---|-----------------|-----------|----------|
| 1 | Gate 版本 | 否 | — |
| 2 | 全局代理状态 | 否 | — |
| 3 | Shell Hook | **是** | 调用 `gate install-shell-hook` |
| 4 | 工具加载 | 否 | — |
| 5 | 插件格式 | 否 | — |
| 6 | 数据目录权限 | **是** | 创建 DataDir 目录 |
| 7 | 默认预设 | 否 | — |
| 8 | 连通性 | 否 | — |

### 3.3 全局配置 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `configKey` | Label | 配置键名；蓝色 `#4f8ef7`；`JetBrainsMono-Regular` 字体；11px |
| `configDesc` | Label | 说明文字；灰色 `#64748b`；11px |
| `configToggle` | Toggle | bool 类型字段（`autoSaveHistory`、`colorEnabled`）；即时绑定值，不自动保存 |
| `configInt` | IntegerField | int 类型字段（`maxHistoryCount`、`defaultTimeout`）；实时校验 > 0，非法时红框 |
| `configStr` | TextField | string 类型字段（`defaultTestUrl`、`defaultPreset`、`pluginRegistryUrl`）；空字符串合法 |
| `saveConfigBtn` | Button | 将所有字段变更序列化到 `{DataDir}/config.json`；成功后 `ShowFeedback` |
| `resetConfigBtn` | Button | 弹 `ConfirmDialog`；确认后将所有字段重置为 `GlobalConfig` 默认值并保存 |

配置项渲染映射（与 `GlobalConfig` 字段对应）：

| 字段名 | 控件类型 | 说明文字 | 默认值 |
|--------|---------|---------|-------|
| `autoSaveHistory` | Toggle | 自动保存代理历史 | `true` |
| `maxHistoryCount` | IntegerField | 历史记录最大条数 | `20` |
| `defaultTestUrl` | TextField | 默认测试 URL | `https://www.google.com` |
| `defaultTimeout` | IntegerField | 默认超时（ms） | `10000` |
| `defaultPreset` | TextField | 默认预设名（空 = 不设置） | `""` |
| `pluginRegistryUrl` | TextField | 社区插件索引 URL（P1） | `""` |
| `colorEnabled` | Toggle | 启用彩色 CLI 输出 | `true` |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/ConsolePanel.uxml");
    tree.CloneTree(rootVisualElement);

    // 标签页
    _tabConsole   = rootVisualElement.Q<Button>("tabConsole");
    _tabDoctor    = rootVisualElement.Q<Button>("tabDoctor");
    _tabConfig    = rootVisualElement.Q<Button>("tabConfig");
    _panelConsole = rootVisualElement.Q<VisualElement>("panelConsole");
    _panelDoctor  = rootVisualElement.Q<VisualElement>("panelDoctor");
    _panelConfig  = rootVisualElement.Q<VisualElement>("panelConfig");

    _tabConsole.RegisterCallback<ClickEvent>(_ => SwitchTab(0));
    _tabDoctor .RegisterCallback<ClickEvent>(_ => SwitchTab(1));
    _tabConfig .RegisterCallback<ClickEvent>(_ => SwitchTab(2));

    // 命令控制台
    _cmdInput     = rootVisualElement.Q<TextField>("cmdInput");
    _runBtn       = rootVisualElement.Q<Button>("runBtn");
    _outputLabel  = rootVisualElement.Q<Label>("outputLabel");
    _outputScroll = rootVisualElement.Q<ScrollView>("outputScroll");

    _cmdInput.RegisterCallback<KeyDownEvent>(OnCmdKeyDown);
    _cmdInput.RegisterValueChangedCallback(evt =>
        _runBtn.SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue)));
    _runBtn.RegisterCallback<ClickEvent>(OnRunClicked);
    rootVisualElement.Q<Button>("clearOutputBtn")
        .RegisterCallback<ClickEvent>(_ => { _outputLabel.text = ""; });

    // 快捷按钮
    BindQuickBtn("btnInfo",       "info");
    BindQuickBtn("btnEnv",        "env");
    BindQuickBtn("btnHistory",    "history");
    BindQuickBtn("btnWizard",     "wizard");
    BindQuickBtn("btnDoctor",     "doctor");
    BindQuickBtn("btnCompletion", "completion pwsh");

    // 诊断
    rootVisualElement.Q<Button>("runDoctorBtn")
        .RegisterCallback<ClickEvent>(_ => RunDoctorAsync());

    // 全局配置
    rootVisualElement.Q<Button>("saveConfigBtn")
        .RegisterCallback<ClickEvent>(OnSaveConfig);
    rootVisualElement.Q<Button>("resetConfigBtn")
        .RegisterCallback<ClickEvent>(OnResetConfig);

    Refresh();
}

private void BindQuickBtn(string btnName, string cmd) {
    rootVisualElement.Q<Button>(btnName)?.RegisterCallback<ClickEvent>(_ => {
        _cmdInput.value = cmd;
        OnRunClicked(null);
    });
    _registeredBtns.Add(btnName);
}
```

### 4.2 标签页切换

```csharp
private int _activeTab = 0;

private void SwitchTab(int idx) {
    _activeTab = idx;
    _panelConsole.style.display = idx == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    _panelDoctor .style.display = idx == 1 ? DisplayStyle.Flex : DisplayStyle.None;
    _panelConfig .style.display = idx == 2 ? DisplayStyle.Flex : DisplayStyle.None;

    _tabConsole.EnableInClassList("active", idx == 0);
    _tabDoctor .EnableInClassList("active", idx == 1);
    _tabConfig .EnableInClassList("active", idx == 2);

    if (idx == 1) RunDoctorAsync();
    if (idx == 2) RebuildConfigPanel();
}
```

### 4.3 命令执行（异步）

```csharp
private async void OnRunClicked(ClickEvent evt) {
    var rawCmd = _cmdInput.value.Trim();
    if (string.IsNullOrEmpty(rawCmd)) return;

    // 组装全局标志
    var flags = new List<string>();
    if (_jsonToggle.value)  flags.Add("--json");
    if (_quietToggle.value) flags.Add("--quiet");
    if (_plainToggle.value) flags.Add("--plain");
    var fullArgs = rawCmd + (flags.Count > 0 ? " " + string.Join(" ", flags) : "");

    // 记录历史
    _cmdHistory.Insert(0, rawCmd);
    if (_cmdHistory.Count > 20) _cmdHistory.RemoveAt(20);
    _historyIndex = -1;

    _runBtn.SetEnabled(false);
    _runBtn.text = "执行中…";
    AppendOutput($"> gate {fullArgs}\n", "#94a3b8");

    try {
        // 在后台进程中执行 gate CLI（避免阻塞 Unity 主线程）
        var (stdout, stderr, exitCode) = await Task.Run(() =>
            GateCliRunner.Run(fullArgs));

        if (!string.IsNullOrEmpty(stdout)) AppendOutput(stdout, "#e2e8f0");
        if (!string.IsNullOrEmpty(stderr)) AppendOutput(stderr, "#f87171");
        AppendOutput($"[退出码: {exitCode}]\n", exitCode == 0 ? "#22c55e" : "#f87171");
    } catch (Exception ex) {
        AppendOutput($"执行失败：{ex.Message}\n", "#f87171");
    } finally {
        _runBtn.SetEnabled(true);
        _runBtn.text = "执行";
        // 滚动到底部
        _outputScroll.scrollOffset = new Vector2(0, float.MaxValue);
    }
}

private void AppendOutput(string text, string hexColor) {
    // UIToolkit Label 不支持富文本，通过追加新 Label 实现多色输出
    var lbl = new Label(text);
    lbl.style.color = new StyleColor(ColorUtility.TryParseHtmlString(hexColor, out var c) ? c : Color.white);
    lbl.AddToClassList("output-line");
    _outputScroll.Add(lbl);
}
```

### 4.4 键盘导航（方向键历史）

```csharp
private void OnCmdKeyDown(KeyDownEvent evt) {
    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
        OnRunClicked(null);
        evt.StopPropagation();
    } else if (evt.keyCode == KeyCode.UpArrow && _cmdHistory.Count > 0) {
        _historyIndex = Mathf.Min(_historyIndex + 1, _cmdHistory.Count - 1);
        _cmdInput.SetValueWithoutNotify(_cmdHistory[_historyIndex]);
        evt.StopPropagation();
    } else if (evt.keyCode == KeyCode.DownArrow) {
        _historyIndex = Mathf.Max(_historyIndex - 1, -1);
        _cmdInput.SetValueWithoutNotify(_historyIndex < 0 ? "" : _cmdHistory[_historyIndex]);
        evt.StopPropagation();
    }
}
```

### 4.5 诊断执行（异步）

```csharp
private async void RunDoctorAsync() {
    _runDoctorBtn.SetEnabled(false);
    _runDoctorBtn.text = "检查中…";
    _doctorScroll.Clear();

    try {
        var results = await Task.Run(() => DoctorRunner.RunAll(
            _envVarManager, _toolRegistry, _pluginManager, _profileManager));

        foreach (var item in results)
            _doctorScroll.Add(BuildDoctorRow(item));

        _lastCheckTime.text = $"上次检查：{DateTime.Now:HH:mm:ss}";
    } finally {
        _runDoctorBtn.SetEnabled(true);
        _runDoctorBtn.text = "重新检查";
    }
}

private VisualElement BuildDoctorRow(DoctorCheckResult item) {
    var row = new VisualElement();
    row.AddToClassList("doctor-row");

    var icon = new Label(item.Passed ? "✓" : (item.CanAutoFix ? "⚠" : "✗"));
    icon.AddToClassList("check-icon");
    icon.style.color = item.Passed
        ? new Color(0.13f, 0.77f, 0.37f)     // #22c55e
        : item.CanAutoFix
            ? new Color(0.96f, 0.62f, 0.04f) // #f59e0b
            : new Color(0.97f, 0.44f, 0.44f);// #f87171

    var content = new VisualElement();
    content.AddToClassList("check-content");
    content.Add(new Label(item.Name) { name = "checkName" });
    if (!string.IsNullOrEmpty(item.Description))
        content.Add(new Label(item.Description) { name = "checkDesc" });

    row.Add(icon);
    row.Add(content);

    if (!item.Passed && item.CanAutoFix) {
        var fixBtn = new Button(() => AutoFixAsync(item)) { text = "修复" };
        fixBtn.AddToClassList("btn-xs-primary");
        row.Add(fixBtn);
    }
    return row;
}
```

### 4.6 全局配置面板渲染

```csharp
private void RebuildConfigPanel() {
    _configScroll.Clear();
    var cfg = _globalConfigManager.Load();
    // 按字段类型动态渲染
    AddConfigRow("autoSaveHistory",   "自动保存代理历史",    cfg.AutoSaveHistory,   v => cfg.AutoSaveHistory   = v);
    AddConfigRow("maxHistoryCount",   "历史记录最大条数",    cfg.MaxHistoryCount,   v => cfg.MaxHistoryCount   = v);
    AddConfigRow("defaultTestUrl",    "默认测试 URL",        cfg.DefaultTestUrl,    v => cfg.DefaultTestUrl    = v);
    AddConfigRow("defaultTimeout",    "默认超时 (ms)",       cfg.DefaultTimeout,    v => cfg.DefaultTimeout    = v);
    AddConfigRow("defaultPreset",     "默认预设名",          cfg.DefaultPreset,     v => cfg.DefaultPreset     = v);
    AddConfigRow("pluginRegistryUrl", "社区插件索引 URL (P1)",cfg.PluginRegistryUrl, v => cfg.PluginRegistryUrl = v);
    AddConfigRow("colorEnabled",      "启用彩色 CLI 输出",   cfg.ColorEnabled,      v => cfg.ColorEnabled      = v);
    _pendingConfig = cfg;
}
```

### 4.7 Dispose — 事件注销

```csharp
public override void Dispose() {
    _tabConsole ?.UnregisterCallback<ClickEvent>(_ => SwitchTab(0));
    _tabDoctor  ?.UnregisterCallback<ClickEvent>(_ => SwitchTab(1));
    _tabConfig  ?.UnregisterCallback<ClickEvent>(_ => SwitchTab(2));
    _cmdInput   ?.UnregisterCallback<KeyDownEvent>(OnCmdKeyDown);
    _cmdInput   ?.UnregisterValueChangedCallback(null);
    _runBtn     ?.UnregisterCallback<ClickEvent>(OnRunClicked);
    _runDoctorBtn?.UnregisterCallback<ClickEvent>(_ => RunDoctorAsync());
    _saveConfigBtn ?.UnregisterCallback<ClickEvent>(OnSaveConfig);
    _resetConfigBtn?.UnregisterCallback<ClickEvent>(OnResetConfig);
    foreach (var name in _registeredBtns)
        rootVisualElement.Q<Button>(name)?.UnregisterCallback<ClickEvent>(null);
}
```

---

## 5. 响应式适配

| 窗口宽度 | 布局行为 |
|----------|---------|
| ≥ 600px | 快捷按钮三列网格；命令输入行水平排列 |
| 400–599px | 快捷按钮两列；输出格式 Toggle 换行 |
| < 400px | 快捷按钮单列；输入框与执行按钮纵向堆叠 |

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|----------|
| `.console-panel` | `padding: 0; flex-direction: column; flex-grow: 1` |
| `.tab-bar` | `flex-direction: row; background: #0a0e18; padding: 0 16px; border-bottom: 1px #1e293b` |
| `.tab-btn` | `background: transparent; color: #64748b; padding: 8px 14px; border-width: 0; border-radius: 0; font-size: 12px` |
| `.tab-btn.active` | `color: #4f8ef7; border-bottom: 2px solid #4f8ef7` |
| `.quick-cmds-card` | `padding: 12px 16px; background: #111827; border-bottom: 1px #1e293b` |
| `.quick-grid` | `flex-direction: row; flex-wrap: wrap; gap: 6px; margin-top: 8px` |
| `.quick-btn` | `background: #1e293b; color: #94a3b8; border-radius: 4px; padding: 4px 10px; font-size: 11px; font-family: JetBrainsMono` |
| `.quick-btn:hover` | `background: #1e3a5f; color: #4f8ef7` |
| `.cmd-input-card` | `padding: 12px 16px; background: #111827; border-bottom: 1px #1e293b` |
| `.cmd-input-row` | `flex-direction: row; align-items: center; margin-bottom: 6px` |
| `.cmd-prefix` | `color: #4f8ef7; font-size: 12px; font-family: JetBrainsMono; margin-right: 6px; flex-shrink: 0` |
| `.cmd-field` | `flex-grow: 1; height: 32px; font-family: JetBrainsMono; font-size: 12px; background: #0a0e18` |
| `.format-row` | `flex-direction: row; gap: 12px; align-items: center` |
| `.format-toggle` | `font-size: 11px; color: #64748b` |
| `.output-card` | `flex-grow: 1; padding: 12px 16px; background: #0a0e18` |
| `.output-scroll` | `flex-grow: 1` |
| `.output-text` | `font-family: JetBrainsMono; font-size: 11px; white-space: normal` |
| `.output-line` | `font-family: JetBrainsMono; font-size: 11px; white-space: pre-wrap` |
| `.doctor-row` | `flex-direction: row; align-items: flex-start; padding: 8px 0; border-bottom: 1px #1e293b` |
| `.check-icon` | `font-size: 14px; font-family: JetBrainsMono; width: 24px; flex-shrink: 0` |
| `.check-content` | `flex-grow: 1` |
| `.check-name` | `color: #e2e8f0; font-size: 12px; -unity-font-style: bold` |
| `.check-desc` | `color: #94a3b8; font-size: 11px; margin-top: 2px` |
| `.config-row` | `flex-direction: row; align-items: center; padding: 8px 0; border-bottom: 1px #1e293b` |
| `.config-info` | `flex-grow: 1` |
| `.config-key` | `color: #4f8ef7; font-size: 11px; font-family: JetBrainsMono` |
| `.config-desc` | `color: #64748b; font-size: 10px; margin-top: 2px` |

**字体**：命令输出 / 键名使用 `JetBrainsMono-Regular SDF.asset`，其余使用 `SourceHanSansSC-Regular SDF.asset`。
