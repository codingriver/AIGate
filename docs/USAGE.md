# Gate CLI 使用文档

## 1. 安装与部署

```
publish/
+-- gate-win-x64/        Windows 自包含 CLI
+-- gate-linux-x64/
+-- gate-osx-x64/
```

```powershell
# Windows
.\gate.exe --help

# Linux / macOS
./gate --help
```

---

## 2. 命令总览

| 命令 | 别名 | 说明 |
|------|------|------|
| `gate global` | `env` | 全局代理环境变量管理 |
| `gate app` | `tool` | 应用代理配置（支持批量） |
| `gate preset` | `profile` | 预设配置集管理 |
| `gate info` | `status`, `show` | 当前代理状态总览 |
| `gate test` | `check` | 代理连通性测试 |
| `gate set` | — | 一站式快速配置 |
| `gate apply <name>` | — | 直接应用预设 |
| `gate list [apps\|presets]` | — | 统一列出资源 |
| `gate wizard` | — | 交互式配置向导 |

### 常用短选项

| 短选项 | 长选项 | 说明 |
|--------|--------|------|
| `-p` | `--proxy` | 代理地址（同时设置 HTTP/HTTPS） |
| `-H` | — | 单独指定 HTTP 代理 |
| `-S` | — | 单独指定 HTTPS 代理 |
| `-n` | `--name` | 工具/预设名称 |
| `-l` | `--list` | 列出信息 |
| `-c` | `--clear` | 清除配置 |
| `-v` | `--verify` | 测试代理连通性 |
| `-g` | `--global` | 设置全局代理（set 命令） |
| `-a` | `--app` | 指定应用（set 命令） |

---

## 3. 命令详解

### 3.1 global / env

```bash
# 设置全局代理（-p 同时设置 HTTP/HTTPS）
gate global -p http://proxy:8080

# 分别指定
gate global -H http://proxy:8080 -S https://proxy:8080

# 设置并验证
gate global -p http://proxy:8080 --verify

# 设置排除列表
gate global -p http://proxy:8080 --no-proxy "localhost,127.0.0.1"

# 清除
gate global --clear

# 查看当前
gate global
gate env   # 等价
```

### 3.2 app / tool

```bash
# 列出所有支持的应用
gate app -l

# 为单个应用设置代理
gate app -n git -p http://proxy:8080

# 批量设置
gate app -n git,npm,yarn,pip -p http://proxy:8080

# 查看当前配置
gate app -n git

# 清除
gate app -n git -c
gate app -n git,npm -c   # 批量清除
```

### 3.3 preset / profile

```bash
# 列出
gate preset
gate preset -l

# 保存
gate preset -n office --save

# 加载
gate preset -n office --load

# 设置默认
gate preset -n office --set-default

# 删除
gate preset -n old --delete
```

### 3.4 info / status / show

```bash
gate info
gate status   # 别名
gate show     # 别名
```

输出：全局代理 + 已配置工具（按分类）+ 预设列表。

### 3.5 test / check

```bash
# 测试指定代理
gate test -p http://proxy:8080

# 测试当前环境变量
gate test

# 指定 URL
gate test -p http://proxy:8080 --url https://github.com

gate check -p http://proxy:8080   # 别名
```

### 3.6 set — 一站式配置

```bash
# 全局 + 应用同时配置
gate set -g http://proxy:8080 -a git,npm

# 仅全局
gate set -g http://proxy:8080

# 设置前测试
gate set -g http://proxy:8080 -a git,npm --verify
```

### 3.7 apply

```bash
gate apply office   # 直接应用预设，等价于 gate preset -n office --load
```

### 3.8 list

```bash
gate list              # 预设（默认）
gate list presets      # 预设
gate list apps         # 应用
```

### 3.9 wizard

```bash
gate wizard
```

引导步骤：
1. 输入全局代理地址
2. 选择要配置的应用
3. 设置 NO_PROXY
4. 保存为预设（可选）

---

## 4. 典型场景

### 场景一：公司网络快速配置

```bash
gate set -g http://proxy.company.com:8080 -a git,npm,pip --verify
gate preset -n company --save
```

### 场景二：切换代理场景

```bash
gate apply company   # 应用公司预设
gate apply home      # 切换到家庭预设
```

### 场景三：AI 工具代理

```bash
gate app -n cursor,ollama,openai -p http://proxy:8080
```

### 场景四：查看完整状态

```bash
gate info
```

---

## 5. 构建说明

```powershell
# Windows 全平台
.\build.ps1

# 仅 Windows
.\build.ps1 -Runtimes win-x64

# 框架依赖（更小体积）
.\build.ps1 -fd
```

```bash
# Linux / macOS
./build.sh
./build.sh --runtimes linux-x64
```

---

## 6. 常见问题

**Q: 代理格式要求？**
A: 需为完整 URL：`http://host:port`、`https://host:port`、`socks5://host:port`。

**Q: 设置的代理只对当前进程有效？**
A: 是的。若需持久化请使用 `gate preset --save` 并在 shell 配置文件中调用 `gate apply <name>`。

**Q: 工具显示「未安装」但实际已装？**
A: 检查工具是否在 PATH 中可执行。

**Q: 如何查看支持哪些应用？**
A: 运行 `gate list apps` 或 `gate app -l`。

**Q: Unity GUI 如何使用？**
A: 在 Unity 中创建 GameObject，挂载 `GatePanelController`，并在 Inspector 中绑定 `UIDocument` 和各面板 `VisualTreeAsset`。
