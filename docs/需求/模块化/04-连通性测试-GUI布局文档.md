# 04 连通性测试 — GUI 布局文档

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 关联文档：[需求文档](./04-连通性测试-需求文档.md)

---

## 1. 面板概述

| 属性 | 值 |
|------|----|
| 面板名称 | 连通性测试面板（Test Panel） |
| 控制器类 | `TestPanelController` |
| UXML | `Assets/UI/AIGate/TestPanel.uxml` |
| USS | `Assets/UI/AIGate/TestPanel.uss` |
| 功能 | 测试代理连通性，支持单测/多代理并发对比 |

---

## 2. 布局结构

```
TestPanel (VisualElement .test-panel)
├── 测试配置卡 (.form-card)
│   ├── Label "连通性测试" .card-title
│   ├── 代理行 (.form-row)
│   │   ├── Label "代理地址" .form-label
│   │   └── TextField name=proxyInput placeholder="留空则使用当前代理" .input-field
│   ├── URL 行 (.form-row)
│   │   ├── Label "测试 URL" .form-label
│   │   └── TextField name=urlInput placeholder="https://www.google.com" .input-field
│   ├── 超时行 (.form-row)
│   │   ├── Label "超时 (ms)" .form-label
│   │   └── IntegerField name=timeoutInput value=10000 .input-sm
│   └── 按钮行 (.action-row)
│       ├── Button name=testBtn  text="开始测试" .btn-primary
│       └── Button name=clearBtn text="清除结果" .btn-secondary
│
├── 多代理对比区 (.compare-card)
│   ├── Label "多代理对比" .card-title
│   ├── TextField name=compareInput
│   │   placeholder="代理1,代理2,代理3（逗号分隔）" .input-field
│   └── Button name=compareBtn text="并发对比" .btn-primary
│
├── 结果区 (.results-card)
│   ├── Label "测试结果" .card-title
│   ├── 加载指示器 (VisualElement name=spinner .spinner display=none)
│   └── ScrollView name=resultsScroll .results-list
│       └── [foreach 结果]
│           └── 结果行 (.result-row .success/.failure)
│               ├── Label name=resultProxy .result-proxy
│               ├── Label name=resultLatency .result-latency
│               ├── Label name=resultStatus .result-status
│               └── Label name=resultBest text="★" .result-best display=none
│
└── Label name=feedbackLabel .feedback-label display=none
```

---

## 3. 控件属性

| 控件名 | 类型 | 关键属性 |
|--------|------|----------|
| `proxyInput` | TextField | 留空时使用 `HTTP_PROXY` 环境变量 |
| `urlInput` | TextField | 留空时使用默认 URL `https://www.google.com` |
| `timeoutInput` | IntegerField | 范围 100–300000；超出范围红色边框，测试按钮禁用 |
| `testBtn` | Button | 测试中变为「测试中...」并禁用；完成后恢复 |
| `spinner` | VisualElement | CSS 旋转动画，测试中 `display:flex` |
| `compareInput` | TextField | 逗号分隔代理地址；至少 2 个才启用对比按钮 |
| `.result-row.success` | VisualElement | 左边框色 `#22c55e` |
| `.result-row.failure` | VisualElement | 左边框色 `#f87171` |
| `.result-best` | Label | 仅最快结果 `display:flex` |

---

## 4. 交互逻辑

### 4.1 单代理测试

```csharp
private async void OnTestClicked(ClickEvent evt) {
    _testBtn.SetEnabled(false);
    _testBtn.text = "测试中...";
    _spinner.style.display = DisplayStyle.Flex;
    _resultsScroll.Clear();
    try {
        var proxy = string.IsNullOrEmpty(_proxyInput.value)
            ? _envVarManager.GetEffectiveProxy("HTTP_PROXY")
            : _proxyInput.value;
        if (proxy == null) { ShowFeedback("当前未设置代理", false); return; }
        var url     = string.IsNullOrEmpty(_urlInput.value) ? null : _urlInput.value;
        var timeout = _timeoutInput.value;
        var result  = await Task.Run(() => _proxyTester.Test(proxy, url, timeout));
        _resultsScroll.Add(BuildResultRow(result, isBest: true));
    } finally {
        _testBtn.SetEnabled(true);
        _testBtn.text = "开始测试";
        _spinner.style.display = DisplayStyle.None;
    }
}
```

### 4.2 多代理对比

```csharp
private async void OnCompareClicked(ClickEvent evt) {
    var proxies = _compareInput.value.Split(',').Select(p => p.Trim())
        .Where(p => !string.IsNullOrEmpty(p)).ToList();
    if (proxies.Count < 2) { ShowFeedback("至少需要 2 个代理地址", false); return; }

    _compareBtn.SetEnabled(false);
    _resultsScroll.Clear();
    try {
        var results = await Task.Run(() => _proxyTester.TestMany(proxies));
        var sorted  = results.OrderBy(r => r.LatencyMs ?? int.MaxValue).ToList();
        for (int i = 0; i < sorted.Count; i++)
            _resultsScroll.Add(BuildResultRow(sorted[i], isBest: i == 0 && sorted[i].Success));
    } finally {
        _compareBtn.SetEnabled(true);
    }
}
```

### 4.3 BuildResultRow

```csharp
private VisualElement BuildResultRow(ProxyTestResult r, bool isBest) {
    var row = new VisualElement();
    row.AddToClassList("result-row");
    row.AddToClassList(r.Success ? "success" : "failure");

    row.Add(new Label(r.Proxy) { name = "resultProxy" });
    row.Add(new Label(r.Success ? $"{r.LatencyMs}ms" : "超时/失败") { name = "resultLatency" });
    row.Add(new Label(r.Success ? $"{r.StatusCode}" : r.ErrorMessage) { name = "resultStatus" });

    var star = new Label("★") { name = "resultBest" };
    star.style.display = isBest ? DisplayStyle.Flex : DisplayStyle.None;
    row.Add(star);
    return row;
}
```

---

## 5. 响应式适配

| 宽度 | 布局行为 |
|------|----------|
| ≥ 500px | 表单行水平：label + input 排列 |
| < 500px | 表单行纵向：label 上方，input 下方 |

---

## 6. 样式规范

| USS 类 | 关键样式 |
|--------|----------|
| `.results-card` | background: `#111827`; border-radius: 10px; padding: 16px |
| `.result-row` | padding: 8px 12px; border-radius: 6px; margin-bottom: 4px; border-left: 3px solid transparent |
| `.result-row.success` | border-left-color: `#22c55e`; background: `#0d1f17` |
| `.result-row.failure` | border-left-color: `#f87171`; background: `#1f0d0d` |
| `.result-latency` | color: `#f59e0b`; font-size: 12px; margin-left: auto |
| `.result-best` | color: `#f59e0b`; font-size: 14px; margin-left: 6px |
| `.spinner` | width: 20px; height: 20px; border: 2px solid `#4f8ef7`; border-radius: 50%; animation: spin 0.8s linear infinite |
