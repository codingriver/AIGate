# 06 CLI 命令规范

> 所属文档：Gate 需求文档 · [返回索引](./README.md)

---

## 6.1 全命令列表

| 命令 | 说明 | 实现文件 |
|------|------|----------|
| `gate` | 状态总览 | `Program.cs` |
| `gate set` | 设置全局代理 | `Program.cs` |
| `gate clear` | 清除代理 | `Program.cs` |
| `gate app` | 单工具代理管理 | `Program.cs` |
| `gate apps` | 列出所有工具 | `Program.cs` |
| `gate env` | 环境变量三层详情 | `Program.cs` |
| `gate preset` | 预设管理（含子命令） | `PresetCommands.cs` |
| `gate test` | 测试代理连通性 | `Program.cs` |
| `gate list` | 列出工具/预设（`gate apps` 和 `gate preset` 的组合视图） | `Program.cs` |
| `gate path` | 工具路径管理 | `Program.cs` |
| `gate history` | 代理历史记录 | `HistoryCommands.cs` |
| `gate wizard` | 交互式配置向导 | `WizardCommand.cs` |
| `gate doctor` | 诊断报告 | `DoctorCommand.cs` |
| `gate plugin` | 插件管理（含子命令） | `PluginCommands.cs` |
| `gate export-all` | 导出全部配置 | `MigrationCommands.cs` |
| `gate import-all` | 导入全部配置 | `MigrationCommands.cs` |
| `gate completion` | Shell Tab 补全脚本 | `CompletionCommand.cs` |
| `gate install-shell-hook` | 安装 Shell 启动钩子 | `ShellHookCommand.cs` |
| `gate reset` | 完全重置（危险操作） | `Program.cs` |
| `gate info` | 状态总览别名 | `Program.cs` |
| `gate config` | 全局配置管理 | `ConfigCommand.cs`（待建） |
| `gate watch` | 代理连通性定时监测（P1） | `WatchCommand.cs`（待建） |
| `gate pick` | 多代理延迟对比自动选择（P1） | `PickCommand.cs`（待建） |
| `gate backup` | 备份工具配置文件（P1） | `BackupCommand.cs`（待建） |
| `gate rollback` | 回滚到备份（P1） | `BackupCommand.cs`（待建） |
| `gate pac` | PAC 文件生成与托管（P1） | `PacCommand.cs`（待建） |
| `gate mcp` | MCP 服务器（P0 待实现） | `McpCommand.cs`（未建） |

---

## 6.2 子命令结构

### gate preset
```
gate preset
gate preset save <name> [--desc <text>]
gate preset load <name>
gate preset del <name>
gate preset rename <old> <new>
gate preset set-default <name>
gate preset show <name>
gate preset export <name> [file]
gate preset import <file> [--as <name>] [--overwrite]
```

### gate plugin
```
gate plugin list
gate plugin install <file> [--overwrite]           # 本地文件安装
gate plugin install <id> [--force]                # 社区索引安装（P1，id 不含路径分隔符）
gate plugin remove <name> [--force]
gate plugin validate <file>
gate plugin show <name>
gate plugin search <keyword>                      # 搜索社区索引（P1）
gate plugin update <name>                         # 更新指定社区插件（P1）
gate plugin update --all                          # 更新所有社区插件（P1）
```

> **区分本地安装和社区安装**：`gate plugin install` 参数含路径分隔符（`/`、`\`、`.`）时视为本地文件路径；否则视为社区插件 id（P1 实现后生效）。

### gate history
```
gate history
gate history clear
```

### gate config（P1）
```
gate config list                          # 列出所有全局配置项及当前值
gate config get <key>                     # 读取单个配置项
gate config set <key> <value>             # 写入配置项
gate config reset                         # 恢复所有配置项为默认值
```

**可配置项**：

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `defaultPreset` | string | `""` | 默认加载的预设名 |
| `defaultTestUrl` | string | `https://www.google.com` | `gate test` 默认测试目标 |
| `testTimeoutMs` | int | `10000` | 代理测试超时（毫秒） |
| `autoSaveHistory` | bool | `true` | `gate set` 时自动记录历史 |
| `maxHistoryCount` | int | `20` | 历史记录最大条数 |
| `proxyExpiresAt` | string | `""` | 代理过期时间（ISO8601），空值表示不过期（P1 `--expires` 写入） |
| `uiTheme` | string | `"dark"` | Unity GUI 主题，`"dark"` 或 `"light"`（P2 主题切换写入） |

### gate backup（P1）
```
gate backup                               # 备份所有已安装工具配置文件
gate backup list                          # 列出备份（带时间戳和工具数）
gate backup show <timestamp>             # 查看指定备份包含的文件列表
```

### gate rollback（P1）
```
gate rollback                             # 回滚到最近一次备份
gate rollback --index 2                   # 回滚到第 N 份备份（1-based）
gate rollback --list                      # 同 gate backup list
gate rollback --dry-run                   # 预览回滚操作，不实际修改
```

### gate pac（P1）
```
gate pac generate [options]               # 生成 PAC 文件
  --proxy <url>                           # 代理地址（默认使用当前环境变量代理）
  --no-proxy <list>                      # 排除列表（默认使用当前 NO_PROXY）
  --output <file>                        # 输出文件路径（默认 proxy.pac）
gate pac serve [options]                  # 本地托管 PAC 文件
  --port <n>                             # 监听端口（默认 8889）
  --file <file>                          # 指定 PAC 文件（不指定则动态生成）
```

---

## 6.3 向后兼容旧命令

| 旧命令 | 新命令 | 处理方式 |
|--------|--------|----------|
| `gate global` | `gate env` | 隐藏，功能保留，执行时提示「请使用 gate env」 |
| `gate tool` | `gate app` | 同上 |
| `gate profile` | `gate preset` | 同上 |
| `gate apply <name>` | `gate preset load <name>` | 同上 |
| `gate status`/`show` | `gate`/`gate info` | 同上 |
| `gate check` | `gate test` | 同上 |

隐藏命令不出现在 `--help` 中，但执行时正常运行并输出迁移提示。

---

## 6.4 输出格式规范

| 类型 | 彩色模式 | NoColor 模式 | 含义 |
|------|----------|--------------|------|
| 成功 | `✓` 绿色 | `[OK]` | 操作完成 |
| 错误 | `✗` 红色 | `[ERR]` | 操作失败 |
| 警告 | `⚠` 黄色 | `[WARN]` | 操作完成但有注意事项 |
| 信息 | `ℹ` 青色 | `[INFO]` | 提示信息 |

`--json` 模式下，所有输出为合法 JSON 对象，无任何 ANSI 代码和装饰字符。

---

## 6.5 退出码规范

| 退出码 | 含义 | 触发场景 |
|--------|------|----------|
| `0` | 成功 | 所有正常完成的操作 |
| `1` | 参数错误 | 命令行参数不合法、缺少必填项 |
| `2` | 资源未找到 | 工具名不存在、预设名不存在、文件不存在 |
| `3` | 代理测试失败 | `--verify` 或 `gate test` 测试不通过 |
| `4` | 写入失败 | 配置文件无写入权限、磁盘空间不足 |
| `5` | 用户取消 | 交互式操作被用户中断（Ctrl+C 或选择取消） |

---

## 6.6 `--help` 输出规范

每个命令的 `--help` 必须包含：
1. 一行简短描述
2. 用法示例（至少 1 个）
3. 所有选项说明
4. 相关命令提示（`See also`）

示例格式：
```
设置全局代理

用法:
  gate set <proxy> [tools] [options]

示例:
  gate set http://127.0.0.1:7890
  gate set http://127.0.0.1:7890 git,npm --verify

选项:
  --verify          设置前测试连通性
  --no-proxy <list> 设置 NO_PROXY 排除列表
  --history-index N 从历史记录第 N 条设置

相关命令:
  gate clear        清除代理
  gate test         测试代理连通性
  gate preset save  保存当前配置为预设
```

---

## 6.7 交互式向导（gate wizard）步骤

```
步骤 1/4  输入代理地址
  > 请输入代理地址（如 http://127.0.0.1:7890）：_
  > 历史记录：[1] http://127.0.0.1:7890  [2] http://proxy.company.com:8080
  > 输入编号直接选择，或输入新地址

步骤 2/4  选择要配置的工具
  已安装工具（输入编号，多选用逗号，a=全选，s=跳过）：
  [1] git          [2] npm          [3] pip
  [4] docker       [5] curl         [6] kubectl
  > _

步骤 3/4  设置 NO_PROXY（可跳过）
  > 输入排除列表（如 localhost,127.0.0.1）或直接回车跳过：_

步骤 4/4  保存为预设（可跳过）
  > 预设名称（回车跳过）：_
```

向导完成后输出操作摘要：

```
✓ 已设置代理：http://127.0.0.1:7890
✓ 已配置工具：git, npm, pip, docker（4 个）
⚠ 已跳过工具：kubectl（未安装）
✓ 已保存预设：office
ℹ 代理仅在当前终端有效，运行以下命令持久化：
  gate install-shell-hook
```

`--json` 模式下向导不可用（非交互命令，`gate wizard --json` 输出错误退出码 `1`）。
