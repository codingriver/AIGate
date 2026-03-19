# 01 全局代理管理 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./01-全局代理管理-需求文档.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----|
| 面板名称 | 全局代理面板（Global Panel） |
| 控制器类 | `GlobalPanelController` |
| UXML 文件 | `Assets/UI/AIGate/GlobalPanel.uxml` |
| USS 文件 | `Assets/UI/AIGate/GlobalPanel.uss` |
| 导航项 | `[G] 全局代理`（侧边栏第一项） |
| 功能 | 设置/清除全局环境变量代理（HTTP_PROXY、HTTPS_PROXY、NO_PROXY） |

---

## 2. 布局结构（层级关系）

```
GlobalPanel (VisualElement .global-panel)
├── 状态卡 (VisualElement .status-card)
│   ├── 卡片标题行 (VisualElement .card-header)
│   │   ├── Label "当前代理状态" .card-title
│   │   └── Button name=refreshBtn .btn-icon 「↻」
│   ├── HTTP_PROXY 行 (VisualElement .status-row)
│   │   ├── Label "HTTP_PROXY" .status-key
│   │   └── Label name=httpProxyLabel .status-value
│   ├── HTTPS_PROXY 行 (VisualElement .status-row)
│   │   ├── Label "HTTPS_PROXY" .status-key
│   │   └── Label name=httpsProxyLabel .status-value
│   └── NO_PROXY 行 (VisualElement .status-row)
│       ├── Label "NO_PROXY" .status-key
│       └── Label name=noProxyLabel .status-value
│
├── 表单卡 (VisualElement .form-card)
│   ├── Label "设置代理" .card-title
│   ├── TextField name=proxyInput .input-field
│   │   placeholder="http://127.0.0.1:7890"
│   ├── Foldout name=advancedFoldout text="高级选项" value=false
│   │   ├── TextField name=httpInput  label="HTTP 代理"
│   │   ├── TextField name=httpsInput label="HTTPS 代理"
│   │   └── TextField name=noProxyInput label="NO_PROXY"
│   │       placeholder="localhost,127.0.0.1,*.local"
│   └── Toggle name=testToggle text="应用前测试连通性" value=false
│
├── 按钮行 (VisualElement .action-row)
│   ├── Button name=clearBtn .btn-danger   「清除代理」
│   ├── Button name=refreshBtn2 .btn-secondary 「刷新」
│   └── Button name=applyBtn .btn-primary  「应用设置」
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性详细说明

| 控件名 | 类型 | 关键属性 / 行为 |
|--------|------|----------------|
| `proxyInput` | TextField | placeholder: `http://127.0.0.1:7890`；实时校验，不合法时边框变红 `#f87171` |
| `testToggle` | Toggle | 默认未选中；选中时应用前自动测试连通性 |
| `httpInput` | TextField | label: "HTTP 代理"；仅高级展开时可见 |
| `httpsInput` | TextField | label: "HTTPS 代理"；仅高级展开时可见 |
| `noProxyInput` | TextField | placeholder: `localhost,127.0.0.1,*.local`；仅高级展开时可见 |
| `clearBtn` | Button | class: `btn-danger`；调用 `OnClearClicked` |
| `applyBtn` | Button | class: `btn-primary`；输入为空或格式错误时禁用 |
| `feedbackLabel` | Label | 默认 `display:none`；显示 3 秒后自动隐藏；成功绿/失败红/警告黄 |
| `httpProxyLabel` | Label | 空值时显示 `(未设置)` 色 `#64748b`；有值时色 `#f59e0b` |
| `httpsProxyLabel` | Label | 同上 |
| `noProxyLabel` | Label | 空值时 `#64748b`；有值时 `#e2e8f0` |

---

## 4. 交互逻辑

### 4.1 初始化

```csharp
public override void CreateGUI() {
    var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/AIGate/GlobalPanel.uxml");
    tree.CloneTree(rootVisualElement);

    _proxyInput    = rootVisualElement.Q<TextField>("proxyInput");
    _testToggle    = rootVisualElement.Q<Toggle>("testToggle");
    _clearBtn      = rootVisualElement.Q<Button>("clearBtn");
    _applyBtn      = rootVisualElement.Q<Button>("applyBtn");
    _feedbackLabel = rootVisualElement.Q<Label>("feedbackLabel");
    _httpLabel     = rootVisualElement.Q<Label>("httpProxyLabel");
    _httpsLabel    = rootVisualElement.Q<Label>("httpsProxyLabel");
    _noProxyLabel  = rootVisualElement.Q<Label>("noProxyLabel");

    _proxyInput.RegisterValueChangedCallback(OnProxyInputChanged);
    _clearBtn.RegisterCallback<ClickEvent>(OnClearClicked);
    _applyBtn.RegisterCallback<ClickEvent>(OnApplyClicked);
    rootVisualElement.Q<Button>("refreshBtn").RegisterCallback<ClickEvent>(_ => Refresh());

    Refresh();
}
```

### 4.2 Refresh() — 刷新状态卡

```csharp
public override void Refresh() {
    SetStatusLabel(_httpLabel,  _envVarManager.GetEffectiveProxy("HTTP_PROXY"));
    SetStatusLabel(_httpsLabel, _envVarManager.GetEffectiveProxy("HTTPS_PROXY"));
    SetStatusLabel(_noProxyLabel, _envVarManager.GetEffectiveProxy("NO_PROXY"));
}

private void SetStatusLabel(Label label, string? value) {
    label.text = string.IsNullOrEmpty(value) ? "(未设置)" : value;
    label.style.color = string.IsNullOrEmpty(value)
        ? new Color(0.39f, 0.45f, 0.55f)   // #64748b 灰
        : new Color(0.96f, 0.62f, 0.04f);   // #f59e0b 黄
}
```

### 4.3 OnApplyClicked — 应用设置（异步，后台线程）

```csharp
private async void OnApplyClicked(ClickEvent evt) {
    _applyBtn.SetEnabled(false);
    _applyBtn.text = "设置中...";
    try {
        if (_testToggle.value) {
            // 必须在后台线程执行网络 I/O
            var r = await Task.Run(() => _proxyTester.Test(_proxyInput.value));
            if (!r.Success) { ShowFeedback($"测试失败：{r.ErrorMessage}", false); return; }
        }
        await Task.Run(() => {
            _envVarManager.SetProcess("HTTP_PROXY",  _proxyInput.value);
            _envVarManager.SetProcess("HTTPS_PROXY", _proxyInput.value);
            if (!string.IsNullOrEmpty(_noProxyInput?.value))
                _envVarManager.SetProcess("NO_PROXY", _noProxyInput.value);
            _proxyHistory.Add(_proxyInput.value);
        });
        Refresh();
        ShowFeedback("代理已设置", true);
    } catch (Exception ex) {
        ShowFeedback($"设置失败：{ex.Message}", false);
    } finally {
        _applyBtn.SetEnabled(true);
        _applyBtn.text = "应用设置";
    }
}
```

### 4.4 OnClearClicked — 清除代理

```csharp
private async void OnClearClicked(ClickEvent evt) {
    await Task.Run(() => {
        _envVarManager.ClearProcess("HTTP_PROXY");
        _envVarManager.ClearProcess("HTTPS_PROXY");
        _envVarManager.ClearProcess("NO_PROXY");
    });
    Refresh();
    ShowFeedback("代理已清除", true);
}
```

### 4.5 ShowFeedback — 3 秒自动隐藏

```csharp
private async void ShowFeedback(string msg, bool success) {
    _feedbackLabel.text = msg;
    _feedbackLabel.style.color = success
        ? new Color(0.13f, 0.77f, 0.37f)   // #22c55e 绿
        : new Color(0.97f, 0.44f, 0.44f);  // #f87171 红
    _feedbackLabel.style.display = DisplayStyle.Flex;
    await Task.Delay(3000);
    _feedbackLabel.style.display = DisplayStyle.None;
}
```

### 4.6 Dispose — 事件注销（防热重载泄漏）

```csharp
public void Dispose() {
    _proxyInput?.UnregisterValueChangedCallback(OnProxyInputChanged);
    _clearBtn?.UnregisterCallback<ClickEvent>(OnClearClicked);
    _applyBtn?.UnregisterCallback<ClickEvent>(OnApplyClicked);
}
```

---

## 5. 响应式适配

| 窗口宽度 | 布局行为 |
|----------|----------|
| ≥ 500px | 按钮行水平排列 |
| 300–499px | 按钮行允许换行 |
| < 300px | 按钮行纵向堆叠，宽度 100% |

状态卡标签超长时以 `...` 省略，tooltip 显示完整值。

---

## 6. 样式规范（USS 关键类）

| USS 类 | 关键样式 |
|--------|----------|
| `.global-panel` | padding: 16px; background: `#0f1117`; flex-direction: column |
| `.status-card` / `.form-card` | background: `#111827`; border-radius: 10px; padding: 16px 20px; border: 1px `#1e293b` |
| `.status-key` | color: `#64748b`; font-size: 11px; width: 110px |
| `.status-value` | color: `#f59e0b`; font-size: 11px; text-overflow: ellipsis |
| `.input-field` | background: `#0a0e18`; border: 1px `#1e293b`; border-radius: 6px; height: 34px |
| `.input-field:focus` | border-color: `#4f8ef7` |
| `.action-row` | flex-direction: row; justify-content: flex-end; margin-top: 8px |
| `.btn-primary` | background: `#4f8ef7`; color: white; border-radius: 6px; padding: 6px 16px |
| `.btn-primary:hover` | background: `#6ba3f9` |
| `.btn-secondary` | background: `#1e293b`; color: `#94a3b8` |
| `.btn-danger` | background: transparent; color: `#f87171`; border: 1px `#f87171` |
| `.feedback-label` | font-size: 11px; margin-top: 6px; padding: 4px 8px |

**字体**：思源黑体 `SourceHanSansSC-Bold SDF.asset`  
**主题色变量**参见 `docs/需求/09-Unity-GUI规范.md §9.2`
