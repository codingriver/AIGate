# 08 配置迁移 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./08-配置迁移-需求文档.md) | [Unity GUI 规范](./09-Unity-GUI规范.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----||
| 面板名称 | 配置迁移面板（Migration Panel） |
| 控制器类 | `MigrationPanelController` |
| UXML 文件 | `Assets/UI/AIGate/MigrationPanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/MigrationPanel.uss` |
| 导航项 | `[↕] 迁移`（侧边栏第八项，可选显示） |
| 功能 | 导出/导入全量 Gate 配置（ZIP）；完全重置 Gate 数据；备份/回滚工具配置文件（P1） |

---

## 2. 布局结构（层级关系）

```
MigrationPanel (VisualElement .migration-panel)
├── 导出区 (.section-card)
│   ├── Label "导出全部配置" .card-title
│   ├── 说明行 (Label .section-desc)
│   │   text="将所有预设、工具路径、全局配置、插件声明打包为 ZIP 文件"
│   ├── 输出路径行 (.form-row)
│   │   ├── Label "导出路径" .form-label
│   │   ├── TextField name=exportPathInput placeholder="backup.zip" .input-field
│   │   └── Button name=exportBrowseBtn text="浏览..." .btn-secondary
│   └── 操作行 (.action-row)
│       └── Button name=exportBtn text="导出配置" .btn-primary
│
├── 导入区 (.section-card)
│   ├── Label "导入全部配置" .card-title
│   ├── 说明行 (Label .section-desc)
│   │   text="从 ZIP 文件恢复配置；默认跳过已有文件，勾选覆盖则强制替换"
│   ├── 输入路径行 (.form-row)
│   │   ├── Label "导入文件" .form-label
│   │   ├── TextField name=importPathInput placeholder="选择 .zip 文件" .input-field
│   │   └── Button name=importBrowseBtn text="浏览..." .btn-secondary
│   ├── 覆盖行 (.form-row)
│   │   └── Toggle name=overwriteToggle text="覆盖已有文件" .form-toggle
│   └── 操作行 (.action-row)
│       └── Button name=importBtn text="导入配置" .btn-primary
│
├── 导入结果区 (VisualElement name=importResultArea display=none .result-card)
│   ├── Label name=importSummary .result-summary
│   └── ListView name=importDetailList .result-list
│
├── 重置区 (.section-card .danger-card)
│   ├── Label "完全重置 Gate 数据" .card-title
│   ├── Label .section-desc
│   │   text="删除整个 Gate 数据目录（预设、历史、插件等），不影响工具自身配置文件"
│   └── 操作行 (.action-row)
│       └── Button name=resetBtn text="重置 Gate 数据" .btn-danger
│
├── 备份区 (.section-card) [P1 - 标注「P1 功能，即将可用」]
│   ├── Label "工具配置文件备份" .card-title
│   ├── TextField name=backupCommentInput placeholder="备注（可选）" .input-field
│   └── 操作行 (.action-row)
│       ├── Button name=backupBtn  text="立即备份" .btn-primary
│       └── Button name=rollbackBtn text="查看/回滚" .btn-secondary
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性详细说明

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `exportPathInput` | TextField | 默认值 `backup.zip`；路径为空时禁用导出按钮 |
| `exportBrowseBtn` | Button | 打开系统文件保存对话框，过滤 `*.zip`；将选择结果写回 `exportPathInput` |
| `exportBtn` | Button | 执行中变「导出中…」并禁用；完成后显示结果反馈 |
| `importPathInput` | TextField | 路径为空或文件不存在时禁用导入按钮；文件不存在时红色边框 |
| `importBrowseBtn` | Button | 打开文件选择对话框，过滤 `*.zip` |
| `overwriteToggle` | Toggle | 默认未选中（跳过已有文件）；选中后显示橙色警告说明 |
| `importBtn` | Button | 路径为空 / 文件不存在时禁用；执行中变「导入中…」并禁用 |
| `importResultArea` | VisualElement | 导入完成后 `display:flex`，显示摘要和详情列表 |
| `importSummary` | Label | 格式：`导入 N 个 / 跳过 M 个 / 失败 K 个`；失败数 > 0 时字体色警告黄 |
| `importDetailList` | ListView | 每项一条「✓ 文件名」或「✗ 文件名（原因）」；虚拟化滚动 |
| `resetBtn` | Button | 必须弹二次确认对话框；确认文本为「此操作将删除所有 Gate 数据，无法撤销！」 |
| `backupBtn` | Button | P1 阶段**隐藏**（`display:none`）；整个备份区改为显示灰色说明文字「备份功能即将上线」；P1 实现后移除说明文字并显示控件（B39） |
| `rollbackBtn` | Button | P1 阶段同上隐藏；P1 实现后显示，打开备份列表弹窗 |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/MigrationPanel.uxml");
    tree.CloneTree(rootVisualElement);

    _exportPathInput = rootVisualElement.Q<TextField>("exportPathInput");
    _importPathInput = rootVisualElement.Q<TextField>("importPathInput");
    _overwriteToggle = rootVisualElement.Q<Toggle>("overwriteToggle");
    _feedbackLabel   = rootVisualElement.Q<Label>("feedbackLabel");

    rootVisualElement.Q<Button>("exportBrowseBtn").RegisterCallback<ClickEvent>(OnExportBrowse);
    rootVisualElement.Q<Button>("exportBtn").RegisterCallback<ClickEvent>(OnExport);
    rootVisualElement.Q<Button>("importBrowseBtn").RegisterCallback<ClickEvent>(OnImportBrowse);
    rootVisualElement.Q<Button>("importBtn").RegisterCallback<ClickEvent>(OnImport);
    rootVisualElement.Q<Button>("resetBtn").RegisterCallback<ClickEvent>(OnReset);

    // P1 功能：隐藏备份/回滚控件，显示说明文字（B39）
    var backupSection = rootVisualElement.Q<VisualElement>("backupSection");
    if (backupSection != null) {
        backupSection.style.display = DisplayStyle.None;
        var p1Label = new Label("备份功能即将上线");
        p1Label.AddToClassList("section-desc");
        backupSection.parent?.Add(p1Label);
    }

    // 路径输入实时校验
    _importPathInput.RegisterValueChangedCallback(evt =>
        rootVisualElement.Q<Button>("importBtn")
            .SetEnabled(!string.IsNullOrWhiteSpace(evt.newValue) && File.Exists(evt.newValue)));

    Refresh();
}
```

### 4.2 导出（异步）

```csharp
private async void OnExport(ClickEvent evt) {
    var path = _exportPathInput.value.Trim();
    if (string.IsNullOrEmpty(path)) return;

    var exportBtn = rootVisualElement.Q<Button>("exportBtn");
    exportBtn.SetEnabled(false);
    exportBtn.text = "导出中…";

    try {
        await Task.Run(() => _migrationManager.ExportAll(path));
        ShowFeedback($"配置已导出：{path}", true);
    } catch (Exception ex) {
        ShowFeedback($"导出失败：{ex.Message}", false);
    } finally {
        exportBtn.SetEnabled(true);
        exportBtn.text = "导出配置";
    }
}
```

### 4.3 导入（异步）

```csharp
private async void OnImport(ClickEvent evt) {
    var path      = _importPathInput.value.Trim();
    var overwrite = _overwriteToggle.value;

    if (overwrite) {
        bool confirmed = await ConfirmDialog.Show(
            $"将覆盖 Gate 数据目录中已有文件，确认继续？");
        if (!confirmed) return;
    }

    var importBtn = rootVisualElement.Q<Button>("importBtn");
    importBtn.SetEnabled(false);
    importBtn.text = "导入中…";

    try {
        var result = await Task.Run(() =>
            _migrationManager.ImportAll(path, overwrite));

        // 显示结果区
        var resultArea   = rootVisualElement.Q<VisualElement>("importResultArea");
        var summaryLabel = rootVisualElement.Q<Label>("importSummary");
        resultArea.style.display = DisplayStyle.Flex;
        summaryLabel.text = $"导入 {result.Imported} 个 / 跳过 {result.Skipped} 个 / 失败 {result.Failed} 个";
        summaryLabel.style.color = result.Failed > 0
            ? new Color(0.96f, 0.62f, 0.04f)  // #f59e0b
            : new Color(0.13f, 0.77f, 0.37f); // #22c55e

        ShowFeedback(
            result.Failed == 0 ? "配置导入完成" : $"导入完成，{result.Failed} 个文件失败",
            result.Failed == 0);
    } catch (Exception ex) {
        ShowFeedback($"导入失败：{ex.Message}", false);
    } finally {
        importBtn.SetEnabled(true);
        importBtn.text = "导入配置";
    }
}
```

### 4.4 重置（确认对话框）

```csharp
private async void OnReset(ClickEvent evt) {
    bool confirmed = await ConfirmDialog.Show(
        "此操作将删除所有 Gate 数据（预设、历史、插件等），无法撤销！\n不影响工具自身配置文件（如 ~/.gitconfig）。",
        title: "确认重置",
        confirm: "确认删除",
        cancel: "取消");
    if (!confirmed) return;

    try {
        await Task.Run(() => _migrationManager.Reset());
        ShowFeedback("Gate 数据已重置", true);
        Refresh();
    } catch (Exception ex) {
        ShowFeedback($"重置失败：{ex.Message}", false);
    }
}
```

### 4.5 Dispose

```csharp
public override void Dispose() {
    rootVisualElement.Q<Button>("exportBrowseBtn")?.UnregisterCallback<ClickEvent>(OnExportBrowse);
    rootVisualElement.Q<Button>("exportBtn")?.UnregisterCallback<ClickEvent>(OnExport);
    rootVisualElement.Q<Button>("importBrowseBtn")?.UnregisterCallback<ClickEvent>(OnImportBrowse);
    rootVisualElement.Q<Button>("importBtn")?.UnregisterCallback<ClickEvent>(OnImport);
    rootVisualElement.Q<Button>("resetBtn")?.UnregisterCallback<ClickEvent>(OnReset);
    _importPathInput?.UnregisterValueChangedCallback(null);
}
```

---

## 5. 响应式适配

| 窗口宽度 | 布局行为 |
|----------|---------|
| ≥ 500px | 表单行水平：标签 + 输入框 + 浏览按钮同行 |
| 300–499px | 输入框与浏览按钮换行到第二行 |
| < 300px | 按钮宽度 100%，纵向堆叠 |

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|---------|
| `.migration-panel` | `padding: 16px; flex-direction: column; gap: 12px` |
| `.section-card` | `background: #111827; border-radius: 10px; padding: 16px 20px; border: 1px solid #1e293b` |
| `.danger-card` | `border-color: #3d1515` |
| `.section-desc` | `color: #64748b; font-size: 11px; margin-bottom: 10px; white-space: normal` |
| `.result-card` | `background: #0d1117; border-radius: 8px; padding: 12px 16px; border: 1px solid #1e293b` |
| `.result-summary` | `font-size: 12px; font-weight: bold; margin-bottom: 8px` |
| `.result-list` | `font-size: 11px; font-family: JetBrainsMono; color: #94a3b8` |

**字体**：摘要/结果列表使用 `JetBrainsMono-Regular SDF.asset`；标题/说明使用 `SourceHanSansSC-Regular SDF.asset`。
