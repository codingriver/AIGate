# Gate 使用文档

## 1. 安装与部署

### 1.1 获取发布包

从 `publish/` 目录获取对应平台的发布包：

```
publish/
├── gate-win-x64/        # Windows 自包含 CLI
├── gate-win-x64-fd/     # Windows 框架依赖 CLI（需安装 .NET 8）
├── gate-linux-x64/
├── gate-osx-x64/
└── ...
```

### 1.2 运行 CLI

**Windows**：
```powershell
# 自包含版本（推荐，无需安装 .NET）
.\gate.exe --help

# 框架依赖版本（需已安装 .NET 8 SDK/Runtime）
dotnet gate.dll --help
```

**Linux/macOS**：
```bash
# 自包含版本
./gate --help

# 或添加执行权限
chmod +x gate
./gate --help
```

> 说明：原 API 服务 (`ProxyTool.API`) 已从当前项目中移除，Gate 现仅提供 CLI 工具。

## 2. CLI 命令详解

### 2.1 env - 环境变量管理

管理当前进程的代理环境变量（HTTP_PROXY、HTTPS_PROXY、NO_PROXY）。

#### 设置代理
```bash
# 基本用法
gate env --http http://proxy.example.com:8080

# 分别指定 HTTP 和 HTTPS
gate env --http http://proxy.example.com:8080 --https https://proxy.example.com:8080

# 设置排除列表（不走代理的地址）
gate env --http http://proxy.example.com:8080 --no-proxy "localhost,127.0.0.1,.internal"

# 设置前验证代理连通性
gate env --http http://proxy.example.com:8080 --verify
```

#### 清除代理
```bash
gate env --clear
```

#### 查看当前配置
```bash
# 不传参数时显示当前用户级环境变量
gate env
```

**输出示例**：
```
当前环境变量代理设置:
  HTTP_PROXY:  http://proxy.example.com:8080
  HTTPS_PROXY: http://proxy.example.com:8080
  NO_PROXY:    (未设置)
```

---

### 2.2 tool - 工具代理配置

为各类开发工具（Git、Npm、Docker 等）配置代理。

#### 列出所有支持的工具
```bash
gate tool --list
```

**输出示例**：
```
支持的工具:

[版本控制]
  git              ✅ 已安装       [已配置]
  svn              ❌ 未安装       [未配置]

[包管理器]
  npm              ✅ 已安装       [未配置]
  pip              ✅ 已安装       [已配置]
  ...
```

#### 为工具设置代理
```bash
gate tool --name npm --proxy http://proxy.example.com:8080
gate tool --name git --proxy http://proxy.example.com:8080
gate tool --name docker --proxy http://proxy.example.com:8080
```

#### 清除工具代理
```bash
gate tool --name npm --clear
gate tool --name git --clear
```

#### 查看工具当前配置
```bash
gate tool --name npm
# 输出: npm 当前代理: HTTP=http://proxy.example.com:8080
```

**注意**：
- 工具必须已安装（在 PATH 中或配置文件存在）才能设置
- 不同工具的配置文件路径不同，如 Git 使用 `~/.gitconfig`，Npm 使用 `~/.npmrc`

---

### 2.3 profile - 配置集管理

保存、加载、切换多套代理配置（适用于公司/家庭/不同项目等场景）。

#### 列出配置集
```bash
gate profile --list
# 或不传参数
gate profile
```

**输出示例**：
```
保存的配置集:
  - company
  - home
  - project-a

默认: company
```

#### 保存当前配置为配置集
```bash
# 先设置好环境变量和各工具代理，再保存
proxy-tool env --http http://company-proxy:8080
proxy-tool tool --name npm --proxy http://company-proxy:8080
gate profile --name company --save
```

#### 加载配置集
```bash
gate profile --name company --load
```

加载后会将配置集内的环境变量应用到当前进程。

#### 删除配置集
```bash
gate profile --name old-profile --delete
```

#### 设置默认配置集
```bash
gate profile --name company --set-default
```

---

### 2.4 test - 代理连通性测试

测试代理是否可用及响应时间。

#### 测试指定代理
```bash
gate test --proxy http://proxy.example.com:8080
```

#### 测试当前环境变量中的代理
```bash
# 不指定 --proxy 时使用 HTTP_PROXY 或 HTTPS_PROXY
gate test
```

#### 指定测试 URL
```bash
gate test --proxy http://proxy.example.com:8080 --url https://www.google.com
```

**输出示例**：
```
测试中...
✅ 连接成功! 响应时间: 125ms
```

或失败时：
```
❌ 连接失败: Connection timed out
```

---

## 3. 典型使用场景

### 3.1 场景一：公司网络需代理

```bash
# 1. 设置环境变量（当前终端生效）
gate env --http http://proxy.company.com:8080 --verify

# 2. 为常用工具设置代理
gate tool --name npm --proxy http://proxy.company.com:8080
gate tool --name git --proxy http://proxy.company.com:8080
gate tool --name go --proxy http://proxy.company.com:8080

# 3. 保存为配置集，下次一键加载
gate profile --name company --save
```

### 3.2 场景二：切换不同代理

```bash
# 加载公司配置
gate profile --name company --load

# 切换到家庭/直连（清除代理）
gate env --clear
gate tool --name npm --clear
gate tool --name git --clear
```

### 3.3 场景三：AI 开发工具代理

```bash
# 为 Cursor、Ollama、OpenAI 等设置代理
gate tool --name cursor --proxy http://proxy.example.com:8080
gate tool --name ollama --proxy http://proxy.example.com:8080
gate tool --name openai --proxy http://proxy.example.com:8080
```

### 3.4 场景四：批量配置

通过 API 可批量设置所有已安装工具的代理，见 [API.md](./API.md)。

---

## 4. API 服务使用

### 4.1 启动服务

```bash
cd publish/proxy-tool-api-win-x64  # 或对应平台目录
.\ProxyTool.API.exe
```

服务启动后监听 `http://localhost:5000`。

### 4.2 快速测试

```bash
# 获取当前代理配置
curl http://localhost:5000/api/v1/proxy/config

# 设置代理
curl -X POST http://localhost:5000/api/v1/proxy/config \
  -H "Content-Type: application/json" \
  -d '{"httpProxy":"http://proxy.example.com:8080"}'

# 测试代理
curl -X POST http://localhost:5000/api/v1/proxy/test \
  -H "Content-Type: application/json" \
  -d '{"proxy":"http://proxy.example.com:8080"}'
```

### 4.3 Swagger UI

开发环境下访问 `http://localhost:5000/swagger` 可交互式测试所有 API。

---

## 5. 打包构建

### 5.1 使用 build.ps1（Windows）

```powershell
# 完整打包（自包含，win/linux/osx，仅 CLI；Core 作为依赖随 CLI 输出）
.\build.ps1

# 框架依赖版本（体积更小，需目标机器有 .NET）
.\build.ps1 -fd

# 指定运行时
.\build.ps1 -Runtimes "win-x64"

# 指定版本号（仅用于脚本标题显示；压缩包版本见 package.ps1）
.\build.ps1 -Version "1.1.0"
```

### 5.2 使用 build.sh（Linux/macOS）

```bash
chmod +x build.sh
./build.sh

# 框架依赖
./build.sh --fd

# 指定运行时
./build.sh --runtimes "linux-x64,osx-x64"
```

输出目录为 `publish/gate-{rid}/`，不生成压缩包。

## 6. 常见问题

### Q: env 设置的代理只对当前进程有效？
A: 是的。`env` 命令仅修改当前进程的环境变量。若需持久化，请使用 `profile --save` 保存配置集，或手动配置系统/用户环境变量。

### Q: 工具显示「未安装」但实际已安装？
A: 检查工具是否在 PATH 中，或配置文件路径是否正确。部分工具（如 Docker）需特殊权限或仅支持提示手动配置。

### Q: 代理格式要求？
A: 支持 `http://host:port`、`https://host:port`、`socks5://host:port` 或 `host:port`。主机需为有效 IP、localhost 或包含点的域名。

### Q: API 服务如何修改端口？
A: 通过环境变量或启动参数，如 `ASPNETCORE_URLS=http://localhost:8080`。
