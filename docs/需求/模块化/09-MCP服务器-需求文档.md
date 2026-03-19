# 09 MCP 服务器 — 需求文档

> 所属项目：Gate · 模块化文档 v2.0 · 优先级：P0  
> 关联文档：[CLI命令](./06-CLI命令-需求文档.md) | [数据模型](./07-数据模型-需求文档.md)

---

## 1. 功能概述

MCP（Model Context Protocol）服务器使 AI 助手（Cursor、Claude Desktop 等）能通过标准化工具调用协议直接控制 Gate，实现代理自动化管理。

支持两种传输模式：
- **stdio 模式**（默认）：JSON-RPC 2.0 over stdin/stdout，适合本地 AI 工具
- **SSE 模式**：HTTP + Server-Sent Events，适合远程客户端

---

## 2. 启动命令

```bash
gate mcp                                    # stdio 模式
gate mcp --sse --port 3001                  # SSE 模式
gate mcp --sse --port 3001 --host 0.0.0.0  # 允许远程连接
```

---

## 3. MCP 工具定义

### set_proxy
**描述**：设置全局代理  
**输入**：`proxy`（必填）、`noProxy`（可选）、`verify`（可选，bool，默认 false）  
**返回**：`{ success: bool, message: string }`

> **A9 行为**：`verify: true` 时执行连通性测试；SSL 证书失败给出 WARN 但不中止测试，以代理实际可达性为准。  
> **A14 行为**：`verify: true` 且测试失败时，返回 `result: { success: false, message: "代理测试失败: {reason}" }`，**不返回 JSON-RPC error**。

### clear_proxy
**描述**：清除全局代理  
**输入**：`all`（可选，bool，是否同时清除工具代理）  
**返回**：`{ success: bool }`

### get_status
**描述**：获取当前代理状态  
**输入**：无  
**返回**：
```json
{
  "globalProxy": { "httpProxy": "...", "httpsProxy": "...", "noProxy": "..." },
  "configuredTools": [ { "toolName": "git", "proxy": "..." } ],
  "activePreset": "office"
}
```

> **B41**：`configuredTools` 只返回 `ReadProxy() != null` 的工具，并使用内存缓存（工具代理写入/清除时更新），避免每次全量读取 214 个工具配置文件。

### set_tool_proxy
**描述**：设置指定工具代理  
**输入**：`tool`（必填）、`proxy`（设置时必填）、`clear`（bool，与 proxy 互斥）  
**返回**：`{ success: bool, message: string }`

> **B40**：`proxy` 与 `clear: true` 同时传入时，返回 JSON-RPC `-32602` 参数错误，`message` 为 `"proxy and clear are mutually exclusive"`。

### list_tools
**描述**：列出所有支持的工具  
**输入**：`installedOnly`（bool）、`category`（string）  
**返回**：工具列表数组

### list_presets
**描述**：列出所有预设  
**输入**：无  
**返回**：预设列表数组

### save_preset
**描述**：将当前配置保存为预设  
**输入**：`name`（必填）、`description`、`overwrite`（bool）  
**返回**：`{ success: bool }`

### load_preset
**描述**：加载预设  
**输入**：`name`（必填）  
**返回**：`{ success: bool, failedTools: string[] }`

### test_proxy
**描述**：测试代理连通性  
**输入**：`proxy`（空则测当前）、`url`、`timeoutMs`  
**返回**：`{ success: bool, latencyMs: int, statusCode: int, errorMessage: string }`

---

## 4. JSON Schema（工具输入完整定义）

```json
{
  "set_proxy": {
    "type": "object",
    "properties": {
      "proxy":   { "type": "string" },
      "noProxy": { "type": "string" },
      "verify":  { "type": "boolean", "default": false }
    },
    "required": ["proxy"]
  },
  "set_tool_proxy": {
    "type": "object",
    "properties": {
      "tool":  { "type": "string" },
      "proxy": { "type": "string" },
      "clear": { "type": "boolean", "default": false }
    },
    "required": ["tool"]
  },
  "save_preset": {
    "type": "object",
    "properties": {
      "name":        { "type": "string" },
      "description": { "type": "string", "default": "" },
      "overwrite":   { "type": "boolean", "default": false }
    },
    "required": ["name"]
  },
  "load_preset":  { "type": "object", "properties": { "name": { "type": "string" } }, "required": ["name"] },
  "test_proxy": {
    "type": "object",
    "properties": {
      "proxy":     { "type": "string" },
      "url":       { "type": "string" },
      "timeoutMs": { "type": "integer", "minimum": 100, "maximum": 300000, "default": 10000 }
    }
  }
}
```

---

## 5. 技术实现要点

```csharp
// Gate.Mcp/McpServer.cs
public class McpServer {
    private readonly IMcpTransport _transport; // StdioTransport 或 SseTransport

    public async Task RunAsync(CancellationToken ct) {
        await _transport.StartAsync(ct);
        await foreach (var req in _transport.ReadRequestsAsync(ct)) {
            var res = await DispatchAsync(req);
            await _transport.WriteResponseAsync(res, ct);
        }
    }

    private Task<McpResponse> DispatchAsync(McpRequest req) => req.Method switch {
        "tools/list" => HandleToolsList(),
        "tools/call" => HandleToolCall(req),
        _            => Task.FromResult(McpResponse.MethodNotFound(req.Id))
    };
}

public interface IMcpTransport {
    Task StartAsync(CancellationToken ct);
    IAsyncEnumerable<McpRequest> ReadRequestsAsync(CancellationToken ct);
    Task WriteResponseAsync(McpResponse res, CancellationToken ct);
}
```

**JSON-RPC 2.0 规范要求**：
- 请求必须含 `jsonrpc: "2.0"`、`method`、`id`
- 响应含 `result` 或 `error`（含 `code` 和 `message`）
- `tools/list` 返回所有工具 schema 定义
- `tools/call` 参数：`{ name: string, arguments: object }`

---

## 6. SSE 模式端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/sse` | GET | SSE 长连接，推送服务器事件 |
| `/messages` | POST | 客户端发送 JSON-RPC 请求；`Content-Type` 必须为 `application/json`，否则返回 HTTP 400（B43） |
| `/health` | GET | 健康检查，返回 `{"status":"ok"}` |

SSE 事件格式：
```
event: message
data: {"jsonrpc":"2.0","id":1,"result":{...}}
```

---

## 7. 客户端配置示例

**Cursor `mcp.json`（stdio）**：
```json
{
  "mcpServers": {
    "gate": { "command": "gate", "args": ["mcp"] }
  }
}
```

**Claude Desktop（SSE）**：
```json
{
  "mcpServers": {
    "gate": { "url": "http://localhost:3001/sse" }
  }
}
```

---

## 8. 错误码规范

| 场景 | JSON-RPC code | message |
|------|---------------|---------|
| 工具不存在 | -32602 | `Unknown tool: {name}` |
| 必填参数缺失 | -32602 | `Missing required: {field}` |
| 代理格式错误 | -32602 | `Invalid proxy format` |
| 代理测试失败 | -32000 | `Proxy test failed: {reason}` |
| 文件权限不足 | -32000 | `Permission denied: {path}` |
| 内部错误 | -32603 | `Internal error` |

---

## 9. 日志与输出约束

**stdio 模式输出约束**：`gate mcp` 运行期间，stdout 只输出 JSON-RPC 响应，所有日志写入 `{DataDir}/mcp.log`（A15：方案A）。stdio 模式进程由 Cursor / Claude Desktop 等 AI 客户端通过 `mcp.json` 的 `command` 字段启动并管理；客户端退出时自动终止进程（B42）。

```csharp
// Gate.Mcp/McpLogger.cs
public class McpLogger {
    private readonly string _logPath;
    public void Log(string level, string message) {
        var line = $"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}] [{level}] {message}";
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}
```

- 日志文件路径：`{DataDir}/mcp.log`
- 日志格式：`[时间戳] [LEVEL] 消息`
- 日志级别：`INFO`（工具调用记录）、`WARN`、`ERROR`
- 不向 stderr 输出（客户端可能将 stderr 视为协议错误）
- 日志轮转：**每次写日志前检查文件大小**；超过 10MB 则先轮转再写入，当前行不丢失；轮转在同一写操作的锁内完成；保留最近 3 份（B44）

---

## 10. 验收标准

- [ ] `gate mcp` 启动后，Cursor 可通过 MCP 调用 `set_proxy`，进程环境变量更新
- [ ] `get_status` 返回正确的当前状态和工具代理列表
- [ ] `test_proxy` 返回延迟和状态码；代理不可达时 `success: false`
- [ ] `tools/list` 返回全部 9 个工具定义，schema 与本文档一致
- [ ] SSE 模式：`/health` 返回 200，`/messages` POST 返回正确 JSON-RPC 响应
- [ ] stdio 模式：大量快速请求下不丢消息，不死锁
- [ ] 无效 method 返回 `-32601 Method not found`

---

## 10. 周边可选功能

| 功能 | 优先级 | 说明 |
|------|--------|------|
| MCP 认证（API Key） | P1 | SSE 模式 Bearer Token 验证 |
| 工具调用日志 | P1 | 记录每次 MCP 调用到 `{DataDir}/mcp.log` |
| Streamable HTTP 传输 | P2 | MCP 最新规范支持 |
| 多客户端并发（SSE） | P1 | 支持多个 AI 助手同时连接 |
