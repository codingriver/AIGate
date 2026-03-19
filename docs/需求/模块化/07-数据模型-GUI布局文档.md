# 07 数据模型 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./07-数据模型-需求文档.md) | [Unity GUI 规范](./09-Unity-GUI规范.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----||
| 面板名称 | 数据浏览面板（Data Panel） |
| 控制器类 | `DataPanelController` |
| UXML 文件 | `Assets/UI/AIGate/DataPanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/DataPanel.uss` |
| 导航项 | `[D] 数据`（侧边栏第七项，可选显示） |
| 功能 | 只读浏览 Gate 所有持久化数据结构；支持查看存储路径、原始 JSON、历史记录及工具路径配置 |

> **设计说明**：数据模型本身无交互写入逻辑（写入由各功能模块负责），此面板定位为**开发调试与数据审查工具**，所有操作均为只读展示，仅提供「在文件管理器中打开」和「复制路径」两类辅助操作。

---

## 2. 布局结构（层级关系）

```
DataPanel (VisualElement .data-panel)
├── 标签页导航 (VisualElement .tab-bar)
│   ├── Button name=tabPaths    text="存储路径"   .tab-btn .active
│   ├── Button name=tabConfig   text="全局配置"   .tab-btn
│   ├── Button name=tabHistory  text="代理历史"   .tab-btn
│   ├── Button name=tabProfiles text="预设快照"   .tab-btn
│   └── Button name=tabPaths2   text="工具路径"   .tab-btn
│
├── [Tab 0: 存储路径] (VisualElement name=panelPaths)
│   ├── Label "Gate 数据目录结构" .card-title
│   └── ScrollView .paths-list
│       └── [foreach GatePaths 静态字段]
│           └── 路径行 (.path-row)
│               ├── Label name=pathKey   .path-key
│               ├── Label name=pathValue .path-value
│               ├── Label name=existsBadge .badge（存在/不存在）
│               ├── Button name=openBtn  text="打开" .btn-xs-secondary
│               └── Button name=copyBtn  text="复制" .btn-xs-secondary
│
├── [Tab 1: 全局配置] (VisualElement name=panelConfig display=none)
│   ├── Label "config.json 当前内容" .card-title
│   ├── Button name=refreshConfigBtn text="刷新" .btn-secondary
│   └── ScrollView .json-viewer
│       └── Label name=configJson .json-text
│
├── [Tab 2: 代理历史] (VisualElement name=panelHistory display=none)
│   ├── Label "history.json（最近记录）" .card-title
│   └── ScrollView .history-list
│       └── [foreach HistoryEntry]
│           └── 历史行 (.history-row)
│               ├── Label name=histIndex  .hist-index
│               ├── Label name=histProxy  .hist-proxy
│               └── Label name=histTime   .hist-time
│
├── [Tab 3: 预设快照] (VisualElement name=panelProfiles display=none)
│   ├── Label "profiles/ 目录" .card-title
│   └── ScrollView .profiles-list
│       └── [foreach Profile 文件]
│           └── 预设行 (.profile-row)
│               ├── Label name=profileName   .profile-name
│               ├── Label name=profileDefault .badge-default
│               ├── Label name=profileTools  .profile-meta
│               └── Button name=showJsonBtn text="查看 JSON" .btn-xs-secondary
│
└── [Tab 4: 工具路径] (VisualElement name=panelToolPaths display=none)
    ├── Label "tool-paths.json（自定义路径）" .card-title
    └── ScrollView .toolpaths-list
        └── [foreach ToolPathEntry（非空项）]
            └── 工具路径行 (.toolpath-row)
                ├── Label name=tpToolName  .tp-tool
                ├── Label name=tpExecPath  .tp-path
                └── Label name=tpConfigPath .tp-path
```

---

## 3. 控件属性详细说明

### 3.1 存储路径 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `pathKey` | Label | 字段名（如 `DataDir`、`HistoryFile`）；蓝色 `#4f8ef7`；等宽字体 |
| `pathValue` | Label | 展开 `~` 和 `%APPDATA%` 后的绝对路径；灰色 `#94a3b8`；11px；超长省略 |
| `existsBadge` | Label | 路径存在时显示「存在」（绿 `#22c55e`）；不存在时显示「不存在」（红 `#f87171`） |
| `openBtn` | Button | 调用 `EditorUtility.RevealInFinder(path)` 在系统文件管理器中定位 |
| `copyBtn` | Button | `GUIUtility.systemCopyBuffer = path`；点击后 3 秒内按钮文字变为「已复制」 |

存储路径渲染列表（与 `GatePaths` 对应）：

| 字段名 | 说明 |
|--------|------|
| `DataDir` | Gate 数据根目录 |
| `HistoryFile` | 代理历史文件 (`history.json`) |
| `ConfigFile` | 全局配置文件 (`config.json`) |
| `ToolPathsFile` | 工具路径配置 (`tool-paths.json`) |
| `ProfilesDir` | 预设目录 (`profiles/`) |
| `PluginsDir` | 插件目录 (`plugins/`) |
| `BackupsDir` | 备份目录 (`backups/`)，P1 功能，不存在时标红 |

### 3.2 全局配置 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `configJson` | Label | 格式化 JSON 文本（`WriteIndented = true`）；等宽字体；只读 |
| `refreshConfigBtn` | Button | 重新从磁盘读取 `config.json` 并更新显示 |

### 3.3 代理历史 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `histIndex` | Label | 序号（1-based）；灰色；40px 固定宽度 |
| `histProxy` | Label | 代理地址；黄色 `#f59e0b`；等宽字体；超长省略 |
| `histTime` | Label | `usedAt` 本地时间格式 `MM-dd HH:mm`；灰色；右对齐 |

### 3.4 预设快照 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `profileName` | Label | 预设名称；白色粗体 |
| `profileDefault` | Label | 仅默认预设显示「默认」徽章（蓝色）；其余 `display:none` |
| `profileTools` | Label | 格式：`{N} 个工具代理 · 更新于 MM-dd`；灰色 11px |
| `showJsonBtn` | Button | 点击后在 `panelConfig` 的 JSON 查看器中显示该预设的完整 JSON（切换到 Tab 1） |

### 3.5 工具路径 Tab

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `tpToolName` | Label | 工具名；蓝色 `#4f8ef7`；90px 固定宽度 |
| `tpExecPath` | Label | 自定义可执行路径；灰色；「exec: 」前缀；若为 null 则不渲染该行 |
| `tpConfigPath` | Label | 自定义配置路径；灰色；「config: 」前缀；若为 null 则不渲染 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/DataPanel.uxml");
    tree.CloneTree(rootVisualElement);

    // 标签页
    BindTab("tabPaths",    0);
    BindTab("tabConfig",   1);
    BindTab("tabHistory",  2);
    BindTab("tabProfiles", 3);
    BindTab("tabPaths2",   4);

    Refresh();
}

private void SwitchTab(int idx) {
    string[] panels = { "panelPaths", "panelConfig", "panelHistory", "panelProfiles", "panelToolPaths" };
    for (int i = 0; i < panels.Length; i++)
        rootVisualElement.Q<VisualElement>(panels[i]).style.display =
            i == idx ? DisplayStyle.Flex : DisplayStyle.None;

    string[] tabs = { "tabPaths", "tabConfig", "tabHistory", "tabProfiles", "tabPaths2" };
    for (int i = 0; i < tabs.Length; i++)
        rootVisualElement.Q<Button>(tabs[i]).EnableInClassList("active", i == idx);

    // 切换时刷新当前 Tab 数据
    switch (idx) {
        case 0: RebuildPathsTab(); break;
        case 1: RebuildConfigTab(); break;
        case 2: RebuildHistoryTab(); break;
        case 3: RebuildProfilesTab(); break;
        case 4: RebuildToolPathsTab(); break;
    }
}
```

### 4.2 Refresh()

```csharp
public override void Refresh() {
    // 仅刷新当前激活 Tab
    SwitchTab(_activeTab);
}
```

### 4.3 存储路径 Tab 构建

```csharp
private void RebuildPathsTab() {
    var scroll = rootVisualElement.Q<ScrollView>("panelPaths").Q<ScrollView>();
    scroll.Clear();

    var paths = new[] {
        ("DataDir",       GatePaths.DataDir,       "Gate 数据根目录"),
        ("HistoryFile",   GatePaths.HistoryFile,   "代理历史"),
        ("ConfigFile",    GatePaths.ConfigFile,    "全局配置"),
        ("ToolPathsFile", GatePaths.ToolPathsFile, "工具路径配置"),
        ("ProfilesDir",   GatePaths.ProfilesDir,   "预设目录"),
        ("PluginsDir",    GatePaths.PluginsDir,    "插件目录"),
        ("BackupsDir",    GatePaths.BackupsDir,    "备份目录（P1）"),
    };

    foreach (var (key, path, desc) in paths)
        scroll.Add(BuildPathRow(key, path, desc));
}

private VisualElement BuildPathRow(string key, string path, string desc) {
    var row = new VisualElement();
    row.AddToClassList("path-row");

    row.Add(new Label(key)  { name = "pathKey" });
    row.Add(new Label(path) { name = "pathValue", tooltip = path });

    bool exists = Directory.Exists(path) || File.Exists(path);
    var badge = new Label(exists ? "存在" : "不存在");
    badge.AddToClassList("badge");
    badge.style.color = exists
        ? new Color(0.13f, 0.77f, 0.37f)
        : new Color(0.97f, 0.44f, 0.44f);
    row.Add(badge);

    var openBtn = new Button(() => EditorUtility.RevealInFinder(path)) { text = "打开" };
    openBtn.AddToClassList("btn-xs-secondary");
    openBtn.SetEnabled(exists);
    row.Add(openBtn);

    var copyBtn = new Button();
    copyBtn.text = "复制";
    copyBtn.AddToClassList("btn-xs-secondary");
    copyBtn.RegisterCallback<ClickEvent>(_ => {
        GUIUtility.systemCopyBuffer = path;
        copyBtn.text = "已复制";
        rootVisualElement.schedule.Execute(() => copyBtn.text = "复制").StartingIn(3000);
    });
    row.Add(copyBtn);

    return row;
}
```

### 4.4 JSON 查看器

```csharp
private void RebuildConfigTab() {
    try {
        var raw = File.Exists(GatePaths.ConfigFile)
            ? File.ReadAllText(GatePaths.ConfigFile)
            : "(文件不存在)";
        rootVisualElement.Q<Label>("configJson").text = raw;
    } catch (Exception ex) {
        rootVisualElement.Q<Label>("configJson").text = $"读取失败：{ex.Message}";
    }
}
```

### 4.5 Dispose

```csharp
public override void Dispose() {
    // 注销所有标签页按钮 RegisterCallback
    foreach (var name in new[]{ "tabPaths","tabConfig","tabHistory","tabProfiles","tabPaths2" })
        rootVisualElement.Q<Button>(name)?.UnregisterCallback<ClickEvent>(null);
}
```

---

## 5. 响应式适配

| 窗口宽度 | 布局行为 |
|----------|---------|
| ≥ 500px | 路径行：键名 + 路径 + 徽章 + 按钮，水平排列 |
| 300–499px | 路径换行到第二行，宽度 100% |
| < 300px | 操作按钮折叠隐藏，仅显示键名和路径 |

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|---------|
| `.data-panel` | `padding: 0; flex-direction: column; flex-grow: 1` |
| `.tab-bar` | `flex-direction: row; background: #0a0e18; padding: 0 16px; border-bottom: 1px #1e293b` |
| `.tab-btn` | `background: transparent; color: #64748b; padding: 8px 14px; border-width: 0; font-size: 12px` |
| `.tab-btn.active` | `color: #4f8ef7; border-bottom: 2px solid #4f8ef7` |
| `.paths-list` | `padding: 12px 16px; flex-grow: 1` |
| `.path-row` | `flex-direction: row; align-items: center; padding: 8px 0; border-bottom: 1px #1e293b; gap: 8px` |
| `.path-key` | `color: #4f8ef7; font-size: 11px; font-family: JetBrainsMono; width: 120px; flex-shrink: 0` |
| `.path-value` | `color: #94a3b8; font-size: 11px; flex-grow: 1; overflow: hidden; text-overflow: ellipsis` |
| `.json-viewer` | `padding: 12px 16px; flex-grow: 1` |
| `.json-text` | `font-family: JetBrainsMono; font-size: 11px; color: #e2e8f0; white-space: pre-wrap` |
| `.history-row` | `flex-direction: row; align-items: center; padding: 7px 16px; border-bottom: 1px #1e293b` |
| `.hist-index` | `color: #64748b; font-size: 11px; width: 32px; flex-shrink: 0` |
| `.hist-proxy` | `color: #f59e0b; font-size: 11px; font-family: JetBrainsMono; flex-grow: 1` |
| `.hist-time` | `color: #64748b; font-size: 10px; margin-left: auto` |
| `.profile-row` | `flex-direction: row; align-items: center; padding: 8px 16px; border-bottom: 1px #1e293b; gap: 8px` |
| `.profile-name` | `color: #e2e8f0; font-size: 12px; -unity-font-style: bold; flex-grow: 1` |
| `.profile-meta` | `color: #64748b; font-size: 10px` |
| `.toolpath-row` | `flex-direction: row; flex-wrap: wrap; padding: 8px 16px; border-bottom: 1px #1e293b; gap: 6px` |
| `.tp-tool` | `color: #4f8ef7; font-size: 11px; font-family: JetBrainsMono; width: 90px; flex-shrink: 0` |
| `.tp-path` | `color: #94a3b8; font-size: 11px; font-family: JetBrainsMono` |

**字体**：键名、路径、JSON 内容使用 `JetBrainsMono-Regular SDF.asset`，其余使用 `SourceHanSansSC-Regular SDF.asset`。
