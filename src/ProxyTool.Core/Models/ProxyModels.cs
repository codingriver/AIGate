using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxyTool.Models;

/// <summary>
/// 代理配置模型
/// </summary>
public class ProxyConfig
{
    public string? HttpProxy { get; set; }
    public string? HttpsProxy { get; set; }
    public string? FtpProxy { get; set; }
    public string? SocksProxy { get; set; }
    public string? NoProxy { get; set; }

    public bool IsEmpty => string.IsNullOrEmpty(HttpProxy) 
        && string.IsNullOrEmpty(HttpsProxy) 
        && string.IsNullOrEmpty(FtpProxy)
        && string.IsNullOrEmpty(SocksProxy);

    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(HttpProxy)) parts.Add($"HTTP={HttpProxy}");
        if (!string.IsNullOrEmpty(HttpsProxy)) parts.Add($"HTTPS={HttpsProxy}");
        if (!string.IsNullOrEmpty(FtpProxy)) parts.Add($"FTP={FtpProxy}");
        if (!string.IsNullOrEmpty(SocksProxy)) parts.Add($"SOCKS={SocksProxy}");
        if (!string.IsNullOrEmpty(NoProxy)) parts.Add($"NO_PROXY={NoProxy}");
        return string.Join(", ", parts);
    }
}

/// <summary>
/// 环境变量级别
/// </summary>
public enum EnvLevel
{
    User,
    System
}

/// <summary>
/// 工具代理配置状态
/// </summary>
public enum ToolProxyStatus
{
    NotConfigured,
    Inherited,
    Configured
}

/// <summary>
/// 单个工具的代理配置
/// </summary>
public class ToolProxyConfig
{
    public string ToolName { get; set; } = "";
    public string Category { get; set; } = "";
    public ToolProxyStatus Status { get; set; } = ToolProxyStatus.NotConfigured;
    public string? ConfigPath { get; set; }
    public ProxyConfig? CustomProxy { get; set; }
    public string? InheritedFrom { get; set; }
}

/// <summary>
/// 配置集（Profile）
/// </summary>
public class Profile
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ProxyConfig EnvVars { get; set; } = new();
    public Dictionary<string, ProxyConfig> ToolConfigs { get; set; } = new();
}

/// <summary>
/// 代理测试结果
/// </summary>
public class ProxyTestResult
{
    public bool Success { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string TestUrl { get; set; } = "";
}