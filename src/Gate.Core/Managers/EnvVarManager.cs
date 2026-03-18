using System;
using System.Runtime.InteropServices;
using Gate.Models;

namespace Gate.Managers;

/// <summary>
/// 环境变量管理器
/// </summary>
public static class EnvVarManager
{
    /// <summary>
    /// 获取指定层级的代理配置（Process/User/Machine 三层独立读取）
    /// </summary>
    public static ProxyConfig GetProxyConfig(EnvLevel level)
    {
        var target = level switch
        {
            EnvLevel.System  => EnvironmentVariableTarget.Machine,
            EnvLevel.User    => EnvironmentVariableTarget.User,
            EnvLevel.Process => EnvironmentVariableTarget.Process,
            _                => EnvironmentVariableTarget.Process
        };

        string? Get(string key)
        {
            // 进程级直接读（含继承自 User/Machine 的合并值），其余两层用 Target 精确读
            if (target == EnvironmentVariableTarget.Process)
                return Environment.GetEnvironmentVariable(key)
                    ?? Environment.GetEnvironmentVariable(key.ToLowerInvariant());
            return Environment.GetEnvironmentVariable(key, target)
                ?? Environment.GetEnvironmentVariable(key.ToLowerInvariant(), target);
        }

        return new ProxyConfig
        {
            HttpProxy  = Get("HTTP_PROXY"),
            HttpsProxy = Get("HTTPS_PROXY"),
            FtpProxy   = Get("FTP_PROXY"),
            SocksProxy = Get("SOCKS_PROXY"),
            NoProxy    = Get("NO_PROXY")
        };
    }

    /// <summary>
    /// 获取 Windows 系统代理设置（注册表 HKCU\...Internet Settings）
    /// 非 Windows 平台返回 null
    /// </summary>
    public static (bool enabled, string? server, string? bypass)? GetWindowsSystemProxy()
    {
        return null; // 由调用方（Gate.CLI）负责平台特定实现
    }

    /// <summary>
    /// 获取三个层级的代理配置（System > User > Process）
    /// </summary>
    public static (ProxyConfig machine, ProxyConfig user, ProxyConfig process) GetProxyConfigAllLevels()
        => (GetProxyConfig(EnvLevel.System), GetProxyConfig(EnvLevel.User), GetProxyConfig(EnvLevel.Process));

    /// <summary>
    /// 设置环境变量代理（仅当前进程）
    /// </summary>
    public static void SetProxyForCurrentProcess(ProxyConfig config)
    {
        if (!string.IsNullOrEmpty(config.HttpProxy))
            Environment.SetEnvironmentVariable("HTTP_PROXY", config.HttpProxy);
        if (!string.IsNullOrEmpty(config.HttpsProxy))
            Environment.SetEnvironmentVariable("HTTPS_PROXY", config.HttpsProxy);
        if (!string.IsNullOrEmpty(config.FtpProxy))
            Environment.SetEnvironmentVariable("FTP_PROXY", config.FtpProxy);
        if (!string.IsNullOrEmpty(config.SocksProxy))
            Environment.SetEnvironmentVariable("SOCKS_PROXY", config.SocksProxy);
        if (!string.IsNullOrEmpty(config.NoProxy))
            Environment.SetEnvironmentVariable("NO_PROXY", config.NoProxy);
    }

    /// <summary>
    /// 解析代理地址为 (host, port)
    /// </summary>
    public static (string host, int port)? ParseProxyUrl(string? proxyUrl)
    {
        if (string.IsNullOrEmpty(proxyUrl))
            return null;

        try
        {
            var url = proxyUrl;
            if (!url.Contains("://"))
                url = "http://" + url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri == null)
                return null;

            var host = uri.Host;
            if (string.IsNullOrEmpty(host))
                return null;

            if (host != "localhost" &&
                !System.Net.IPAddress.TryParse(host, out _) &&
                !host.Contains("."))
                return null;

            var port = uri.Port > 0 ? uri.Port : (url.StartsWith("socks", StringComparison.OrdinalIgnoreCase) ? 1080 : 8080);
            return (host, port);
        }
        catch
        {
            return null;
        }
    }
}