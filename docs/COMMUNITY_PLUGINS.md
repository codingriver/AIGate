# Gate 社区插件商店方案

> 开源协作 · 社区驱动 · 插件生态

---

## 一、背景与目标

Gate 内置 200+ 工具，但工具生态持续增长。通过社区插件让用户能够：
1. **发现** 社区贡献的工具插件
2. **安装** 一条命令装好插件
3. **贡献** 自己开发的工具插件
4. **维护** 更新和修复已有插件

---

## 二、可行性评估

| 方案 | 代表产品 | 复杂度 | 适合Gate |
|------|----------|--------|----------|
| 技能商店（OpenClaw模式） | OpenClaw Skills | 中 | 部分 |
| VS Code 扩展商店 | marketplace.visualstudio.com | 高 | 否 |
| **Git 仓库索引（推荐）** | Homebrew Tap | **低** | **是** |
| 包注册表 | npm/cargo | 中 | 否 |

**结论：可行。** 推荐「轻量 GitHub 索引模式」。Gate 工具插件本质是一个 C# 类或 JSON 声明，无需独立服务器和审核团队。

---

## 三、当前代码支持情况

### 已支持
- `ToolRegistry.Register()` — 动态注册工具
- `IToolConfiguratorPlugin` 接口 — 插件契约已定义
- `PluginManager` 扫描框架 — 目录加载框架
- `plugin.json` 清单格式 — 已定义

### 尚未实现（需补充）
- `gate plugin install/list/update/remove` CLI 命令
- 插件索引拉取与缓存（`index.json`）
- 声明式 JSON 插件解析器（无需编译 C#）
- Unity GUI 插件商店面板
- SHA256 校验与安全验证

---

## 四、整体架构

```
GitHub: gate-community/gate-plugins
  index.json                    插件总索引
  plugins/
    mytool/
      plugin.json               插件清单
      plugin-descriptor.json    声明式配置（可选，无需DLL）
      README.md
    anothertool/
      ...

本地: ~/.gate/plugins/
  mytool/
    plugin.json
    MyToolConfigurator.dll      DLL插件
  anothertool/
    plugin-descriptor.json      声明式插件
```

---

## 五、插件索引格式

`index.json`：
```json
{
  "version": 1,
  "updated": "2026-03-10",
  "plugins": [
    {
      "id": "mytool-configurator",
      "name": "MyTool Proxy Configurator",
      "description": "为 MyTool 配置 HTTP 代理",
      "version": "1.0.0",
      "author": "community-user",
      "type": "ToolConfigurator",
      "tags": ["mytool"],
      "downloadUrl": "https://github.com/.../releases/latest/download/plugin.zip",
      "sha256": "abc123...",
      "verified": false
    }
  ]
}
```

---

## 六、声明式插件（JSON，无需编译）

约 80% 的工具只需设置环境变量或写配置文件，无需 C# 代码：

```json
{
  "$schema": "gate-plugin-v1",
  "type": "declarative",
  "toolName": "mytool",
  "displayName": "MyTool",
  "category": "自定义工具",
  "detection": {
    "executable": "mytool",
    "configFile": "~/.mytoolrc"
  },
  "proxy": {
    "method": "env",
    "envVars": ["MYTOOL_PROXY", "MYTOOL_HTTP_PROXY"]
  }
}
```

---

## 七、CLI 插件命令

```bash
gate plugin list                        # 列出可用插件
gate plugin list --search mytool        # 搜索
gate plugin install mytool-configurator # 安装
gate plugin install https://github.com/user/repo  # 从URL安装
gate plugin update mytool-configurator  # 更新
gate plugin remove mytool-configurator  # 卸载
gate plugin info mytool-configurator    # 查看详情
```

安装流程：
```
1. 拉取 index.json（本地缓存1小时）
2. 查找匹配插件
3. 下载 plugin.zip
4. 验证 SHA256
5. 解压到 ~/.gate/plugins/mytool/
6. 验证插件格式
7. 注册到本地插件列表
```

---

## 八、社区贡献流程

### 方式一：声明式插件（推荐新手）

1. Fork `gate-community/gate-plugins`
2. 在 `plugins/yourtools/` 创建 `plugin-descriptor.json`
3. 创建 `README.md`
4. 提交 Pull Request
5. 审核通过后自动更新 `index.json`

### 方式二：DLL 插件

1. 参考 [PLUGIN_DEVELOPMENT.md](./PLUGIN_DEVELOPMENT.md)
2. 创建独立 GitHub 仓库
3. 实现 `IToolConfiguratorPlugin`
4. 配置 GitHub Actions 自动构建发布
5. 提 PR 更新 `index.json`

### 审核标准

| 项目 | 要求 |
|------|------|
| 功能 | 正确设置/清除代理，不影响其他工具 |
| 安全 | 不读取/上传用户私密数据 |
| 质量 | 有 README，说明工具和配置方式 |
| 兼容 | 实现接口所有方法，不抛未捕获异常 |
| 命名 | id 格式：`toolname-configurator` |

---

## 九、Unity GUI 插件面板

新增「插件」导航项，面板布局：

```
插件商店
+--------------------------------------------------+
| [搜索插件...]  [分类 v]  [发现] [已安装]           |
+------------------------------------------------- +
| mytool-configurator         v1.0.0               |
| 为 MyTool 配置 HTTP 代理    作者: user            |
|                                       [安装]     |
+--------------------------------------------------+
| anothertool-configurator    v2.1.0    [已安装]   |
| AnotherTool 代理配置                  [更新][卸载]|
+--------------------------------------------------+
```

C# 实现要点：
- `PluginStorePanelController` 调用 `PluginManager.FetchIndexAsync()`
- 安装按钮触发 `PluginManager.InstallAsync(id)`
- 安装完成后调用 `ToolRegistry.Register()` 热加载，无需重启

---

## 十、路线图

| 阶段 | 内容 | 预计 |
|------|------|------|
| v1.0 | 声明式JSON插件 + 手动复制到插件目录 | 已可实现 |
| v1.1 | `gate plugin install/list` CLI命令 | 1-2周 |
| v1.2 | `gate-community/gate-plugins` 仓库建立 | 2-4周 |
| v1.3 | Unity GUI 插件商店面板 | 4-6周 |
| v2.0 | 自动化审核 + 下载统计 + star系统 | 3月+ |

---

## 十一、与 OpenClaw 技能商店对比

| 维度 | OpenClaw Skills | Gate Plugin Store |
|------|-----------------|-------------------|
| 内容类型 | AI 提示词/技能 | 工具代理配置器 |
| 分发格式 | Markdown/JSON | JSON/DLL |
| 安装方式 | 复制文件 | gate plugin install |
| 运行方式 | AI 解释执行 | .NET 动态加载 |
| 安全模型 | 沙箱（AI执行） | 进程内加载（需信任） |
| 维护成本 | 低 | 低 |
| **适合Gate** | 部分参考 | **推荐本方案** |

Gate 插件比 AI 技能更接近传统包管理，直接参考 Homebrew Tap 和 VS Code Extension 的轻量版更合适。
