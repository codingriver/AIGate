using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Gate.Models;

namespace Gate.Managers;

/// <summary>
/// 代理测试器
/// </summary>
public static class ProxyTester
{
    /// <summary>
    /// 测试代理连通性（HTTP/HTTPS）
    /// </summary>
    public static async Task<ProxyTestResult> TestHttpProxyAsync(string proxyUrl, string? testUrl = null, int timeoutSec = 10)
    {
        testUrl ??= "http://www.google.com";
        
        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxyUrl),
                UseProxy = true
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSec)
            };

            // 不验证 SSL 证书
            if (proxyUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(testUrl);
            sw.Stop();

            return new ProxyTestResult
            {
                Success = response.IsSuccessStatusCode,
                ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                TestUrl = testUrl
            };
        }
        catch (Exception ex)
        {
            return new ProxyTestResult
            {
                Success = false,
                ResponseTimeMs = 0,
                ErrorMessage = ex.Message,
                TestUrl = testUrl
            };
        }
    }

    /// <summary>
    /// 测试 SOCKS5 代理（简化版，仅检查端口连通性）
    /// </summary>
    public static async Task<ProxyTestResult> TestSocksProxyAsync(string proxyUrl, string? testUrl = null, int timeoutSec = 10)
    {
        testUrl ??= "http://www.google.com";
        
        try
        {
            var (host, port) = EnvVarManager.ParseProxyUrl(proxyUrl) ?? ("", 0);
            if (string.IsNullOrEmpty(host) || port == 0)
            {
                return new ProxyTestResult
                {
                    Success = false,
                    ErrorMessage = "无效的 SOCKS 代理地址",
                    TestUrl = testUrl
                };
            }

            var sw = Stopwatch.StartNew();
            
            // 简化：只测试 TCP 连通性
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
            sw.Stop();
            
            // 如果能连接，假设 SOCKS 代理可用
            return new ProxyTestResult
            {
                Success = true,
                ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                TestUrl = testUrl
            };
        }
        catch (Exception ex)
        {
            return new ProxyTestResult
            {
                Success = false,
                ResponseTimeMs = 0,
                ErrorMessage = ex.Message,
                TestUrl = testUrl
            };
        }
    }

    /// <summary>
    /// 自动检测并测试代理类型
    /// </summary>
    public static async Task<ProxyTestResult> TestProxyAsync(string? proxyUrl, string? testUrl = null, int timeoutSec = 10)
    {
        if (string.IsNullOrEmpty(proxyUrl))
        {
            return new ProxyTestResult
            {
                Success = false,
                ErrorMessage = "未配置代理"
            };
        }

        if (proxyUrl.StartsWith("socks", StringComparison.OrdinalIgnoreCase))
        {
            return await TestSocksProxyAsync(proxyUrl, testUrl, timeoutSec);
        }
        
        return await TestHttpProxyAsync(proxyUrl, testUrl, timeoutSec);
    }
}