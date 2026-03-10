using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gate.Models;

namespace Gate.Configurators;

/// <summary>
/// 工具配置器基类 - 模板方法模式
/// 提供通用逻辑，子类只需重写特定方法
/// </summary>
public abstract class ToolConfiguratorBase
{
    #region 属性（子类可重写）

    /// <summary>
    /// 工具名称
    /// </summary>
    public abstract string ToolName { get; }

    /// <summary>
    /// 工具分类
    /// </summary>
    public virtual string Category => "其他";

    /// <summary>
    /// 配置文件路径（可自动检测）
    /// </summary>
    public virtual string? ConfigPath => DetectConfigPath();

    /// <summary>
    /// 工具路径缓存
    /// </summary>
    private string? _cachedToolPath;

    /// <summary>
    /// 工具是否已安装（带缓存）
    /// </summary>
    private bool? _isInstalled;

    #endregion

    #region 公共方法

    /// <summary>
    /// 检测工具是否已安装
    /// </summary>
    public virtual bool IsInstalled()
    {
        _isInstalled ??= !string.IsNullOrEmpty(DetectToolPath());
        return _isInstalled.Value;
    }

    /// <summary>
    /// 异步检测工具是否已安装
    /// </summary>
    public virtual Task<bool> IsInstalledAsync() => Task.FromResult(IsInstalled());

    /// <summary>
    /// 获取当前代理配置
    /// </summary>
    public virtual ProxyConfig? GetCurrentConfig()
    {
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return null;

        try
        {
            var content = File.ReadAllText(configPath);
            return ParseConfig(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 异步获取当前代理配置
    /// </summary>
    public virtual Task<ProxyConfig?> GetCurrentConfigAsync()
    {
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return Task.FromResult<ProxyConfig?>(null);

        try
        {
            var content = File.ReadAllText(configPath);
            var config = Task.Run(() => ParseConfig(content));
            return config;
        }
        catch
        {
            return Task.FromResult<ProxyConfig?>(null);
        }
    }

    /// <summary>
    /// 设置代理
    /// </summary>
    public virtual bool SetProxy(string proxyUrl)
    {
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath))
            return false;

        try
        {
            EnsureDirectoryExists(configPath);
            
            var lines = File.Exists(configPath)
                ? new List<string>(File.ReadAllLines(configPath))
                : new List<string>();

            // 模板方法：子类可重写清理逻辑
            lines = ClearProxyLines(lines);
            
            // 添加新配置
            var newLines = FormatProxyLines(proxyUrl);
            lines.AddRange(newLines);

            File.WriteAllLines(configPath, lines);
            OnProxySet?.Invoke(this, proxyUrl);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return false;
        }
    }

    /// <summary>
    /// 异步设置代理
    /// </summary>
    public virtual Task<bool> SetProxyAsync(string proxyUrl)
    {
        var result = SetProxy(proxyUrl);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 清除代理
    /// </summary>
    public virtual bool ClearProxy()
    {
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return true;

        try
        {
            var lines = File.ReadAllLines(configPath)
                .Where(line => !IsProxyLine(line))
                .ToArray();

            File.WriteAllLines(configPath, lines);
            OnProxyCleared?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return false;
        }
    }

    /// <summary>
    /// 异步清除代理
    /// </summary>
    public virtual Task<bool> ClearProxyAsync()
    {
        var result = ClearProxy();
        return Task.FromResult(result);
    }

    #endregion

    #region 模板方法（子类可重写）

    /// <summary>
    /// 检测配置文件路径 - 子类可重写
    /// </summary>
    protected virtual string? DetectConfigPath() => null;

    /// <summary>
    /// 检测工具路径 - 子类可重写
    /// </summary>
    protected virtual string? DetectToolPath()
    {
        if (_cachedToolPath != null)
            return _cachedToolPath;

        _cachedToolPath = FindToolInPath(ToolName);
        return _cachedToolPath;
    }

    /// <summary>
    /// 解析配置内容 - 子类可重写
    /// </summary>
    protected virtual ProxyConfig ParseConfig(string content)
    {
        var config = new ProxyConfig();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // 检测 HTTP 代理
            if (IsHttpProxyLine(trimmed))
            {
                config.HttpProxy = ExtractProxyValue(trimmed);
            }
            // 检测 HTTPS 代理
            else if (IsHttpsProxyLine(trimmed))
            {
                config.HttpsProxy = ExtractProxyValue(trimmed);
            }
            // 检测 NoProxy
            else if (IsNoProxyLine(trimmed))
            {
                config.NoProxy = ExtractProxyValue(trimmed);
            }
        }

        return config;
    }

    /// <summary>
    /// 清除代理配置行 - 子类可重写
    /// </summary>
    protected virtual List<string> ClearProxyLines(List<string> lines)
    {
        return lines.Where(line => !IsProxyLine(line)).ToList();
    }

    /// <summary>
    /// 格式化代理配置行 - 子类可重写
    /// </summary>
    protected virtual List<string> FormatProxyLines(string proxyUrl)
    {
        return new List<string>
        {
            FormatProxyLine("proxy", proxyUrl),
            FormatProxyLine("https-proxy", proxyUrl)
        };
    }

    /// <summary>
    /// 检测是否是代理配置行 - 子类可重写
    /// </summary>
    protected virtual bool IsProxyLine(string line)
    {
        return IsHttpProxyLine(line) || IsHttpsProxyLine(line) || IsNoProxyLine(line);
    }

    /// <summary>
    /// 检测是否是 HTTP 代理行 - 子类可重写
    /// </summary>
    protected virtual bool IsHttpProxyLine(string line)
    {
        var lower = line.ToLowerInvariant().Trim();
        return lower.StartsWith("proxy=") || 
               lower.StartsWith("http.proxy") ||
               lower.StartsWith("proxy =") ||
               lower.StartsWith("http.proxy =");
    }

    /// <summary>
    /// 检测是否是 HTTPS 代理行 - 子类可重写
    /// </summary>
    protected virtual bool IsHttpsProxyLine(string line)
    {
        var lower = line.ToLowerInvariant().Trim();
        return lower.StartsWith("https-proxy=") ||
               lower.StartsWith("https.proxy") ||
               lower.StartsWith("https-proxy =") ||
               lower.StartsWith("https.proxy =");
    }

    /// <summary>
    /// 检测是否是 NoProxy 行 - 子类可重写
    /// </summary>
    protected virtual bool IsNoProxyLine(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.StartsWith("noproxy=") ||
               lower.StartsWith("no_proxy") ||
               lower.StartsWith("no_proxy =");
    }

    /// <summary>
    /// 提取代理值 - 子类可重写
    /// </summary>
    protected virtual string ExtractProxyValue(string line)
    {
        // 支持多种格式: proxy=http://host:port, proxy = http://host:port, "http://host:port"
        var idx = line.IndexOf('=');
        if (idx == -1) idx = line.IndexOf(':');
        if (idx >= 0 && idx < line.Length - 1)
        {
            return line.Substring(idx + 1).Trim().Trim('"').Trim('\'');
        }
        return string.Empty;
    }

    /// <summary>
    /// 格式化代理配置行 - 子类可重写
    /// </summary>
    protected virtual string FormatProxyLine(string key, string value)
    {
        return $"{key}=\"{value}\"";
    }

    #endregion

    #region 事件

    /// <summary>
    /// 代理设置后触发
    /// </summary>
    public event EventHandler<string>? OnProxySet;

    /// <summary>
    /// 代理清除后触发
    /// </summary>
    public event EventHandler? OnProxyCleared;

    /// <summary>
    /// 错误时触发
    /// </summary>
    public event EventHandler<Exception>? OnError;

    #endregion

    #region 辅助方法

    /// <summary>
    /// 在 PATH 环境变量中查找工具
    /// </summary>
    protected string? FindToolInPath(string toolName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? $"{toolName}.exe" 
            : toolName;

        foreach (var dir in pathEnv.Split(separator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            
            var exePath = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(exePath))
                return exePath;
        }

        return null;
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    protected void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// 清除缓存（用于测试）
    /// </summary>
    public void ClearCache()
    {
        _cachedToolPath = null;
        _isInstalled = null;
    }

    #endregion
}