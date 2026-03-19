# 06 CLI 命令系统 — 需求文档

> 所属项目：Gate · 模块化文档 v2.0  
> 关联文档：[数据模型](./07-数据模型-需求文档.md)

---

## 1. 完整命令表

### 全局代理
| 命令 | 说明 |
|------|------|
| `gate set <proxy> [tools]` | 设置全局代理（同时写入 HTTP_PROXY、HTTPS_PROXY、ALL_PROXY） |
| `gate clear [--all]` | 清除全局代理（含 ALL_PROXY）；`--all` 含插件工具（A12）；过期自动清除仅清全局代理并置 `proxyExpiresAt` 为 null（B7、B8） |
| `gate env [--write-registry]` | 显示三层环境变量 |
| `gate history [clear]` | 查看/清除历史记录；输出格式：表格（序号、地址、最后使用时间）；`--json` 时输出数组（B6） |
| `gate install-shell-hook` | 安装 Shell 持久化钩子（动态读取模式，A2） |
| `gate env --export-script <shell>` | 输出 shell 代理导出脚本；无代理时输出空脚本；有代理时输出三行 export 语句（B4） |

### 工具代理
| 命令 | 说明 |
|------|------|
| `gate app <tool> [<proxy>]` | 查看/设置工具代理 |
| `gate app <tool> --clear` | 清除工具代理 |
| `gate app --all <proxy> [--except ...]` | 批量设置 |
| `gate apps [--installed] [--category]` | 列出工具 |
| `gate path [options]` | 管理自定义工具路径 |

### 预设
| 命令 | 说明 |
|------|------|
| `gate preset` | 列出预设 |
| `gate preset save/load/del/rename` | 增删改加载 |
| `gate preset set-default/show/export/import` | 默认/详情/导入导出 |

### 测试
| 命令 | 说明 |
|------|------|
| `gate test [<proxy>] [--url] [--timeout] [--json]` | 连通性测试 |
| `gate test --compare <p1>,<p2>,...` | 多代理对比 |
| `gate watch [--interval] [--notify]` | 定时监测（P1） |
| `gate pick <p1>,...[--apply]` | 选最快节点（P1） |

### 插件
| 命令 | 说明 |
|------|------|
| `gate plugin list/install/remove/validate/show` | 插件管理 |
| `gate plugin search/update` | 社区插件（P1） |

### 配置迁移
| 命令 | 说明 |
|------|------|
| `gate export-all <file>` | 导出全部配置 |
| `gate import-all <file> [--overwrite]` | 导入全部配置 |
| `gate backup / rollback` | 备份/回滚（P1） |
| `gate reset [--force]` | 完全重置 Gate 数据 |

### 工具与诊断
| 命令 | 说明 |
|------|------|
| `gate` / `gate info` | 状态总览 |
| `gate doctor` | 诊断报告（8 项检查） |
| `gate wizard` | 交互式向导 |
| `gate completion <shell> [install]` | Tab 补全脚本 |
| `gate config list/get/set/reset` | 全局配置管理（P1） |
| `gate pac generate/serve` | PAC 文件（P1） |
| `gate mcp [--sse --port]` | MCP 服务器（P0） |

---

## 2. 全局标志

| 标志 | 说明 |
|------|------|
| `--json` | 输出 JSON，不含 ANSI 转义码；与 `--quiet` 同时使用时 `--json` 优先，仍输出完整 JSON（B32） |
| `--quiet` / `-q` | 仅输出错误；若与 `--json` 同时使用，`--json` 优先（B32） |
| `--no-color` | 禁用彩色（自动检测 `NO_COLOR` 环境变量） |
| `--plain` | 纯文本，无表格框线 |
| `--version` / `-v` | 显示版本后退出 |
| `--help` / `-h` | 显示帮助 |

---

## 3. 退出码规范

| 退出码 | 含义 |
|--------|------|
| 0 | 成功（可含警告摘要） |
| 1 | 参数错误（格式不合规、缺少必填参数） |
| 2 | 资源未找到（工具/预设/历史索引不存在） |
| 3 | 连通性测试失败 |
| 4 | 文件/目录权限不足 |
| 5 | 内部错误（未预期异常） |

---

## 4. Tab 补全

**命令**：`gate completion <shell>`（输出补全脚本到 stdout）  
**支持 shell**：`bash`、`zsh`、`fish`、`pwsh`

补全范围：
- 所有子命令名称
- `gate app <TAB>` → 所有工具名（214+，动态加载）
- `gate preset load <TAB>` → 已保存预设名（动态读取）
- `gate plugin remove <TAB>` → 已安装插件名
- `--shell` 选项 → `bash zsh fish pwsh`

---

## 5. `gate doctor` 诊断项

| # | 检查项 | 通过条件 |
|---|--------|----------|
| 1 | Gate 版本 | 已安装，版本字段可读 |
| 2 | 全局代理状态 | 三层环境变量可读 |
| 3 | Shell Hook | profile 文件含 Gate Hook |
| 4 | 工具加载 | ToolRegistry 无加载错误 |
| 5 | 插件格式 | 所有已安装插件 schema 合法 |
| 6 | 数据目录权限 | `{DataDir}` 可读写 |
| 7 | 默认预设 | 若配置了默认预设，文件存在 |
| 8 | 连通性 | 当前代理可访问默认测试 URL |

> **B28**：第 8 项无代理时显示 `⚠`，`checkDesc` 显示「当前未设置代理，跳过连通性检测」，不显示 `✗`。

输出示例：
```
✓ Gate 版本        v1.2.3
✓ 全局代理         http://127.0.0.1:7890
✗ Shell Hook       未找到（运行 gate install-shell-hook 修复）
✓ 工具加载         214 个工具加载成功
✓ 插件格式         2 个插件，均合法
✓ 数据目录权限     C:\Users\user\AppData\Local\Gate
✓ 默认预设         office
✓ 连通性           123ms
```

---

## 6. `gate wizard` 交互式向导

向导流程（使用 Spectre.Console 等交互库）：
1. 是否配置全局代理？→ 输入代理地址（含格式校验）
2. 是否同时配置工具代理？→ 多选工具列表
3. 是否保存为预设？→ 输入预设名称
4. 是否安装 Shell Hook？→ 选择 shell 类型
5. 是否立即测试连通性？

> **B27**：`gate wizard` 是交互式 CLI，需键盘输入。GUI 命令控制台为只读输出区，无法完成交互。GUI 里 `btnWizard` 应在系统终端中打开（`Process.Start("cmd.exe", "/k gate wizard")`），不在控制台面板内执行。

> **B29**：`gate completion install` 只写 Tab 补全脚本；`gate install-shell-hook` 写代理动态加载钩子；两者功能独立，互不替代。

> **B30**：`GateCliRunner.Run` 实现为启动 `gate.exe` 子进程（行为与用户在终端运行完全一致）；默认超时 30s。

> **B31**：GUI 命令控制台的命令历史不持久化，每次打开 Editor 窗口重新清空。

---

## 7. 向后兼容

旧命令别名（隐藏，不出现在 `--help` 中）：

| 旧命令 | 新命令 |
|--------|--------|
| `gate set-proxy <p>` | `gate set <p>` |
| `gate clear-proxy` | `gate clear` |
| `gate tool <t> <p>` | `gate app <t> <p>` |

---

## 8. `gate config` 全局配置管理（P1）

```
gate config list               # 显示所有全局配置项
gate config get <key>          # 获取单项
gate config set <key> <value>  # 设置单项
gate config reset [<key>]      # 重置（单项或全部）
```

配置项（存储于 `{DataDir}/config.json`）：

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `autoSaveHistory` | bool | `true` | 是否自动保存代理历史 |
| `maxHistoryCount` | int | `20` | 历史记录最大条数 |
| `defaultTestUrl` | string | `https://www.google.com` | 默认测试 URL |
| `defaultTimeout` | int | `10000` | 默认超时（ms） |
| `defaultPreset` | string | `""` | 默认预设名 |
| `pluginRegistryUrl` | string | `""` | 社区插件索引 URL |
| `colorEnabled` | bool | `true` | 是否启用彩色输出 |

---

## 9. 验收标准

- [ ] 所有命令 `--help` 输出正确，无遗漏选项
- [ ] `--json` 模式下所有输出均为合法 JSON，不含 ANSI 码
- [ ] `--quiet` 模式下成功操作无任何 stdout 输出
- [ ] `gate completion bash` 输出可 `source` 的补全脚本
- [ ] `gate doctor` 8 项全部检查，✗ 项附修复建议
- [ ] 退出码与规范完全一致
- [ ] 旧命令别名正常工作，不出现在 `--help` 中
