# Gate

> 跨平台命令行代理管理工具（Gate.CLI + Gate.Core + Gate.Tests）

## 功能概览

- **环境变量代理管理**：快速设置/清除 `HTTP_PROXY`、`HTTPS_PROXY`、`NO_PROXY`
- **常用开发工具代理配置**：一条命令为 `git`、`npm`、`pip`、`docker` 等设置代理
- **配置集（Profile）管理**：保存/加载不同场景的代理配置（公司、家、项目）
- **代理连通性测试**：一键测试代理是否可用及延迟
- **状态总览**：查看当前所有环境变量 + 工具代理配置

## 目录结构

- `src/Gate.Core`：核心库（DLL），封装所有代理逻辑
- `src/Gate.CLI`：命令行程序入口（最终发布的可执行文件）
- `src/Gate.Tests`：自动化测试
- `build.ps1` / `build.sh`：构建发布脚本
- `package.sh`：将 `publish/` 中的输出打包为 `tar.gz`

## 快速开始

### 1. 环境要求

- [.NET SDK 8.0+](https://dotnet.microsoft.com/)（Windows / Linux / macOS 任意一种）

### 2. 构建与打包

#### Windows（PowerShell）

```powershell
# 在仓库根目录
.\build.ps1                      # 自包含，多平台（win/linux/osx）
.\build.ps1 -Runtimes "win-x64"  # 仅构建 Windows x64
.\build.ps1 -fd                  # 框架依赖模式（需目标机安装 .NET）
```

构建完成后，CLI 位于：

- `publish/gate-win-x64/`（自包含）
- `publish/gate-win-x64-fd/`（框架依赖）

#### Linux / macOS

```bash
chmod +x build.sh
./build.sh                         # 自包含，多平台
./build.sh --runtimes "linux-x64"  # 仅构建 Linux x64
./build.sh --fd                    # 框架依赖模式
```

#### 打包为 tar.gz（可选）

```bash
./package.sh 1.0.0   # 版本号可自定义，默认 1.0.0
```

产物位于 `releases/` 目录，如：

- `releases/gate-v1.0.0-win-x64.tar.gz`
- `releases/gate-v1.0.0-linux-x64.tar.gz`

## CLI 使用教程

下面示例假设你已经进入某个平台的发布目录，例如：

```powershell
cd publish/gate-win-x64
.\gate.exe --help
```

### 1. 管理环境变量代理（env）

#### 设置代理

```bash
# 基本用法（HTTP/HTTPS 使用同一代理）
gate env --http http://proxy.example.com:8080

# 设置排除列表（不走代理的地址）
gate env --http http://proxy.example.com:8080 --no-proxy "localhost,127.0.0.1,.internal"

# 设置前先测试代理是否可用
gate env --http http://proxy.example.com:8080 --verify
```

#### 清除 / 查看代理

```bash
# 清除当前进程代理设置
gate env --clear

# 查看当前环境变量代理配置
gate env
```

### 2. 为工具设置代理（tool）

#### 查看支持的工具

```bash
gate tool --list
```

#### 设置 / 清除某个工具代理

```bash
# 为 git 设置代理
gate tool --name git --proxy http://proxy.example.com:8080

# 查看 git 当前代理
gate tool --name git

# 清除 git 代理
gate tool --name git --clear
```

### 3. 配置集管理（profile）

```bash
# 列出所有已保存的配置集
gate profile --list

# 保存当前环境变量 + 工具代理为一个配置集
gate profile --name company --save

# 加载配置集
gate profile --name company --load

# 设置默认配置集
gate profile --name company --set-default

# 删除配置集
gate profile --name old-profile --delete
```

### 4. 测试代理连通性（test）

```bash
# 指定代理测试
gate test --proxy http://proxy.example.com:8080

# 使用当前环境变量中的代理测试
gate test
```

### 5. 一次性查看当前所有代理状态（status）

```bash
gate status
```

输出内容包括：

- 当前用户级环境变量代理（HTTP_PROXY / HTTPS_PROXY / NO_PROXY）
- 所有已配置的工具代理（仅显示真正有配置的工具）
- 已保存的配置集及默认配置集

## 开发与测试

在仓库根目录：

```bash
dotnet test src/Gate.Tests/Gate.Tests.csproj
```

更多开发细节和 API 说明可参考：

- `DEVELOPMENT.md`
- `docs/USAGE.md`
- `docs/API.md`