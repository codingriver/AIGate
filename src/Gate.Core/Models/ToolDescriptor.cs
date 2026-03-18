using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Gate.Models;

// ═══════════════════════════════════════════════════════════════════════════
// 声明式工具描述符 — 跨平台 JSON Schema
//
// 示例 (npm.json):
// {
//   "toolName": "npm",
//   "displayName": "npm",
//   "category": "包管理器",
//   "executable": "npm",
//   "config": {
//     "platforms": {
//       "windows": { "path": "%APPDATA%\\.npmrc" },
//       "linux":   { "path": "~/.npmrc" },
//       "macos":   { "path": "~/.npmrc" }
//     },
//     "proxyLines": [
//       { "key": "proxy",       "format": "key=value" },
//       { "key": "https-proxy", "format": "key=value" }
//     ]
//   }
// }
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// 工具描述符根对象（对应一个 tool.json 文件）
/// </summary>
public class ToolDescriptor
{
    /// <summary>工具唯一名称（小写，如 git / npm / cursor）</summary>
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = "";

    /// <summary>界面显示名称</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    /// <summary>分类名（版本控制 / 包管理器 / AI IDE …）</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "其他";

    /// <summary>
    /// 可执行文件名（在 PATH 中搜索），可跨平台指定不同名称
    /// 简写："npm"  或 跨平台对象：{ "windows": "npm.cmd", "linux": "npm" }
    /// </summary>
    [JsonPropertyName("executable")]
    public PlatformString? Executable { get; set; }

    /// <summary>配置文件描述（可选，部分工具仅通过环境变量配置）</summary>
    [JsonPropertyName("config")]
    public ConfigDescriptor? Config { get; set; }

    /// <summary>
    /// 环境变量代理配置（工具读取自身的 env var，如 OLLAMA_HOST）
    /// 与 config 二选一或同时使用
    /// </summary>
    [JsonPropertyName("envProxy")]
    public EnvProxyDescriptor? EnvProxy { get; set; }

    /// <summary>备注/说明</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 平台字符串：支持统一值或平台差异值
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 跨平台字符串：可以是一个统一值，也可以为三平台分别指定。
/// 序列化时同时支持 string 和 object 两种形式，由自定义 Converter 处理。
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(PlatformStringConverter))]
public class PlatformString
{
    /// <summary>所有平台统一值（当三平台相同时使用）</summary>
    public string? All { get; set; }

    /// <summary>仅 Windows 的值</summary>
    public string? Windows { get; set; }

    /// <summary>仅 Linux 的值</summary>
    public string? Linux { get; set; }

    /// <summary>仅 macOS 的值</summary>
    public string? MacOS { get; set; }

    public PlatformString() { }
    public PlatformString(string all) { All = all; }

    /// <summary>
    /// 根据当前运行时平台解析出实际字符串，
    /// 展开 ~ / %APPDATA% / $HOME 等路径变量。
    /// </summary>
    public string? Resolve()
    {
        string? raw;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            raw = Windows ?? All;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            raw = MacOS ?? Linux ?? All;
        else
            raw = Linux ?? All;

        return raw == null ? null : ExpandPath(raw);
    }

    private static string ExpandPath(string path)
    {
        // 展开 ~ 为 HOME 目录
        if (path.StartsWith("~/") || path == "~")
        {
            var home = Environment.GetEnvironmentVariable("HOME")
                    ?? Environment.GetEnvironmentVariable("USERPROFILE")
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = home + path.Substring(1);
        }

        // 展开 %VARNAME% 环境变量
        path = Environment.ExpandEnvironmentVariables(path);

        // 展开 $VARNAME（Linux/macOS 风格）
        if (path.Contains("$"))
        {
            path = System.Text.RegularExpressions.Regex.Replace(
                path,
                @"\$([A-Za-z_][A-Za-z0-9_]*)",
                m =>
                {
                    var val = Environment.GetEnvironmentVariable(m.Groups[1].Value);
                    return val ?? m.Value;
                });
        }

        return Path.GetFullPath(path);
    }
}

/// <summary>
/// PlatformString 的 JSON 转换器：
///  - JSON string  → PlatformString { All = value }
///  - JSON object  → PlatformString { Windows/Linux/MacOS = ... }
/// </summary>
public class PlatformStringConverter : System.Text.Json.Serialization.JsonConverter<PlatformString>
{
    public override PlatformString Read(
        ref System.Text.Json.Utf8JsonReader reader,
        Type typeToConvert,
        System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            return new PlatformString(reader.GetString()!);

        if (reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
        {
            var ps = new PlatformString();
            while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndObject)
            {
                var propName = reader.GetString()!.ToLowerInvariant();
                reader.Read();
                var val = reader.GetString();
                switch (propName)
                {
                    case "windows": ps.Windows = val; break;
                    case "linux":   ps.Linux   = val; break;
                    case "macos":
                    case "osx":     ps.MacOS   = val; break;
                    case "all":
                    case "default": ps.All     = val; break;
                }
            }
            return ps;
        }

        throw new System.Text.Json.JsonException("PlatformString must be a string or object");
    }

    public override void Write(
        System.Text.Json.Utf8JsonWriter writer,
        PlatformString value,
        System.Text.Json.JsonSerializerOptions options)
    {
        if (value.Windows == null && value.Linux == null && value.MacOS == null)
        {
            writer.WriteStringValue(value.All);
            return;
        }
        writer.WriteStartObject();
        if (value.Windows != null) writer.WriteString("windows", value.Windows);
        if (value.Linux   != null) writer.WriteString("linux",   value.Linux);
        if (value.MacOS   != null) writer.WriteString("macos",   value.MacOS);
        if (value.All     != null) writer.WriteString("all",     value.All);
        writer.WriteEndObject();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 配置文件描述符
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 描述工具的配置文件位置和代理写入格式
/// </summary>
public class ConfigDescriptor
{
    /// <summary>
    /// 配置文件路径，支持跨平台差异化：
    /// { "windows": "%APPDATA%\\.npmrc", "linux": "~/.npmrc", "macos": "~/.npmrc" }
    /// 或统一路径: "~/.npmrc"
    /// </summary>
    [JsonPropertyName("path")]
    public PlatformString? Path { get; set; }

    /// <summary>
    /// 各平台差异化的路径列表（probe 模式，按顺序查找第一个存在的）。
    /// 当配置文件位置因安装方式不同而不固定时使用。
    /// </summary>
    [JsonPropertyName("probePaths")]
    public List<PlatformString>? ProbePaths { get; set; }

    /// <summary>配置文件格式：ini / keyvalue / json / yaml / toml / xml / custom</summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "keyvalue";

    /// <summary>
    /// 代理配置行的写入规则列表
    /// </summary>
    [JsonPropertyName("proxyLines")]
    public List<ProxyLineDescriptor> ProxyLines { get; set; } = new();

    /// <summary>
    /// JSON 格式时代理字段的 JSON Path（如 "http.proxy"）
    /// </summary>
    [JsonPropertyName("jsonProxyPath")]
    public string? JsonProxyPath { get; set; }

    /// <summary>
    /// JSON 格式时 HTTPS 字段的 JSON Path
    /// </summary>
    [JsonPropertyName("jsonHttpsProxyPath")]
    public string? JsonHttpsProxyPath { get; set; }

    /// <summary>
    /// 是否在文件不存在时自动创建（默认 true）
    /// </summary>
    [JsonPropertyName("createIfMissing")]
    public bool CreateIfMissing { get; set; } = true;

    /// <summary>解析出当前平台的配置文件路径</summary>
    public string? ResolvePath()
    {
        // probePaths 模式：按顺序查找存在的路径
        if (ProbePaths != null && ProbePaths.Count > 0)
        {
            foreach (var ps in ProbePaths)
            {
                var resolved = ps.Resolve();
                if (resolved != null && File.Exists(resolved))
                    return resolved;
            }
            // 没有任何存在，返回第一个（用于写入时自动创建）
            return ProbePaths[0].Resolve();
        }

        return Path?.Resolve();
    }
}

/// <summary>
/// 描述一条代理配置行的写入/识别规则
/// </summary>
public class ProxyLineDescriptor
{
        /// <summary>配置键名，如 proxy / https-proxy / http.proxy 等</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    /// <summary>
    /// 写入格式：
    ///   key=value       →  proxy=http://host:port
    ///   key = value     →  proxy = http://host:port
    ///   key value       →  proxy http://host:port
    ///   key "value"     →  proxy "http://host:port"
    ///   [section]\nkey = value  → INI section 格式
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "key=value";

    /// <summary>
    /// 该行对应的代理类型：http / https / ftp / socks / noproxy / all
    /// 默认 all 表示同时匹配 http 和 https
    /// </summary>
    [JsonPropertyName("proxyType")]
    public string ProxyType { get; set; } = "all";

    /// <summary>
    /// INI section 名称（仅 format=ini 时有效），如 "http_proxy"
    /// </summary>
    [JsonPropertyName("section")]
    public string? Section { get; set; }

    /// <summary>
    /// 值的包装方式：none（默认）/ quotes（双引号）/ singlequotes
    /// </summary>
    [JsonPropertyName("valueWrap")]
    public string ValueWrap { get; set; } = "none";

    /// <summary>
    /// 是否仅在特定平台写入该行（null 表示所有平台）
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>根据当前平台判断该行是否适用</summary>
    public bool IsApplicable()
    {
        if (Platform == null) return true;
        return Platform.ToLowerInvariant() switch
        {
            "windows" => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "linux"   => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "macos" or "osx" => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            _ => true
        };
    }

    /// <summary>格式化成配置文件中的一行字符串</summary>
    public string FormatLine(string proxyUrl)
    {
        var wrappedValue = ValueWrap switch
        {
            "quotes"       => $"\"{proxyUrl}\"",
            "singlequotes" => $"'{proxyUrl}'",
            _              => proxyUrl
        };

        return Format switch
        {
            "key=value"   => $"{Key}={wrappedValue}",
            "key = value" => $"{Key} = {wrappedValue}",
            "key value"   => $"{Key} {wrappedValue}",
            _             => $"{Key}={wrappedValue}"
        };
    }

    /// <summary>判断给定行是否是该键的代理配置行</summary>
    public bool IsMatch(string line)
    {
        var t = line.Trim();
        if (t.StartsWith("#") || t.StartsWith(";")) return false;
        // 匹配 key= / key = / key 开头（大小写不敏感）
        var keyLower = Key.ToLowerInvariant();
        var lineLower = t.ToLowerInvariant();
        return lineLower.StartsWith(keyLower + "=")
            || lineLower.StartsWith(keyLower + " =")
            || lineLower.StartsWith(keyLower + " ");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 环境变量代理描述符
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 描述通过工具专属环境变量配置代理的方式
/// 适用于 ollama / openai-sdk / anthropic 等读取自身 env var 的工具
/// </summary>
public class EnvProxyDescriptor
{
    /// <summary>HTTP 代理环境变量名列表（按优先级排序）</summary>
    [JsonPropertyName("httpVars")]
    public List<string> HttpVars { get; set; } = new();

    /// <summary>HTTPS 代理环境变量名列表</summary>
    [JsonPropertyName("httpsVars")]
    public List<string> HttpsVars { get; set; } = new();

    /// <summary>NO_PROXY 环境变量名列表</summary>
    [JsonPropertyName("noProxyVars")]
    public List<string> NoProxyVars { get; set; } = new();

    /// <summary>变量作用域：process（默认）/ user / system</summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "process";
}
 