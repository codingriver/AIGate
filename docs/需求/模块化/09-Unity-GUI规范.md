# 09 Unity GUI 规范

> 所属项目：Gate · 模块化文档 v2.0  
> 技术栈：Unity UIToolkit（UXML + USS）  
> 本文档是所有 GUI 布局文档的公共规范基础，优先级高于各模块 GUI 文档中的局部描述。

---

## 1. 窗口架构

```
AIGateWindow (EditorWindow)
├── 侧边栏 (VisualElement .sidebar)
│   ├── Logo / 标题 (Label .sidebar-logo)
│   ├── 导航项列表
│   │   ├── Button name=navGlobal   text="[G] 全局代理"
│   │   ├── Button name=navTools    text="[T] 工具代理"
│   │   ├── Button name=navPresets  text="[P] 预设"
│   │   ├── Button name=navTest     text="[✓] 测试"
│   │   └── Button name=navPlugins  text="[+] 插件"
│   └── 底部版本号 (Label .sidebar-version)
│
└── 内容区 (VisualElement .content-area)
    └── [当前激活面板]
```

**导航切换逻辑**：
- 点击导航按钮时，先调用当前面板的 `Dispose()`，再实例化新面板并调用 `CreateGUI()`
- 激活导航项添加 `.active` 类，其余移除
- 面板切换在主线程执行，无异步等待

---

## 2. 面板基类

```csharp
// Gate.Editor/UI/Panels/PanelBase.cs
public abstract class PanelBase : IDisposable {
    protected readonly VisualElement Root;
    protected readonly EnvVarManager _envVarManager;
    protected readonly ToolRegistry  _toolRegistry;
    protected readonly ProfileManager _profileManager;
    protected readonly ProxyHistory  _proxyHistory;
    protected readonly ProxyTester   _proxyTester;
    protected readonly PluginManager _pluginManager;

    public abstract void CreateGUI();

    // 刷新面板显示（从数据源重新读取并更新 UI）
    public abstract void Refresh();

    // 注销所有 RegisterCallback，防止热重载内存泄漏
    public abstract void Dispose();

    // 显示反馈标签（3 秒后自动隐藏）
    protected async void ShowFeedback(string message, bool success) {
        var label = Root.Q<Label>("feedbackLabel");
        if (label == null) return;
        label.text = message;
        label.style.color = success
            ? new Color(0.13f, 0.77f, 0.37f)   // #22c55e
            : new Color(0.97f, 0.44f, 0.44f);  // #f87171
        label.style.display = DisplayStyle.Flex;
        await Task.Delay(3000);
        label.style.display = DisplayStyle.None;
    }
}
```

---

## 3. 颜色系统

所有颜色通过 USS 变量定义，各面板 USS 文件通过 `var()` 引用：

```css
/* Assets/UI/AIGate/Variables.uss — 全局变量（在所有面板中 @import）*/
:root {
    --color-bg:           #0f1117;   /* 主背景 */
    --color-surface:      #111827;   /* 卡片背景 */
    --color-surface-2:    #1e293b;   /* 边框、分割线 */
    --color-surface-3:    #0a0e18;   /* 输入框背景 */
    --color-accent:       #4f8ef7;   /* 主蓝色强调 */
    --color-accent-hover: #6ba3f9;   /* 悬停蓝 */
    --color-text-primary: #e2e8f0;   /* 主文本 */
    --color-text-secondary: #94a3b8; /* 次要文本 */
    --color-text-muted:   #64748b;   /* 禁用/占位文本 */
    --color-success:      #22c55e;   /* 成功绿 */
    --color-warning:      #f59e0b;   /* 警告黄 */
    --color-error:        #f87171;   /* 错误红 */
    --color-danger-bg:    #1f0d0d;   /* 错误行背景 */
    --color-highlight-bg: #0d1f17;   /* 成功行背景 */
}
```

---

## 4. 字体规范

| 用途 | 字体资源 | 大小 |
|------|----------|------|
| 标题 / 加粗 | `SourceHanSansSC-Bold SDF.asset` | 13–15px |
| 正文 / 标签 | `SourceHanSansSC-Regular SDF.asset` | 11–12px |
| 代码 / 路径 | `JetBrainsMono-Regular SDF.asset` | 11px |
| 小注 / 元数据 | `SourceHanSansSC-Regular SDF.asset` | 10px |

字体文件路径：`Assets/UI/Fonts/`

---

## 5. 间距规范

| 层级 | 值 |
|------|----|
| 面板外边距 | `padding: 16px` |
| 卡片间距 | `margin-bottom: 12px` |
| 卡片内边距 | `padding: 16px 20px` |
| 行间距 | `margin-bottom: 6–8px` |
| 按钮内边距 | `padding: 6px 16px` |
| 小按钮内边距 | `padding: 0 10px; height: 26–28px` |

---

## 6. 公共 USS 类

```css
/* 适用于所有面板 */

.card-title {
    color: var(--color-text-primary);
    font-size: 13px;
    -unity-font-style: bold;
    margin-bottom: 10px;
}

.card-header {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
}

.form-row {
    flex-direction: row;
    align-items: center;
    margin-bottom: 8px;
}

.form-label {
    width: 90px;
    flex-shrink: 0;
    color: var(--color-text-secondary);
    font-size: 12px;
}

.input-field {
    flex-grow: 1;
    background-color: var(--color-surface-3);
    border-color: var(--color-surface-2);
    border-radius: 6px;
    color: var(--color-text-primary);
    font-size: 12px;
    height: 34px;
}

.input-field:focus { border-color: var(--color-accent); }

.action-row {
    flex-direction: row;
    justify-content: flex-end;
    margin-top: 8px;
}

.btn-primary {
    background-color: var(--color-accent);
    color: white;
    border-radius: 6px;
    padding: 6px 16px;
    margin-left: 8px;
    border-width: 0;
}
.btn-primary:hover { background-color: var(--color-accent-hover); }
.btn-primary:disabled { opacity: 0.5; }

.btn-secondary {
    background-color: var(--color-surface-2);
    color: var(--color-text-secondary);
    border-radius: 6px;
    padding: 6px 16px;
    margin-left: 8px;
    border-width: 1px;
    border-color: var(--color-surface-2);
}
.btn-secondary:hover { border-color: var(--color-accent); color: var(--color-accent); }

.btn-danger {
    background-color: transparent;
    color: var(--color-error);
    border-color: var(--color-error);
    border-width: 1px;
    border-radius: 6px;
    padding: 6px 16px;
}
.btn-danger:hover { background-color: var(--color-danger-bg); }

.feedback-label {
    font-size: 11px;
    margin-top: 6px;
    padding: 4px 8px;
    border-radius: 4px;
}

.badge {
    font-size: 10px;
    padding: 2px 6px;
    border-radius: 10px;
    margin-left: 6px;
}
```

---

## 7. ConfirmDialog 公共组件

```csharp
// Gate.Editor/UI/Components/ConfirmDialog.cs
public static class ConfirmDialog {
    // 显示确认对话框，返回用户选择（true = 确认，false = 取消）
    // 在 Unity Editor 中使用 EditorUtility.DisplayDialog
    public static Task<bool> Show(string message, string title = "确认",
        string confirm = "确认", string cancel = "取消") {
        bool result = EditorUtility.DisplayDialog(title, message, confirm, cancel);
        return Task.FromResult(result);
    }
}
```

---

## 8. 异步模式强制要求

1. 所有网络 I/O（代理测试）必须用 `await Task.Run(...)` 在后台线程执行
2. 所有文件 I/O（读写配置文件）建议用 `await Task.Run(...)` 避免卡顿
3. UI 更新必须在主线程（`await Task.Run` 完成后的代码自动回到主线程）
4. 所有 `async void` 方法必须有 `try/catch`，异常通过 `ShowFeedback` 显示
5. 长时间操作须禁用触发按钮，操作完成后恢复

---

## 9. 事件注销要求

每个面板的 `Dispose()` 必须注销所有通过 `RegisterCallback` 和 `RegisterValueChangedCallback` 注册的回调，防止 Unity 热重载时内存泄漏。

**模板**：
```csharp
public override void Dispose() {
    _applyBtn?.UnregisterCallback<ClickEvent>(OnApplyClicked);
    _clearBtn?.UnregisterCallback<ClickEvent>(OnClearClicked);
    _proxyInput?.UnregisterValueChangedCallback(OnProxyInputChanged);
    // ... 其余所有注册的回调
}
```

---

## 10. 响应式断点

| 断点名 | 窗口宽度 | 布局策略 |
|--------|----------|----------|
| Large | ≥ 600px | 全功能布局，所有控件水平排列 |
| Medium | 400–599px | 部分控件换行，按钮组保持水平 |
| Small | 300–399px | 按钮组纵向堆叠，宽度 100% |
| XSmall | < 300px | 最小可用布局，隐藏次要信息 |

响应式通过 `GeometryChangedEvent` 监听面板宽度变化，动态添加/移除 CSS 类：

```csharp
root.RegisterCallback<GeometryChangedEvent>(evt => {
    root.EnableInClassList("layout-small",  evt.newRect.width < 400);
    root.EnableInClassList("layout-medium", evt.newRect.width >= 400 && evt.newRect.width < 600);
    root.EnableInClassList("layout-large",  evt.newRect.width >= 600);
});
```
