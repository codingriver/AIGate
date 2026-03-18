using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Gate.Models;
using Gate.Managers;

namespace Gate.Configurators;

/// <summary>
/// 声明式工具配置器 — 由 ToolDescriptor (JSON) 驱动，无需编写专用 C# 类。
/// 约 80% 的工具（写 key=value / key = value 格式配置文件）可用此配置器覆盖。
/// </summary>
public class DeclarativeToolConfigurator : ToolConfiguratorBase
{
    private readonly ToolDescriptor _desc;

    public DeclarativeToolConfigurator(ToolDescriptor descriptor)
    {
        _desc = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    // ── Identity ──────────────────────────────────────────────────────────────

    public override string ToolName => _desc.ToolName;
    public override string Category => _desc.Category;

    // ── Path detection ────────────────────────────────────────────────────────

    protected override string? DetectConfigPath()
        => _desc.Config?.ResolvePath();

    protected override string? DetectToolPath()
    {
        // 先检查用户自定义路径
        var custom = Gate.Managers.ToolRegistry.GetCustomPath(ToolName);
        if (!string.IsNullOrEmpty(custom?.Exec) && File.Exists(custom.Exec))
            return custom.Exec;

        // 从描述符解析当前平台的可执行文件名
        var exeName = _desc.Executable?.Resolve() ?? ToolName;

        // Windows 自动附加 .exe / .cmd
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && !exeName.Contains('.'))
        {
            var withExe = FindToolInPath(exeName + ".exe");
            var withCmd = FindToolInPath(exeName + ".cmd");
            return withExe ?? withCmd ?? FindToolInPath(exeName);
        }

        return FindToolInPath(exeName);
    }

    // ── Config read ───────────────────────────────────────────────────────────

    public override ProxyConfig? GetCurrentConfig()
    {
        // 1. 环境变量型工具
        if (_desc.EnvProxy != null)
            return ReadEnvProxyConfig();

        // 2. 配置文件型工具
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return null;

        try
        {
            var content = File.ReadAllText(configPath);
            return _desc.Config?.Format?.ToLowerInvariant() switch
            {
                "json" => ParseJsonConfig(content),
                _      => ParseKeyValueConfig(content)
            };
        }
        catch
        {
            return null;
        }
    }

    // ── Proxy set ─────────────────────────────────────────────────────────────

    public override bool SetProxy(string proxyUrl)
    {
        // 环境变量型
        if (_desc.EnvProxy != null)
            return SetEnvProxy(proxyUrl);

        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath))
            return false;

        try
        {
            EnsureDirectoryExists(configPath);

            return _desc.Config?.Format?.ToLowerInvariant() switch
            {
                "json" => SetJsonProxy(configPath, proxyUrl),
                _      => SetKeyValueProxy(configPath, proxyUrl)
            };
        }
        catch (Exception ex)
        {
            RaiseError(ex);
            return false;
        }
    }

    // ── Proxy clear ───────────────────────────────────────────────────────────

    public override bool ClearProxy()
    {
        if (_desc.EnvProxy != null)
            return ClearEnvProxy();

        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return true;

        try
        {
            return _desc.Config?.Format?.ToLowerInvariant() switch
            {
                "json" => ClearJsonProxy(configPath),
                _      => ClearKeyValueProxy(configPath)
            };
        }
        catch (Exception ex)
        {
            RaiseError(ex);
            return false;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Key-Value 格式实现（ini / keyvalue / npmrc / gitconfig …）
    // ═════════════════════════════════════════════════════════════════════════

    private ProxyConfig ParseKeyValueConfig(string content)
    {
        var cfg = new ProxyConfig();
        if (_desc.Config == null) return cfg;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            foreach (var pld in _desc.Config.ProxyLines)
            {
                if (!pld.IsApplicable()) continue;
                if (pld.IsMatch(line))
                {
                    var val = ExtractValue(line);
                    switch (pld.ProxyType.ToLowerInvariant())
                    {
                        case "https":   cfg.HttpsProxy = val; break;
                        case "ftp":     cfg.FtpProxy   = val; break;
                        case "socks":   cfg.SocksProxy = val; break;
                        case "noproxy": cfg.NoProxy    = val; break;
                        default:        cfg.HttpProxy  = val; break; // http / all
                    }
                }
            }
        }
        return cfg;
    }

    private bool SetKeyValueProxy(string configPath, string proxyUrl)
    {
        if (_desc.Config == null) return false;

        var lines = File.Exists(configPath)
            ? new List<string>(File.ReadAllLines(configPath))
            : new List<string>();

        // 删除旧代理行
        lines = lines
            .Where(l => !IsAnyProxyLine(l))
            .ToList();

        // 追加新代理行（仅适用当前平台的行）
        foreach (var pld in _desc.Config.ProxyLines)
        {
            if (!pld.IsApplicable()) continue;
            var value = pld.ProxyType.ToLowerInvariant() == "https"
                ? proxyUrl  // https 也使用同一代理
                : proxyUrl;
            lines.Add(pld.FormatLine(value));
        }

        File.WriteAllLines(configPath, lines);
        RaiseProxySet(proxyUrl);
        return true;
    }

    private bool ClearKeyValueProxy(string configPath)
    {
        var lines = File.ReadAllLines(configPath)
            .Where(l => !IsAnyProxyLine(l))
            .ToArray();
        File.WriteAllLines(configPath, lines);
        RaiseProxyCleared();
        return true;
    }

    private bool IsAnyProxyLine(string line)
    {
        if (_desc.Config == null) return false;
        return _desc.Config.ProxyLines.Any(pld => pld.IsApplicable() && pld.IsMatch(line));
    }

    private static string ExtractValue(string line)
    {
        var idx = line.IndexOf('=');
        if (idx < 0) idx = line.IndexOf(' ');
        if (idx >= 0 && idx < line.Length - 1)
            return line.Substring(idx + 1).Trim().Trim('"').Trim('\'');
        return string.Empty;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // JSON 格式实现（VSCode settings.json / cursor settings.json …）
    // ═════════════════════════════════════════════════════════════════════════

    private ProxyConfig? ParseJsonConfig(string content)
    {
        if (_desc.Config?.JsonProxyPath == null) return null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            var httpVal  = GetJsonValue(doc.RootElement, _desc.Config.JsonProxyPath);
            var httpsVal = _desc.Config.JsonHttpsProxyPath != null
                ? GetJsonValue(doc.RootElement, _desc.Config.JsonHttpsProxyPath)
                : null;
            return new ProxyConfig { HttpProxy = httpVal, HttpsProxy = httpsVal ?? httpVal };
        }
        catch { return null; }
    }

    private bool SetJsonProxy(string configPath, string proxyUrl)
    {
        if (_desc.Config?.JsonProxyPath == null) return false;

        // 读取现有 JSON（不存在则空对象）
        var dict = new Dictionary<string, object?>();
        if (File.Exists(configPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                dict = FlattenJsonObject(doc.RootElement);
            }
            catch { /* 文件损坏则覆盖 */ }
        }

        // 设置代理字段
        dict[_desc.Config.JsonProxyPath] = proxyUrl;
        if (_desc.Config.JsonHttpsProxyPath != null)
            dict[_desc.Config.JsonHttpsProxyPath] = proxyUrl;

        var json = JsonSerializer.Serialize(dict,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
        RaiseProxySet(proxyUrl);
        return true;
    }

    private bool ClearJsonProxy(string configPath)
    {
        if (_desc.Config?.JsonProxyPath == null || !File.Exists(configPath)) return true;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var dict = FlattenJsonObject(doc.RootElement);
            dict.Remove(_desc.Config.JsonProxyPath);
            if (_desc.Config.JsonHttpsProxyPath != null)
                dict.Remove(_desc.Config.JsonHttpsProxyPath);
            File.WriteAllText(configPath,
                JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            RaiseProxyCleared();
            return true;
        }
        catch { return false; }
    }

    // JSON helper: dot-path reader ("http.proxy" => root["http"]["proxy"])
    private static string? GetJsonValue(JsonElement root, string dotPath)
    {
        var parts = dotPath.Split('.');
        var el = root;
        foreach (var part in parts)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            if (!el.TryGetProperty(part, out el)) return null;
        }
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    // Flatten JSON object to string dict (shallow, sufficient for settings files)
    private static Dictionary<string, object?> FlattenJsonObject(JsonElement root)
    {
        var dict = new Dictionary<string, object?>();
        if (root.ValueKind != JsonValueKind.Object) return dict;
        foreach (var prop in root.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String  => (object?)prop.Value.GetString(),
                JsonValueKind.Number  => prop.Value.GetDouble(),
                JsonValueKind.True    => true,
                JsonValueKind.False   => false,
                JsonValueKind.Null    => null,
                _                     => prop.Value.GetRawText()
            };
        }
        return dict;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // 环境变量型工具实现
    // ═════════════════════════════════════════════════════════════════════════

    private ProxyConfig ReadEnvProxyConfig()
    {
        var cfg = new ProxyConfig();
        if (_desc.EnvProxy == null) return cfg;

        foreach (var v in _desc.EnvProxy.HttpVars)
        {
            var val = Environment.GetEnvironmentVariable(v);
            if (!string.IsNullOrEmpty(val)) { cfg.HttpProxy = val; break; }
        }
        foreach (var v in _desc.EnvProxy.HttpsVars)
        {
            var val = Environment.GetEnvironmentVariable(v);
            if (!string.IsNullOrEmpty(val)) { cfg.HttpsProxy = val; break; }
        }
        foreach (var v in _desc.EnvProxy.NoProxyVars)
        {
            var val = Environment.GetEnvironmentVariable(v);
            if (!string.IsNullOrEmpty(val)) { cfg.NoProxy = val; break; }
        }
        return cfg;
    }

    private bool SetEnvProxy(string proxyUrl)
    {
        if (_desc.EnvProxy == null) return false;
        var target = _desc.EnvProxy.Scope.ToLowerInvariant() switch
        {
            "user"   => EnvironmentVariableTarget.User,
            "system" => EnvironmentVariableTarget.Machine,
            _        => EnvironmentVariableTarget.Process
        };
        foreach (var v in _desc.EnvProxy.HttpVars)
            Environment.SetEnvironmentVariable(v, proxyUrl, target);
        foreach (var v in _desc.EnvProxy.HttpsVars)
            Environment.SetEnvironmentVariable(v, proxyUrl, target);
        RaiseProxySet(proxyUrl);
        return true;
    }

    private bool ClearEnvProxy()
    {
        if (_desc.EnvProxy == null) return false;
        var target = _desc.EnvProxy.Scope.ToLowerInvariant() switch
        {
            "user"   => EnvironmentVariableTarget.User,
            "system" => EnvironmentVariableTarget.Machine,
            _        => EnvironmentVariableTarget.Process
        };
        foreach (var v in _desc.EnvProxy.HttpVars)
            Environment.SetEnvironmentVariable(v, null, target);
        foreach (var v in _desc.EnvProxy.HttpsVars)
            Environment.SetEnvironmentVariable(v, null, target);
        foreach (var v in _desc.EnvProxy.NoProxyVars)
            Environment.SetEnvironmentVariable(v, null, target);
        RaiseProxyCleared();
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // 静态工厂：从 JSON 文件加载
    // ═════════════════════════════════════════════════════════════════════════

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>从 JSON 文件路径加载并构建配置器</summary>
    public static DeclarativeToolConfigurator? LoadFromFile(string jsonPath)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            var desc = JsonSerializer.Deserialize<ToolDescriptor>(json, _jsonOpts);
            if (desc == null || string.IsNullOrEmpty(desc.ToolName)) return null;
            return new DeclarativeToolConfigurator(desc);
        }
        catch { return null; }
    }

    /// <summary>从 JSON 字符串加载并构建配置器</summary>
    public static DeclarativeToolConfigurator? LoadFromJson(string json)
    {
        try
        {
            var desc = JsonSerializer.Deserialize<ToolDescriptor>(json, _jsonOpts);
            if (desc == null || string.IsNullOrEmpty(desc.ToolName)) return null;
            return new DeclarativeToolConfigurator(desc);
        }
        catch { return null; }
    }
}
