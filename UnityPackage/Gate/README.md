# Gate Unity 集成指南

## DLL 位置

编译好的 DLL 文件位于：
```
AIGate\src\Gate.Core\bin\Release\netstandard2.0\Gate.Core.dll
```

## Unity 引用步骤

### 1. 复制 DLL 到 Unity 项目

将 `Gate.Core.dll` 复制到 Unity 项目的 `Assets/Plugins/` 目录下：

```
YourUnityProject/
└── Assets/
    └── Plugins/
        └── Gate/
            └── Gate.Core.dll
```

### 2. Unity 项目设置

1. 打开 Unity 项目
2. 选择 `Edit > Project Settings > Player`
3. 在 **Other Settings** 中确认：
   - **Scripting Backend**: Mono ✅ (必须)
   - **Api Compatibility Level**: .NET Standard 2.0 或 .NET Framework

### 3. 在代码中使用

```csharp
using Gate.Mcp;
using Gate.Models;
using Gate.Managers;
using Gate.Platforms;
using Gate.Security;
using Gate.Configurators;
using Gate.Backup;
using Gate.HotReload;
using Gate.Exceptions;

// 示例：创建代理配置
var config = new ProxyConfig
{
    HttpProxy = "http://proxy.example.com:8080",
    HttpsProxy = "http://proxy.example.com:8080",
    NoProxy = "localhost,127.0.0.1"
};

// 示例：设置环境变量代理
var envProvider = new WindowsEnvProvider(); // 或 LinuxEnvProvider
envProvider.SetHttpProxy(config.HttpProxy);

// 示例：配置验证
var validator = new ConfigValidator();
var result = validator.Validate(config);
if (!result.IsValid)
{
    Debug.LogError($"配置无效: {string.Join(", ", result.Errors)}");
}
```

## 注意事项

### ⚠️ 重要限制

1. **必须使用 Mono 脚本后端** - IL2CPP 会有代码剥离问题
2. **平台代码** - `Platforms/WindowsEnvProvider.cs` 和 `Platforms/LinuxEnvProvider.cs` 需要根据目标平台选择使用
3. **UI 相关代码** - `UI/` 目录包含控制台代码，在 Unity 中不适用

### 推荐的模块

在 Unity 中使用时，建议只引用以下模块：

| 模块 | 用途 |
|------|------|
| `Gate.Models` | 数据模型 (ProxyConfig 等) |
| `Gate.Managers` | 核心管理逻辑 |
| `Gate.Exceptions` | 自定义异常 |
| `Gate.Security` | 加密工具 |
| `Gate.Configurators` | 工具配置器 |
| `Gate.HealthCheck` | 代理健康检查 |

## 重新编译

如果修改了 Core 代码，需要重新编译：

```powershell
cd AIGate\src\Gate.Core
dotnet build -c Release
```

然后将新的 DLL 复制到 Unity 项目覆盖即可。
