using System;
using System.Runtime.InteropServices;
using ProxyTool.Models;

namespace ProxyTool.Managers;

/// <summary>
/// 环境变量管理器
/// </summary>
public static class EnvVarManager
{
    /// <summary>
    /// 获取环境变量代理配置
    /// </summary>
    public static ProxyConfig GetProxyConfig(EnvLevel level)
    {
        var prefix = level == EnvLevel.System ? "System_" : "";
        
        return new ProxyConfig
        {
            HttpProxy = Environment.GetEnvironmentVariable($"{prefix}HTTP_PROXY") 
                ?? Environment.GetEnvironmentVariable($"{prefix}http_proxy"),
            HttpsProxy = Environment.GetEnvironmentVariable($"{prefix}HTTPS_PROXY")
                ?? Environment.GetEnvironmentVariable($"{prefix}https_proxy"),
            FtpProxy = Environment.GetEnvironmentVariable($"{prefix}FTP_PROXY")
                ?? Environment.GetEnvironmentVariable($"{prefix}ftp_proxy"),
            SocksProxy = Environment.GetEnvironmentVariable($"{prefix}SOCKS_PROXY")
                ?? Environment.GetEnvironmentVariable($"{prefix}socks_proxy"),
            NoProxy = Environment.GetEnvironmentVariable($"{prefix}NO_PROXY")
                ?? Environment.GetEnvironmentVariable($"{prefix}no_proxy")
        };
    }

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
            // 支持 http://host:port, socks5://host:port, host:port
            var url = proxyUrl;
            if (!url.Contains("://"))
                url = "http://" + url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri == null)
                return null;

            var host = uri.Host;
            if (string.IsNullOrEmpty(host))
                return null;

            // 拒绝明显无效的主机名：需为 IP、localhost 或包含点的域名
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