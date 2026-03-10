# ProxyTool 插件开发指南

本文档介绍如何为 ProxyTool 开发自定义插件。

## 插件类型

ProxyTool 支持以下类型的插件：

1. **ToolConfigurator** - 工具代理配置器
2. **ProxyTester** - 代理测试器
3. **ConfigParser** - 配置文件解析器
4. **StorageBackend** - 存储后端
5. **UiExtension** - UI 扩展
6. **NotificationService** - 通知服务

## 快速开始

### 1. 创建插件项目

```bash
# 创建类库项目
dotnet new classlib -n MyToolConfigurator -f netstandard2.0
```

### 2. 添加依赖

编辑 .csproj 文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ProxyTool.PluginCore" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### 3. 实现插件接口

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProxyTool.PluginCore;
using ProxyTool.Models;

public class MyToolConfigurator : IToolConfiguratorPlugin
{
    public string ToolName => "mytool";
    public string Category => "自定义工具";
    
    public PluginMetadata GetMetadata()
    {
        return new PluginMetadata
        {
            Id = "mytool-configurator",
            Name = "My Tool Configurator",
            Description = "我的自定义工具代理配置器",
            Version = "1.0.0",
            Author = "Your Name",
            Type = PluginType.ToolConfigurator,
            Tags = new List<string> { "custom", "mytool" }
        };
    }
    
    public Task InitializeAsync(Dictionary<string, object> config)
    {
        // 初始化插件
        return Task.CompletedTask;
    }
    
    public Task ShutdownAsync()
    {
        // 清理资源
        return Task.CompletedTask;
    }
    
    public PluginState GetState()
    {
        return new PluginState
        {
            Metadata = GetMetadata(),
            Status = PluginStatus.Enabled
        };
    }
    
    public string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (home != null)
            return Path.Combine(home, ".mytoolrc");
        return null;
    }
    
    public bool IsInstalled()
    {
        // 检查工具是否安装
        return File.Exists(DetectConfigPath());
    }
    
    public ProxyConfig? GetCurrentConfig()
    {
        var config = new ProxyConfig();
        // 读取当前配置
        return config;
    }
    
    public bool SetProxy(string proxyUrl)
    {
        // 设置代理
        Console.WriteLine($"设置 {ToolName} 代理: {proxyUrl}");
        return true;
    }
    
    public bool ClearProxy()
    {
        // 清除代理
        return true;
    }
}
```

### 4. 创建插件清单

在插件输出目录创建 `plugin.json`：

```json
{
  "id": "mytool-configurator",
  "name": "My Tool Configurator",
  "description": "我的自定义工具代理配置器",
  "version": "1.0.0",
  "author": "Your Name",
  "type": "ToolConfigurator",
  "entryPoint": "MyToolConfigurator",
  "dllPath": "./MyToolConfigurator.dll",
  "tags": ["custom", "mytool"]
}
```

### 5. 打包发布

```bash
# 构建插件
dotnet build -c Release

# 打包
dotnet pack -c Release
```

## 插件目录结构

```
plugins/
├── mytool/
│   ├── plugin.json       # 插件清单
│   └── MyToolConfigurator.dll  # 插件 DLL
└── another-tool/
    └── ...
```

## 注册插件

### 方式 1: 静态注册

在 `ToolRegistry.cs` 中添加：

```csharp
new MyToolConfigurator(),
```

### 方式 2: 动态加载

```csharp
var pluginManager = new PluginManager();
pluginManager.AddPluginDirectory("./plugins");
await pluginManager.ScanAndLoadPluginsAsync();
```

## 插件 API

### IProxyToolPlugin

所有插件必须实现的接口：

```csharp
public interface IProxyToolPlugin
{
    PluginMetadata GetMetadata();
    Task InitializeAsync(Dictionary<string, object> config);
    Task ShutdownAsync();
    PluginState GetState();
}
```

### IToolConfiguratorPlugin

工具配置器插件接口：

```csharp
public interface IToolConfiguratorPlugin : IProxyToolPlugin
{
    string ToolName { get; }
    string Category { get; }
    string? DetectConfigPath();
    bool IsInstalled();
    ProxyConfig? GetCurrentConfig();
    bool SetProxy(string proxyUrl);
    bool ClearProxy();
}
```

## 最佳实践

1. **错误处理** - 始终处理异常，不要让插件崩溃
2. **日志记录** - 使用 Console.WriteLine 输出日志
3. **配置验证** - 验证用户输入的代理 URL
4. **回滚支持** - 失败时尝试回滚到之前状态
5. **文档** - 提供清晰的使用说明

## 示例插件

参考 `templates/plugin-example.json` 获取完整的插件示例。

## 发布到插件仓库

1. 创建 GitHub 仓库 `proxytool-plugins`
2. 在 `plugins.json` 中注册你的插件：

```json
[
  {
    "id": "mytool-configurator",
    "name": "My Tool Configurator",
    "repository": "https://github.com/yourname/proxytool-plugins/releases/latest/download/mytool.zip",
    "version": "1.0.0"
  }
]
```

3. 用户可以通过以下命令安装：

```bash
proxy-tool plugin install mytool-configurator
```