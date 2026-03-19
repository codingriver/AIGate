# 03 预设管理 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./03-预设管理-需求文档.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----|
| 面板名称 | 预设面板（Presets Panel） |
| 控制器类 | `PresetsPanelController` |
| UXML | `Assets/UI/AIGate/PresetsPanel.uxml` |
| USS | `Assets/UI/AIGate/PresetsPanel.uss` |
| 功能 | 保存/加载/管理/导入导出代理预设 |

---

## 2. 布局结构

```
PresetsPanel (VisualElement .presets-panel)
├── 顶部操作栏 (.toolbar)
│   ├── TextField name=newPresetName placeholder="新预设名称" .input-field
│   └── Button name=savePresetBtn text="保存当前配置" .btn-primary
│
├── 预设列表 (ScrollView name=presetScrollView .presets-list)
│   └── [foreach 预设]
│       └── 预设行 (.preset-row)
│           ├── VisualElement .preset-row-main
│           │   ├── Label name=defaultBadge text="默认" .badge-default
│           │   ├── Label name=presetName .preset-name
│           │   ├── Label name=toolCount .preset-meta
│           │   └── Label name=updatedAt .preset-meta
│           └── VisualElement .preset-actions
│               ├── Button name=loadBtn    text="加载"   .btn-xs-primary
│               ├── Button name=exportBtn  text="导出"   .btn-xs-secondary
│               ├── Button name=defaultBtn text="设默认" .btn-xs-secondary
│               └── Button name=deleteBtn  text="删除"   .btn-xs-danger
│
├── 导入区 (.import-section)
│   ├── Label text="导入预设" .section-title
│   ├── TextField name=importPathInput placeholder="选择 .gate-preset.json" .input-field
│   ├── Button name=browseBtn text="浏览..." .btn-secondary
│   └── Button name=importBtn text="导入" .btn-primary
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性

| 控件名 | 类型 | 关键属性 |
|--------|------|----------|
| `newPresetName` | TextField | 实时校验（字母/数字/-/_），不合规时红色边框 |
| `savePresetBtn` | Button | 名称为空或格式错误时禁用 |
| `defaultBadge` | Label | 仅默认预设 `display:flex`，否则 `display:none` |
| `loadBtn` | Button | 调用 `OnLoadClicked(presetName)` |
| `exportBtn` | Button | 打开系统文件保存对话框 |
| `defaultBtn` | Button | 设为默认；已是默认时文字变为「取消默认」 |
| `deleteBtn` | Button | 弹确认对话框；默认预设删除时额外警告 |
| `browseBtn` | Button | 打开系统文件选择对话框，结果填入 `importPathInput` |
| `importBtn` | Button | 路径为空时禁用 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/PresetsPanel.uxml");
    tree.CloneTree(rootVisualElement);
    // Q<> 获取控件引用...
    rootVisualElement.Q<Button>("savePresetBtn").RegisterCallback<ClickEvent>(OnSaveClicked);
    rootVisualElement.Q<Button>("importBtn").RegisterCallback<ClickEvent>(OnImportClicked);
    rootVisualElement.Q<Button>("browseBtn").RegisterCallback<ClickEvent>(OnBrowseClicked);
    Refresh();
}
```

### 4.2 Refresh()

```csharp
public override void Refresh() {
    _presetScroll.Clear();
    var presets = _profileManager.ListAll()
        .OrderByDescending(p => p.IsDefault)
        .ThenByDescending(p => p.UpdatedAt);
    foreach (var p in presets)
        _presetScroll.Add(BuildPresetRow(p));
}
```

### 4.3 保存预设

```csharp
private async void OnSaveClicked(ClickEvent evt) {
    var name = _newPresetName.value.Trim();
    if (_profileManager.Exists(name)) {
        bool confirmed = await ConfirmDialog.Show($"覆盖预设 '{name}'?");
        if (!confirmed) return;
    }
    await Task.Run(() => _profileManager.SaveCurrent(name));
    _newPresetName.value = "";
    Refresh();
    ShowFeedback($"预设 '{name}' 已保存", true);
}
```

### 4.4 加载预设

```csharp
private async void OnLoadClicked(string presetName) {
    var result = await Task.Run(() => _profileManager.Load(presetName));
    Refresh();
    ShowFeedback(
        result.Failed == 0
            ? $"预设 '{presetName}' 已加载"
            : $"预设已加载，{result.Failed} 个工具失败",
        result.Failed == 0);
}
```

### 4.5 删除预设（确认对话框）

```csharp
private async void OnDeleteClicked(string presetName) {
    bool confirmed = await ConfirmDialog.Show($"确认删除预设 '{presetName}'?");
    if (!confirmed) return;
    await Task.Run(() => _profileManager.Delete(presetName));
    Refresh();
    ShowFeedback($"预设 '{presetName}' 已删除", true);
}
```

### 4.6 导入预设

```csharp
private async void OnImportClicked(ClickEvent evt) {
    var path = _importPathInput.value.Trim();
    if (!File.Exists(path)) { ShowFeedback("文件不存在", false); return; }
    try {
        await Task.Run(() => _profileManager.Import(path));
        Refresh();
        ShowFeedback("预设导入成功", true);
    } catch (SchemaValidationException ex) {
        ShowFeedback($"导入失败：{ex.Message}", false);
    }
}
```

### 4.7 Dispose

```csharp
public void Dispose() {
    // UnregisterCallback 所有已注册事件
}
```

---

## 5. 响应式适配

| 宽度 | 布局行为 |
|------|----------|
| ≥ 500px | 预设行操作按钮横排 |
| < 500px | 操作按钮折叠为「…」下拉菜单 |

---

## 6. 样式规范

| USS 类 | 关键样式 |
|--------|----------|
| `.presets-panel` | padding: 16px; flex-direction: column |
| `.preset-row` | padding: 10px 12px; background: `#111827`; border-radius: 8px; margin-bottom: 6px |
| `.preset-row:hover` | border-color: `#1e3a5f` |
| `.preset-name` | color: `#e2e8f0`; font-size: 13px; -unity-font-style: bold |
| `.preset-meta` | color: `#64748b`; font-size: 11px; margin-left: 8px |
| `.badge-default` | background: `#1e3a5f`; color: `#4f8ef7`; font-size: 10px; padding: 2px 6px; border-radius: 10px |
| `.import-section` | margin-top: 16px; padding: 12px; background: `#111827`; border-radius: 8px |
| `.section-title` | color: `#94a3b8`; font-size: 12px; margin-bottom: 8px |
