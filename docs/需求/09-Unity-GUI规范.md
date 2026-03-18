# 09 Unity GUI 规范

> 所属文档：Gate 需求文档 · [返回索引](./README.md)

---

## 9.1 面板结构

| 面板 | 控制器类 | 功能 |
|------|----------|------|
| 全局代理 | `GlobalPanelController` | 设置/清除全局环境变量代理（HTTP/HTTPS/NO_PROXY） |
| 应用代理 | `AppPanelController` | 工具列表：搜索/分类/单工具设置/批量操作 |
| 预设 | `PresetPanelController` | 预设列表（左列表+右详情）、新建/应用/删除/设默认 |
| 状态总览 | `StatusPanelController` | 只读展示全局代理+工具代理（分类分组）+当前预设 |
| 连通测试 | `TestPanelController` | 代理连通性测试 + 测试历史 |
| 配置向导 | `WizardPanelController` | 引导首次配置（4步） |

---

## 9.2 颜色系统（USS 变量）

| 变量名 | 值 | 用途 |
|--------|----|------|
| `--color-bg` | `#0f1117` | 主背景 |
| `--color-surface` | `#111827` | 卡片/弹窗背景 |
| `--color-surface-2` | `#1e293b` | 边框、次级表面 |
| `--color-accent` | `#4f8ef7` | 主色调、选中 |
| `--color-accent-hover` | `#6ba3f9` | 主色 hover |
| `--color-success` | `#22c55e` | 已配置状态点 |
| `--color-warning` | `#f59e0b` | 警告 |
| `--color-danger` | `#f87171` | 危险操作、错误 |
| `--color-text` | `#e2e8f0` | 主文本 |
| `--color-text-muted` | `#64748b` | 次要文本、占位符 |
| `--color-nav-hover` | `#1e2436` | 导航 hover |
| `--color-nav-selected` | `#1e2d4f` | 导航选中 |

**字体**：思源黑体 `SourceHanSansSC-Bold SDF.asset`  
**导航**：默认灰 → hover `#1e2436` → 选中 `#1e2d4f` + 蓝字 + 左边框 3px `#4f8ef7`

---

## 9.3 各面板交互规范

### 全局代理面板

```
[状态卡]  HTTP_PROXY | HTTPS_PROXY | NO_PROXY（只读，实时刷新）
[表单]    代理地址输入框
          高级展开：分项 HTTP/HTTPS/NO_PROXY 输入框
          测试连通性 Toggle
[按钮行]  清除代理(危险红) | 刷新(次要) | 应用设置(主要蓝)
[反馈标签] 成功绿/失败红/警告黄，显示 3 秒后消失
```

### 应用代理面板

```
[工具栏]  搜索框（实时过滤，不区分大小写）
          分类下拉（全部/版本控制/包管理器/AI IDE...）
          仅已安装 Toggle
[列表头]  应用名称 | 分类 | 状态 | 操作
[ListView 每行 44px]
          应用名（粗体）+ 分类（灰小字）
          状态点：绿 #22c55e（已配置）/ 灰 #334155（未配置）
          [设置] 按钮 → 打开编辑弹窗
          [清除] 按钮 → 直接清除（需工具已配置代理时才显示）
[批量栏]  已选N个 | 批量代理输入框 | 全选已安装 | 批量设置 | 批量清除
[反馈标签]
```

**编辑弹窗** `edit-overlay`：
- 默认 `display:none`，C# 用 `DisplayStyle.Flex` 显示
- 内容：标题「编辑: {appName}」 + 当前代理值预填输入框
- 「取消」关闭弹窗；「保存」调用 `ToolConfigurator.SetProxy()`，成功后刷新列表行状态点

### 预设面板

```
[左列 210px]
  标题「预设配置集」
  ListView（每项：名称，默认预设加「★」标记）
  [+新建预设] 按钮
[右列 flex]
  名称（蓝色粗体） | 描述 | 创建时间 | 更新时间
  HTTP代理值（黄色） | 工具代理数
  [应用预设(主要)] [设为默认(次要)] [删除(危险)]
[保存区]  预设名称输入框 + [保存当前配置]
[反馈标签]
```

**新建弹窗** `new-preset-overlay`：
- 名称输入框（必填，校验命名规范）+ 描述输入框（可选）
- 「取消」关闭；「创建」调用 `ProfileManager.Save()`，刷新左侧列表

### 状态总览面板

```
[全局代理卡]
  标题「全局代理」+ [刷新] 按钮（右上角）
  HTTP_PROXY: 值（黄色）  [编辑→跳转全局代理面板]
  HTTPS_PROXY: 值
  NO_PROXY: 值

[应用代理卡]
  标题「应用代理」+ 已配置N个（蓝色徽章）
  可滚动工具列表（C# 动态生成，按分类分组）：
    [分类标题行]（灰色小字）
    [工具行] ● 工具名  代理地址（灰色）

[当前预设卡]
  标题「当前预设」
  预设名称（蓝色粗体），若无则显示「(未保存)」
```

### 连通测试面板

```
[表单]
  代理地址输入框 + [使用当前] 按钮（填入当前环境变量代理）
  测试 URL 输入框（默认 https://www.google.com，可修改）
[大按钮] 开始测试（测试中禁用并显示「测试中...」）
[结果区] success卡 / fail卡（默认 hidden，测试后显示其一）
  success卡：✓ + 响应时间 + HTTP状态码 + 目标URL
  fail卡：✗ + 错误信息
[历史卡] ListView max-height:180px
  每条：时间 | 代理地址 | 结果（绿✓/红✗）| 延迟
```

### 配置向导面板

向导分 4 步，每步有「上一步」（非第一步）和「下一步/完成」按钮。

```
[进度条] 步骤 1/4 ●●○○  步骤标题

步骤 1：输入代理地址
  代理地址输入框（placeholder: http://127.0.0.1:7890）
  [历史记录列表]（最多5条，点击填充）
  [测试连通性] 按钮（可选，不阻塞前进）
  校验：非空 + 格式合法（http://host:port）；不合法时行内红色提示

步骤 2：选择要配置的工具
  [全选已安装] Toggle（默认开启）
  工具列表（按分类分组的 ScrollView）：
    每行 CheckBox + 工具名 + 分类标签 + 安装状态点
  未安装的工具显示灰色且默认不选中

步骤 3：设置 NO_PROXY（可选）
  NO_PROXY 输入框（placeholder: localhost,127.0.0.1,*.local）
  说明文字：「不需要代理的地址，逗号分隔」
  [跳过此步骤] 按钮（清空输入框并前进）

步骤 4：保存为预设（可选）
  预设名称输入框（placeholder: 如 office、home）
  描述输入框（可选）
  [跳过，不保存] 按钮
  「完成」按钮

[完成摘要]（步骤 4 点击完成后显示）
  ✓ 已设置代理：http://...
  ✓ 已配置工具：git, npm, pip（N 个）
  ✓ 已保存预设：office
  ℹ 建议运行 gate install-shell-hook 实现持久化
  [关闭向导] 按钮
```

**向导完成后行为**：调用与 CLI `gate wizard` 相同的逻辑（`WizardPanelController` 直接复用 `Gate.Core` 的 Manager 层，不调用 CLI 进程）。

---

## 9.4 异步要求

所有网络操作必须在后台线程执行，严禁在 Unity 主线程调用网络 I/O：

```csharp
private async void OnTestButtonClick() {
    _btnTest.SetEnabled(false);
    _btnTest.text = "测试中...";
    var result = await Task.Run(() => ProxyTester.Test(_proxyInput.value));
    // 回到 Unity 主线程更新 UI
    UpdateResultUI(result);
    _btnTest.SetEnabled(true);
    _btnTest.text = "开始测试";
}
```

---

## 9.5 事件注销要求

所有在 `RegisterCallbacks()` 中注册的事件，必须在 `Dispose()` 中注销，防止 Unity 热重载时内存泄漏：

```csharp
public void Dispose() {
    _searchField?.UnregisterValueChangedCallback(OnSearchChanged);
    _categoryFilter?.UnregisterValueChangedCallback(OnCategoryChanged);
    _installedOnly?.UnregisterValueChangedCallback(OnInstalledOnlyChanged);
    // 所有 RegisterCallback 都要对应 UnregisterCallback
}
```

---

## 9.6 弹窗通用规范

| 属性 | 值 |
|------|----|
| 默认显示 | `display: none` |
| 显示方式 | `element.style.display = DisplayStyle.Flex`（C# 调用） |
| 遮罩 | `position: absolute`，覆盖全面板，`background-color: rgba(0,0,0,0.75)` |
| 面板宽度 | 460px |
| 面板背景 | `#111827`，圆角 14px，padding 28px×32px |
| 按钮布局 | 右对齐，取消在左（次要），确认在右（主要）|
| 关闭方式 | 点击「取消」或点击遮罩区域关闭 |

---

## 9.7 待实现功能（P2）

| 功能 | 说明 |
|------|------|
| 代理历史下拉 | 代理输入框下方显示最近 10 条历史，点击填充 |
| 插件管理面板 | 侧边导航新增「插件」项，支持安装/卸载/校验 |
| Doctor 诊断面板 | 侧边导航新增「诊断」项，实时展示 8 项检查结果 |
| 主题切换 | 深色/浅色切换，设置存入 `config.json` 的 `uiTheme` 字段 |
| 预设快速切换 | 编辑器顶部菜单栏「Gate: [当前预设]」下拉，一键切换 |
