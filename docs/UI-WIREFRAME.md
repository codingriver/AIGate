# Gate Unity GUI 界面布局文档

> 基于 Gate CLI 功能设计的 Unity UIToolkit 界面线框图
> 文件位于 `Assets/UI/AIGate/`，控制器位于 `Assets/Scripts/AIGate/`

---

## 1. 整体布局结构

```
+---------------------------------------------------------------+
|  GateMainWindow.uxml (主窗口)                                  |
|  +------------+  +----------------------------------------+  |
|  | 侧边导航栏  |  |  内容区（面板切换）flex-grow:1          |  |
|  | 176px      |  |                                        |  |
|  | [全局代理]  |  |  GlobalPanel.uxml   全局代理           |  |
|  | [应用代理]  |  |  AppPanel.uxml      应用代理           |  |
|  | [预  设]   |  |  PresetPanel.uxml   预设管理           |  |
|  | [状态总览]  |  |  StatusPanel.uxml   状态总览           |  |
|  | [连通测试]  |  |  TestPanel.uxml     连通测试           |  |
|  | ─────────  |  |                                        |  |
|  | [配置向导]  |  |                                        |  |
|  +------------+  +----------------------------------------+  |
+---------------------------------------------------------------+
```

---

## 2. GateMainWindow.uxml

**控制器**：`GatePanelController.cs`

```
+---------------------------------------------------------------+
|  Gate  代理配置管理                            v1.0.0          |
+------------+--------------------------------------------------+
|  侧边导航   |  内容面板区 (padding:20px)                        |
|            |                                                  |
|  [全局代理] |                                                  |
|  [应用代理] |  [当前选中面板内容]                               |
|  [预  设]  |                                                  |
|  [状态总览] |                                                  |
|  [连通测试] |                                                  |
|  ────────  |                                                  |
|  [配置向导] |                                                  |
+------------+--------------------------------------------------+

元素:
  #nav-sidebar      侧边栏容器
  #nav-global       全局代理按钮
  #nav-app          应用代理按钮
  #nav-preset       预设按钮
  #nav-status       状态总览按钮
  #nav-test         连通测试按钮
  #nav-wizard       向导按钮（紫色 .nav-item--wizard）
  #content-area     内容区容器（C# 动态加载面板）
  #version-label    版本号标签
```

---

## 3. GlobalPanel.uxml

**控制器**：`GlobalPanelController.cs`

```
+--------------------------------------------------------------+
|  🌐 全局代理    HTTP_PROXY / HTTPS_PROXY / NO_PROXY           |
+--------------------------------------------------------------+
|  当前配置                                                     |
|  HTTP_PROXY   [值 / (未设置)]                                 |
|  HTTPS_PROXY  [值 / (未设置)]                                 |
|  NO_PROXY     [值 / (未设置)]                                 |
|                                                              |
|  设置代理                                                     |
|  代理地址(-p)  [___________________]  同时设置 HTTP/HTTPS      |
|  HTTP(-H)     [___________________]                          |
|  HTTPS(-S)    [___________________]                          |
|  NO_PROXY     [___________________]                          |
|  [ ] 设置前测试连通性 (--verify)                              |
|                                                              |
|              [清除代理]  [刷新状态]  [应用设置]               |
|  (反馈消息)                                                   |
+--------------------------------------------------------------+

元素:
  #global-http-status / #global-https-status / #global-noproxy-status
  #proxy-input  #http-input  #https-input  #noproxy-input
  #verify-toggle
  #btn-clear  #btn-refresh  #btn-apply
  #global-feedback
```

---

## 4. AppPanel.uxml

**控制器**：`AppPanelController.cs`

```
+--------------------------------------------------------------+
|  📦 应用代理配置    支持 130+ 工具，逗号分隔批量操作             |
+--------------------------------------------------------------+
|  [搜索...]  [分类 v]  [ ] 仅已安装                            |
|  应用名称   | 分类      | 状态  | 操作                         |
|  git        | 版本控制  | 绿点  | [Set][Clear]                |
|  npm        | 包管理器  | 灰点  | [Set][Clear]                |
|  cursor     | AI IDE   | 绿点  | [Set][Clear]                |
|  ...                                                         |
|  已选0个  [批量代理...]  [全选已安装] [批量设置] [批量清除]     |
|  (反馈消息)                                                   |
|                                                              |
|  +-- 编辑弹窗 (overlay) --------------------------------+    |
|  |  编辑: git                                          |    |
|  |  代理: [________________________________]           |    |
|  |                                 [取消]  [保存]      |    |
|  +-----------------------------------------------------+    |
+--------------------------------------------------------------+

元素:
  #app-search  #category-filter  #installed-only-toggle
  #app-list (ListView item-height=44)
  #batch-count  #batch-proxy-input
  #btn-select-installed  #btn-batch-set  #btn-batch-clear
  #app-feedback
  #edit-overlay  #edit-app-name  #edit-proxy-input
  #btn-edit-cancel  #btn-edit-save
```

---

## 5. PresetPanel.uxml

**控制器**：`PresetPanelController.cs`

```
+--------------------------------------------------------------+
|  💾 预设配置集    Preset Management                           |
+--------------------------------------------------------------+
|  +------------------+  +--------------------------------+    |
|  | 已保存的预设      |  | 预设详情                        |    |
|  | office (默认)    |  | 名称:    office                 |    |
|  | home             |  | 创建:    2024/3/1               |    |
|  | project-a        |  | 更新:    2024/3/5               |    |
|  |                  |  | HTTP:    http://proxy:8080      |    |
|  | [+ 新建预设]     |  | 应用数:  5 个                   |    |
|  +------------------+  |                                |    |
|                         | [应用][设默认][导出][删除]      |    |
|                         +--------------------------------+    |
|  保存当前：  [预设名称...]  [保存]                             |
|  (反馈消息)                                                   |
+--------------------------------------------------------------+

元素:
  #preset-list  #btn-new-preset
  #detail-name  #detail-created  #detail-updated
  #detail-http  #detail-app-count
  #btn-apply-preset  #btn-set-default  #btn-export-preset  #btn-delete-preset
  #save-name-input  #btn-save-preset  #preset-feedback
  #new-preset-overlay  #new-preset-name  #new-preset-desc
  #btn-new-cancel  #btn-new-save
```

---

## 6. StatusPanel.uxml

**控制器**：`StatusPanelController.cs`

```
+--------------------------------------------------------------+
|  📊 状态总览    Status Overview                  [刷新]       |
+--------------------------------------------------------------+
|  全局代理（环境变量）                                          |
|  HTTP_PROXY   | http://proxy:8080    | [编辑]                 |
|  HTTPS_PROXY  | http://proxy:8080    | [编辑]                 |
|  NO_PROXY     | localhost,127.0.0.1  | [编辑]                 |
|                                                              |
|  应用代理配置  [已配置 12 个]                                  |
|  [版本控制]                                                   |
|  绿点  git         http://proxy:8080                         |
|  [包管理器]                                                   |
|  绿点  npm         http://proxy:8080                         |
|  绿点  pip         http://proxy:8080                         |
|  [AI IDE]                                                    |
|  绿点  cursor      http://proxy:8080                         |
|                                                              |
|  当前预设:  office                                           |
+--------------------------------------------------------------+

元素:
  #status-http  #status-https  #status-noproxy
  #btn-edit-http  #btn-edit-https  #btn-edit-noproxy
  #btn-refresh-status
  #configured-count
  #status-tool-list (VisualElement，C# 动态填充分组)
  #current-preset-label
```

---

## 7. TestPanel.uxml

**控制器**：`TestPanelController.cs`

```
+--------------------------------------------------------------+
|  🔌 代理连通性测试    Proxy Connectivity Test  --verify        |
+--------------------------------------------------------------+
|  代理地址:  [______________________________]  [使用当前]      |
|  测试 URL:  [______________________________]                 |
|                                                              |
|  [         开始测试         ]                                 |
|                                                              |
|  +-- 成功结果（默认隐藏）----------------------------+        |
|  |  ✔ 连接成功                                     |        |
|  |  响应时间: 120 ms     目标: http://www.google.com|        |
|  +--------------------------------------------------+        |
|                                                              |
|  +-- 失败结果（默认隐藏）----------------------------+        |
|  |  ✘ 连接失败                                     |        |
|  |  错误信息: Connection refused                   |        |
|  +--------------------------------------------------+        |
|                                                              |
|  测试历史                                                     |
|  绿点  http://proxy:8080  14:23:01  120ms                    |
|  红点  http://proxy:8080  14:22:10  failed                   |
|  ...                                                         |
+--------------------------------------------------------------+

元素:
  #test-proxy-input  #test-url-input
  #btn-use-current   填入当前环境变量代理
  #btn-run-test      开始测试（大按钮）
  #test-result-success / #test-result-fail  结果卡片
  #result-time  #result-url  #result-error
  #test-history-list  ListView（item-height=44，最多显示 50 条）
```

---

## 8. USS 样式主题

**文件**：`Assets/UI/AIGate/GateTheme.uss`

| 类别 | 关键 CSS 类 |
|------|-------------|
| 布局 | `.gate-root` `.gate-titlebar` `.gate-body` `.content-area` |
| 导航 | `.nav-sidebar` `.nav-item` `.nav-item--active` `.nav-item--wizard` `.nav-divider` |
| 面板 | `.panel-root` `.panel-header` `.panel-title` `.panel-subtitle` `.panel-header-actions` |
| 卡片 | `.status-card` `.form-card` `.card-section-title` `.card-header-row` `.card-count-badge` |
| 表单 | `.form-row` `.form-label` `.form-input` `.form-hint` `.form-toggle` `.action-row` |
| 按钮 | `.btn` `.btn-primary` `.btn-secondary` `.btn-danger` `.btn-sm` `.btn-large` `.btn-full` `.btn-inline` |
| 列表 | `.list-header` `.list-row` `.list-row-name` `.list-row-category` `.list-row-actions` |
| 状态点 | `.status-dot-on` (绿) `.status-dot-off` (灰) `.status-value` `.status-value--empty` |
| 批量栏 | `.batch-bar` `.batch-count` `.batch-input` |
| 预设 | `.preset-body` `.preset-list-pane` `.preset-detail-pane` `.detail-row` `.save-bar` |
| 测试 | `.result-card` `.result-card--success` `.result-card--fail` `.result-icon-label` `.result-detail` |
| 弹窗 | `.overlay` `.overlay-panel` `.overlay-title` `.overlay-subtitle` `.overlay-actions` |
| 反馈 | `.feedback-label` `.feedback-label--success` `.feedback-label--error` `.feedback-label--info` |
| 颜色 | bg `#0f1117` · surface `#1a1f2e` · accent `#4f8ef7` · warning `#f59e0b` · ok `#22c55e` · err `#f87171` |

---

## 9. C# 控制器对照

| UXML 文件 | 控制器类 | 挂载方式 |
|-----------|----------|----------|
| `GateMainWindow.uxml` | `GatePanelController` | MonoBehaviour，挂载到 GameObject |
| `GlobalPanel.uxml` | `GlobalPanelController` | 由 GatePanelController 实例化 |
| `AppPanel.uxml` | `AppPanelController` | 由 GatePanelController 实例化 |
| `PresetPanel.uxml` | `PresetPanelController` | 由 GatePanelController 实例化 |
| `StatusPanel.uxml` | `StatusPanelController` | 由 GatePanelController 实例化 |
| `TestPanel.uxml` | `TestPanelController` | 由 GatePanelController 实例化 |

### Unity Inspector 配置

在 `GatePanelController` Inspector 中绑定：

| 字段 | 值 |
|------|----|
| `uiDocument` | 挂载 `UIDocument` 组件的引用 |
| `globalPanelAsset` | `Assets/UI/AIGate/GlobalPanel.uxml` |
| `appPanelAsset` | `Assets/UI/AIGate/AppPanel.uxml` |
| `presetPanelAsset` | `Assets/UI/AIGate/PresetPanel.uxml` |
| `statusPanelAsset` | `Assets/UI/AIGate/StatusPanel.uxml` |
| `testPanelAsset` | `Assets/UI/AIGate/TestPanel.uxml` |

