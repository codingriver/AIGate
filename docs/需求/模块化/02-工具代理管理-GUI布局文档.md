# 02 工具代理管理 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./02-工具代理管理-需求文档.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----|
| 面板名称 | 工具代理面板（Tools Panel） |
| 控制器类 | `ToolsPanelController` |
| UXML 文件 | `Assets/UI/AIGate/ToolsPanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/ToolsPanel.uss` |
| 导航项 | `[T] 工具代理`（侧边栏第二项） |
| 功能 | 按工具分类浏览、搜索，逐个或批量设置/清除工具代理 |

---

## 2. 布局结构（层级关系）

```
ToolsPanel (VisualElement .tools-panel)
├── 顶部工具栏 (VisualElement .toolbar)
│   ├── 搜索框 (TextField name=searchInput placeholder="搜索工具..." .search-input)
│   ├── 过滤按钮组 (VisualElement .filter-group)
│   │   ├── Button name=btnAll     text="全部"     .filter-btn .active
│   │   ├── Button name=btnInstalled text="已安装"  .filter-btn
│   │   └── Button name=btnConfigured text="已配置" .filter-btn
│   └── 批量操作按钮 (VisualElement .batch-actions)
│       ├── TextField name=batchProxyInput placeholder="批量代理地址" .input-sm
│       ├── Button name=batchApplyBtn  text="批量应用" .btn-primary
│       └── Button name=batchClearBtn  text="批量清除" .btn-danger
│
├── 工具列表 (ScrollView name=toolsScrollView .tools-list)
│   └── [foreach 分类]
│       ├── 分类标题行 (VisualElement .category-header)
│       │   ├── Label text="{分类名}" .category-title
│       │   └── Label text="{N}/{Total}" .category-count
│       └── [foreach 工具]
│           └── 工具行 (VisualElement .tool-row)
│               ├── 工具图标 (VisualElement .tool-icon)
│               ├── 工具信息列 (VisualElement .tool-info)
│               │   ├── Label name=toolNameLabel .tool-name
│               │   └── Label name=toolProxyLabel .tool-proxy-value
│               ├── 状态徽章 (Label name=statusBadge .badge)
│               ├── 代理输入框 (TextField name=toolProxyInput .input-sm)
│               └── 操作按钮组 (VisualElement .tool-actions)
│                   ├── Button name=toolApplyBtn text="设置" .btn-xs-primary
│                   └── Button name=toolClearBtn text="清除" .btn-xs-danger
│
└── 底部状态栏 (VisualElement .status-bar)
    ├── Label name=statsLabel .stats-label
    └── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性详细说明

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `searchInput` | TextField | 实时过滤工具列表（debounce 200ms）；清空时恢复全部 |
| `btnAll` / `btnInstalled` / `btnConfigured` | Button | 单选过滤；当前激活按钮加 `.active` 类；三者互斥 |
| `batchProxyInput` | TextField | 批量操作代理地址；格式校验同全局代理规则 |
| `batchApplyBtn` | Button | 对当前过滤结果中所有已安装工具执行批量设置 |
| `batchClearBtn` | Button | 对当前过滤结果中所有已安装工具执行批量清除 |
| `toolsScrollView` | ScrollView | 虚拟化滚动（仅渲染可见行），支持数百工具流畅滚动 |
| `statusBadge` | Label | `已安装`（绿 `#22c55e`）/ `未安装`（灰 `#64748b`）/ `已配置`（黄 `#f59e0b`） |
| `toolProxyInput` | TextField | 行内代理地址输入，placeholder 显示当前已配置值 |
| `toolApplyBtn` | Button | 调用 `OnToolApplyClicked(toolName)` |
| `toolClearBtn` | Button | 未配置代理时禁用 |
| `statsLabel` | Label | 显示「已安装 N 个 · 已配置代理 M 个」 |
| `feedbackLabel` | Label | 操作反馈，3 秒自动隐藏 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/ToolsPanel.uxml");
    tree.CloneTree(rootVisualElement);

    _searchInput   = rootVisualElement.Q<TextField>("searchInput");
    _toolsScroll   = rootVisualElement.Q<ScrollView>("toolsScrollView");
    _feedbackLabel = rootVisualElement.Q<Label>("feedbackLabel");
    _statsLabel    = rootVisualElement.Q<Label>("statsLabel");

    _searchInput.RegisterValueChangedCallback(OnSearchChanged);
    rootVisualElement.Q<Button>("btnAll").RegisterCallback<ClickEvent>(_ => SetFilter(FilterMode.All));
    rootVisualElement.Q<Button>("btnInstalled").RegisterCallback<ClickEvent>(_ => SetFilter(FilterMode.Installed));
    rootVisualElement.Q<Button>("btnConfigured").RegisterCallback<ClickEvent>(_ => SetFilter(FilterMode.Configured));
    rootVisualElement.Q<Button>("batchApplyBtn").RegisterCallback<ClickEvent>(OnBatchApply);
    rootVisualElement.Q<Button>("batchClearBtn").RegisterCallback<ClickEvent>(OnBatchClear);

    Refresh();
}
```

### 4.2 Refresh() — 重建工具列表

```csharp
public override void Refresh() {
    _allTools = _toolRegistry.GetAll();
    RebuildList();
    UpdateStats();
}

private void RebuildList() {
    _toolsScroll.Clear();
    var filtered = ApplyFilter(_allTools, _currentFilter, _searchQuery);
    var grouped  = filtered.GroupBy(t => t.Category).OrderBy(g => g.Key);
    foreach (var group in grouped) {
        _toolsScroll.Add(BuildCategoryHeader(group.Key, group.Count()));
        foreach (var tool in group.OrderBy(t => t.Name))
            _toolsScroll.Add(BuildToolRow(tool));
    }
}
```

### 4.3 搜索过滤（debounce 200ms）

```csharp
private CancellationTokenSource _searchCts;

private async void OnSearchChanged(ChangeEvent<string> evt) {
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    try {
        await Task.Delay(200, _searchCts.Token); // debounce
        _searchQuery = evt.newValue.Trim();
        RebuildList();
    } catch (TaskCanceledException) { }
}
```

### 4.4 批量应用（异步）

```csharp
private async void OnBatchApply(ClickEvent evt) {
    var proxy = rootVisualElement.Q<TextField>("batchProxyInput").value;
    if (!ProxyValidator.IsValid(proxy)) { ShowFeedback("代理地址格式错误", false); return; }

    var targets = ApplyFilter(_allTools, _currentFilter, _searchQuery)
                    .Where(t => t.IsInstalled()).ToList();

    int ok = 0, fail = 0;
    foreach (var tool in targets) {
        var success = await Task.Run(() => tool.SetProxy(proxy));
        if (success) ok++; else fail++;
    }
    Refresh();
    ShowFeedback($"完成：成功 {ok} 个，失败 {fail} 个", fail == 0);
}
```

### 4.5 Dispose — 事件注销

```csharp
public void Dispose() {
    _searchCts?.Cancel();
    _searchInput?.UnregisterValueChangedCallback(OnSearchChanged);
}
```

---

## 5. 响应式适配

| 宽度 | 布局行为 |
|------|----------|
| ≥ 600px | 工具行：图标 + 信息列 + 徽章 + 输入框 + 按钮组，水平排列 |
| 400–599px | 输入框与按钮组换行到第二行 |
| < 400px | 按钮组纵向堆叠 |

批量操作区在 < 400px 时换行，输入框宽度 100%。

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|----------|
| `.tools-panel` | padding: 0; flex-direction: column; flex-grow: 1 |
| `.toolbar` | padding: 10px 16px; background: `#111827`; flex-direction: row; align-items: center; border-bottom: 1px `#1e293b` |
| `.search-input` | flex-grow: 1; max-width: 200px; height: 30px; font-size: 12px |
| `.filter-group` | flex-direction: row; margin-left: 8px |
| `.filter-btn` | background: transparent; color: `#64748b`; border-radius: 4px; padding: 4px 10px; font-size: 11px |
| `.filter-btn.active` | background: `#1e3a5f`; color: `#4f8ef7` |
| `.category-header` | padding: 6px 16px; background: `#0d1117`; flex-direction: row; align-items: center |
| `.category-title` | color: `#94a3b8`; font-size: 11px; -unity-font-style: bold |
| `.tool-row` | padding: 8px 16px; border-bottom: 1px `#111827`; flex-direction: row; align-items: center |
| `.tool-row:hover` | background: `#111827` |
| `.tool-name` | color: `#e2e8f0`; font-size: 12px; -unity-font-style: bold |
| `.tool-proxy-value` | color: `#64748b`; font-size: 11px |
| `.badge` | font-size: 10px; padding: 2px 6px; border-radius: 10px; margin-left: 8px |
| `.input-sm` | height: 28px; font-size: 11px; flex-grow: 1; max-width: 180px |
| `.btn-xs-primary` | background: `#4f8ef7`; color: white; height: 26px; padding: 0 10px; font-size: 11px |
| `.btn-xs-danger` | color: `#f87171`; border: 1px `#f87171`; height: 26px; padding: 0 10px; font-size: 11px |
| `.status-bar` | padding: 6px 16px; background: `#0d1117`; border-top: 1px `#1e293b` |
