# ProxyTool 配置文件模板

本目录包含预定义的代理配置文件模板，可用于快速配置各种网络环境。

## 模板列表

### 1. corporate-full.json
企业完整网络代理配置，包含：
- HTTP/HTTPS 代理设置
- NO_PROXY 白名单
- Git、npm、pip、Docker 等工具配置

### 2. v2ray.yaml
V2Ray/VMess 代理配置模板，适用于：
- V2Ray
- V2RayNG (Android)
- V2RayX (macOS)
- 其他 VMess 客户端

### 3. clash.yaml
Clash 代理配置模板，适用于：
- Clash for Windows
- ClashX
- Clash for Android
- Surge

### 4. plugin-example.json
插件开发示例模板，用于创建自定义工具配置器

## 使用方法

### 导入配置
```bash
# 导入 JSON 配置
proxy-tool profile --import corporate-full.json

# 导入 YAML 配置  
proxy-tool profile --import v2ray.yaml --name my-v2ray
```

### 导出配置
```bash
# 导出当前配置
proxy-tool profile --save my-config

# 导出为 YAML
proxy-tool profile --export my-config --format yaml
```

## 自定义模板

创建自定义模板只需编辑 JSON 或 YAML 文件：

```json
{
  "name": "my-template",
  "description": "我的自定义配置",
  "envVars": {
    "http_proxy": "http://localhost:8080",
    "https_proxy": "http://localhost:8080"
  },
  "tools": {
    "git": {
      "http.proxy": "http://localhost:8080"
    }
  }
}
```