# 05 插件系统 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./05-插件系统-需求文档.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----|
| 面板名称 | 插件面板（Plugins Panel） |
| 控制器类 | `PluginsPanelController` |
| UXML | `Assets/UI/AIGate/PluginsPanel.uxml` |
| USS | `Assets/UI/AIGate/PluginsPanel.uss` |
| 功能 | 安装/卸载/查看第三方工具插件 |

---

## 2. 布局结构

```
PluginsPanel (.plugins-panel)
├── 安装区 (.install-card)
│   ├── Label "安装插件" .card-title
│   ├── TextField name=pluginPathInput placeholder="工具 JSON 路径" .input-field
│   ├── Button name=browseBtn text="浏览..." .btn-secondary
│   ├── Button name=validateBtn text="校验" .btn-secondary
│   └── Button name=installBtn  text="安装" .btn-primary
│
├── 已安装列表 (ScrollView name=pluginsScroll .plugins-list)
│   └── [foreach 插件] (.plugin-row)
│       ├── Label name=pluginName .plugin-name
│       ├── Label name=pluginPath .plugin-path
│       ├── Label name=overrideBadge text="覆盖内置" .badge-warn
│       ├── Button name=showBtn   text="详情" .btn-xs-secondary
│       └── Button name=removeBtn text="卸载" .btn-xs-danger
│
├── 校验结果 (VisualElement name=validateResult display=none)
│   ├── Label name=validateStatus
│   └── ListView name=errorList
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性

| 控件名 | 类型 | 关键属性 |
|--------|------|----------|
| `pluginPathInput` | TextField | 实时检查文件是否存在，不存在时红色边框 |
| `browseBtn` | Button | 打开文件对话框，过滤 `*.json` |
| `validateBtn` | Button | 路径为空/文件不存在时禁用 |
| `installBtn` | Button | 路径为空/文件不存在时禁用 |
| `overrideBadge` | Label | 仅覆盖内置时 `display:flex` |
| `showBtn` | Button | 弹窗显示完整 JSON |
| `removeBtn` | Button | 确认对话框后卸载 |
| `validateResult` | VisualElement | 校验后显示；成功绿色，失败红色 |
| `errorList` | ListView | 每项一条 schema 错误 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/PluginsPanel.uxml");
    tree.CloneTree(rootVisualElement);
    rootVisualElement.Q<Button>("browseBtn").RegisterCallback<ClickEvent>(OnBrowse);
    rootVisualElement.Q<Button>("validateBtn").RegisterCallback<ClickEvent>(OnValidate);
    rootVisualElement.Q<Button>("installBtn").RegisterCallback<ClickEvent>(OnInstall);
    Refresh();
}
```

### 4.2 校验

```csharp
private async void OnValidate(ClickEvent evt) {
    var r = await Task.Run(() => _pluginManager.Validate(_pluginPathInput.value));
    _validateResult.style.display = DisplayStyle.Flex;
    var status = _validateResult.Q<Label>("validateStatus");
    status.text = r.IsValid ? "✓ 格式合法" : "✗ 格式错误";
    status.style.color = r.IsValid
        ? new Color(0.13f, 0.77f, 0.37f)
        : new Color(0.97f, 0.44f, 0.44f);
    var errList = _validateResult.Q<ListView>("errorList");
    errList.itemsSource = r.Errors.ToList();
    errList.style.display = r.Errors.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
}
```

### 4.3 安装

```csharp
private async void OnInstall(ClickEvent evt) {
    var r = await Task.Run(() => _pluginManager.Install(_pluginPathInput.value));
    if (r.Success) { Refresh(); ShowFeedback("插件安装成功", true); _pluginPathInput.value = ""; }
    else ShowFeedback($"安装失败：{r.ErrorMessage}", false);
}
```

### 4.4 卸载

```csharp
private async void OnRemoveClicked(string name) {
    if (!await ConfirmDialog.Show($"确认卸载 '{name}'?")) return;
    await Task.Run(() => _pluginManager.Remove(name));
    Refresh();
    ShowFeedback($"'{name}' 已卸载", true);
}
```

---

## 5. 响应式适配

| 宽度 | 布局行为 |
|------|----------|
| ≥ 480px | 路径输入框与浏览按钮同行 |
| < 480px | 浏览按钮换行，宽度 100% |

---

## 6. 样式规范

| USS 类 | 关键样式 |
|--------|----------|
| `.install-card` | background: `#111827`; border-radius: 10px; padding: 16px; margin-bottom: 12px |
| `.plugin-row` | padding: 10px 12px; border-bottom: 1px `#1e293b`; flex-direction: row; align-items: center |
| `.plugin-name` | color: `#e2e8f0`; font-size: 12px; font-style: bold |
| `.plugin-path` | color: `#64748b`; font-size: 10px |
| `.badge-warn` | background: `#422006`; color: `#f59e0b`; font-size: 10px; padding: 2px 6px; border-radius: 10px |
| `.validate-result` | padding: 8px 12px; border-radius: 6px; margin-top: 8px |
