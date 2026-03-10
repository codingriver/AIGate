# ProxyTool API 文档

## 1. 概述

ProxyTool API 是基于 REST 的 HTTP 服务，提供代理配置、工具管理、配置集管理的完整接口。

- **Base URL**: `http://localhost:5000`
- **API 版本**: v1
- **路径前缀**: `/api/v1/`
- **数据格式**: JSON
- **CORS**: 允许所有来源

---

## 2. Proxy 代理配置 API

### 2.1 获取代理配置

```
GET /api/v1/proxy/config
```

**Query 参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| level | string | User | 配置级别：`User` 或 `System` |

**响应示例**：
```json
{
  "httpProxy": "http://proxy.example.com:8080",
  "httpsProxy": "http://proxy.example.com:8080",
  "ftpProxy": null,
  "socksProxy": null,
  "noProxy": "localhost,127.0.0.1"
}
```

---

### 2.2 设置代理配置

```
POST /api/v1/proxy/config
```

**请求体** (ProxyConfigRequest)：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| httpProxy | string | 否 | HTTP 代理地址 |
| httpsProxy | string | 否 | HTTPS 代理地址（缺省时使用 httpProxy） |
| ftpProxy | string | 否 | FTP 代理地址 |
| socksProxy | string | 否 | SOCKS 代理地址 |
| noProxy | string | 否 | 排除代理的地址列表 |
| verify | boolean | 否 | 是否在设置前测试代理连通性，默认 false |

**请求示例**：
```json
{
  "httpProxy": "http://proxy.example.com:8080",
  "httpsProxy": "http://proxy.example.com:8080",
  "noProxy": "localhost,.internal",
  "verify": true
}
```

**成功响应** (200)：
```json
{
  "success": true,
  "config": {
    "httpProxy": "http://proxy.example.com:8080",
    "httpsProxy": "http://proxy.example.com:8080",
    "ftpProxy": null,
    "socksProxy": null,
    "noProxy": "localhost,.internal"
  }
}
```

**验证模式响应** (verify=true 时)：
```json
{
  "success": true,
  "testResult": {
    "success": true,
    "responseTimeMs": 125,
    "errorMessage": null,
    "testUrl": "http://httpbin.org/get"
  }
}
```

**错误响应** (400)：
```json
{
  "error": "HTTP 代理: 无效的代理主机名"
}
```

---

### 2.3 清除代理配置

```
DELETE /api/v1/proxy/config
```

**响应** (200)：
```json
{
  "success": true,
  "message": "代理已清除"
}
```

---

### 2.4 测试代理连通性

```
POST /api/v1/proxy/test
```

**请求体** (TestProxyRequest)：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| proxy | string | 否 | 代理地址，不传则使用当前环境变量中的代理 |
| testUrl | string | 否 | 测试目标 URL，默认 httpbin.org |
| timeout | int | 否 | 超时秒数，默认 10 |

**请求示例**：
```json
{
  "proxy": "http://proxy.example.com:8080",
  "testUrl": "https://www.google.com",
  "timeout": 15
}
```

**成功响应** (200)：
```json
{
  "success": true,
  "responseTimeMs": 230,
  "errorMessage": null,
  "testUrl": "https://www.google.com"
}
```

**失败响应** (200，Success=false)：
```json
{
  "success": false,
  "responseTimeMs": 0,
  "errorMessage": "Connection timed out",
  "testUrl": "https://www.google.com"
}
```

**错误响应** (400)：
```json
{
  "error": "未指定代理地址"
}
```

---

## 3. Tools 工具列表 API

### 3.1 获取所有工具

```
GET /api/v1/tools
```

**响应** (200)：
```json
{
  "tools": [
    {
      "toolName": "git",
      "category": "版本控制",
      "isInstalled": true,
      "configPath": "/home/user/.gitconfig",
      "currentConfig": {
        "httpProxy": "http://proxy.example.com:8080",
        "httpsProxy": "http://proxy.example.com:8080",
        "ftpProxy": null,
        "socksProxy": null,
        "noProxy": null
      }
    }
  ]
}
```

---

### 3.2 获取已安装的工具

```
GET /api/v1/tools/installed
```

**响应** (200)：与 `GET /api/v1/tools` 结构相同，仅包含 `isInstalled=true` 的工具。

---

### 3.3 获取工具分类

```
GET /api/v1/tools/categories
```

**响应** (200)：
```json
{
  "categories": ["AI 工具", "包管理器", "版本控制", ...]
}
```

---

### 3.4 按分类获取工具

```
GET /api/v1/tools/category/{category}
```

**路径参数**：`category` - 分类名称

**响应** (200)：与 `GET /api/v1/tools` 结构相同，仅包含指定分类的工具。

---

### 3.5 获取单个工具信息

```
GET /api/v1/tools/{name}
```

**路径参数**：`name` - 工具名称（如 git、npm、docker）

**成功响应** (200)：
```json
{
  "toolName": "git",
  "category": "版本控制",
  "configPath": "/home/user/.gitconfig",
  "isInstalled": true,
  "currentConfig": {
    "httpProxy": "http://proxy.example.com:8080",
    "httpsProxy": "http://proxy.example.com:8080",
    "ftpProxy": null,
    "socksProxy": null,
    "noProxy": null
  }
}
```

**错误响应** (404)：
```json
{
  "error": "未找到工具: xyz"
}
```

---

### 3.6 设置工具代理

```
POST /api/v1/tools/{name}/proxy
```

**路径参数**：`name` - 工具名称

**请求体** (SetToolProxyRequest)：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| proxy | string | 是 | 代理地址 |

**请求示例**：
```json
{
  "proxy": "http://proxy.example.com:8080"
}
```

**成功响应** (200)：
```json
{
  "success": true,
  "tool": "git"
}
```

**错误响应** (404)：
```json
{
  "error": "未找到工具: xyz"
}
```

**错误响应** (400)：
```json
{
  "error": "工具 xyz 未安装"
}
```

---

### 3.7 清除工具代理

```
DELETE /api/v1/tools/{name}/proxy
```

**路径参数**：`name` - 工具名称

**成功响应** (200)：
```json
{
  "success": true,
  "tool": "git"
}
```

---

### 3.8 批量设置代理

```
POST /api/v1/tools/batch/proxy
```

**请求体** (SetToolProxyRequest)：
```json
{
  "proxy": "http://proxy.example.com:8080"
}
```

**成功响应** (200)：
```json
{
  "results": {
    "git": true,
    "npm": true,
    "pip": false
  }
}
```

`results` 为工具名到布尔值的映射，表示各工具是否设置成功。

---

### 3.9 批量清除代理

```
DELETE /api/v1/tools/batch/proxy
```

**成功响应** (200)：
```json
{
  "results": {
    "git": true,
    "npm": true,
    "pip": true
  }
}
```

---

## 4. Profiles 配置集 API

### 4.1 获取所有配置集

```
GET /api/v1/profiles
```

**响应** (200)：
```json
{
  "profiles": ["company", "home", "project-a"]
}
```

---

### 4.2 获取配置集详情

```
GET /api/v1/profiles/{name}
```

**路径参数**：`name` - 配置集名称

**成功响应** (200)：
```json
{
  "name": "company",
  "description": "",
  "createdAt": "2024-01-15T10:00:00",
  "updatedAt": "2024-01-15T10:00:00",
  "envVars": {
    "httpProxy": "http://proxy.company.com:8080",
    "httpsProxy": "http://proxy.company.com:8080",
    "ftpProxy": null,
    "socksProxy": null,
    "noProxy": null
  },
  "toolConfigs": {
    "git": {
      "httpProxy": "http://proxy.company.com:8080",
      "httpsProxy": "http://proxy.company.com:8080",
      "ftpProxy": null,
      "socksProxy": null,
      "noProxy": null
    }
  }
}
```

**错误响应** (404)：
```json
{
  "error": "未找到配置集: xyz"
}
```

---

### 4.3 保存配置集

```
POST /api/v1/profiles
```

**请求体** (Profile)：见 4.2 响应结构

**字段说明**：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| name | string | 是 | 配置集名称 |
| description | string | 否 | 描述 |
| envVars | ProxyConfig | 否 | 环境变量配置 |
| toolConfigs | object | 否 | 各工具配置，key 为工具名 |

**成功响应** (200)：
```json
{
  "success": true,
  "profile": "company"
}
```

---

### 4.4 加载配置集

```
POST /api/v1/profiles/{name}/load
```

**路径参数**：`name` - 配置集名称

**说明**：将配置集的环境变量应用到当前 API 进程。

**成功响应** (200)：
```json
{
  "success": true,
  "profile": { ... }
}
```

---

### 4.5 删除配置集

```
DELETE /api/v1/profiles/{name}
```

**成功响应** (200)：
```json
{
  "success": true
}
```

---

### 4.6 设置默认配置集

```
POST /api/v1/profiles/{name}/default
```

**成功响应** (200)：
```json
{
  "success": true,
  "defaultProfile": "company"
}
```

---

### 4.7 获取默认配置集

```
GET /api/v1/profiles/default
```

**响应** (200)：
```json
{
  "defaultProfile": "company"
}
```

---

### 4.8 导出配置集

```
GET /api/v1/profiles/{name}/export?format=json
```

**Query 参数**：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| format | string | json | 导出格式：`json`、`yaml`、`env` |

**成功响应** (200)：
```json
{
  "path": "/tmp/company.json",
  "content": "{ ... }"
}
```

---

### 4.9 导入配置集

```
POST /api/v1/profiles/import
```

**请求体** (ImportProfileRequest)：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| filePath | string | 是 | 导入文件路径 |
| name | string | 否 | 导入后的配置集名称，不传则使用文件中的 name |
| format | string | 否 | 格式：`json`、`yaml`、`env` |

**成功响应** (200)：
```json
{
  "success": true,
  "profile": { ... }
}
```

---

## 5. 数据模型

### 5.1 ProxyConfig

| 字段 | 类型 | 说明 |
|------|------|------|
| httpProxy | string? | HTTP 代理 |
| httpsProxy | string? | HTTPS 代理 |
| ftpProxy | string? | FTP 代理 |
| socksProxy | string? | SOCKS 代理 |
| noProxy | string? | 排除列表 |

### 5.2 Profile

| 字段 | 类型 | 说明 |
|------|------|------|
| name | string | 名称 |
| description | string | 描述 |
| createdAt | string (ISO 8601) | 创建时间 |
| updatedAt | string (ISO 8601) | 更新时间 |
| envVars | ProxyConfig | 环境变量 |
| toolConfigs | object | 工具配置 |

### 5.3 ProxyTestResult

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 是否成功 |
| responseTimeMs | int | 响应时间(ms) |
| errorMessage | string? | 错误信息 |
| testUrl | string | 测试 URL |

---

## 6. 错误码

| HTTP 状态码 | 说明 |
|-------------|------|
| 200 | 成功 |
| 400 | 请求参数错误、验证失败 |
| 404 | 资源不存在（工具/配置集） |
| 500 | 服务器内部错误 |

---

## 7. Swagger

开发环境下启动 API 服务后，访问：

- **Swagger UI**: `http://localhost:5000/swagger`
- **OpenAPI JSON**: `http://localhost:5000/swagger/v1/swagger.json`
