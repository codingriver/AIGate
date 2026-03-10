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
/// Git 配置器
/// </summary>
public class GitConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "git";
    public override string Category => "版本控制";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME") 
            ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
            return Path.Combine(home, ".gitconfig");
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
    {
        var t = line.Trim();
        return t.StartsWith("http.proxy") || t.StartsWith("proxy =") || t.StartsWith("proxy=");
    }

    protected override bool IsHttpsProxyLine(string line)
    {
        var t = line.Trim();
        return t.StartsWith("https.proxy") || t.StartsWith("proxy =") || t.StartsWith("proxy=");
    }

    protected override string FormatProxyLine(string key, string value)
    {
        // Git 配置格式: http.proxy = http://host:port
        return $"\t{key} = {value}";
    }
}

/// <summary>
/// npm 配置器
/// </summary>
public class NpmConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "npm";
    public override string Category => "包管理器";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetEnvironmentVariable("APPDATA");
        if (home != null)
            return Path.Combine(home, ".npmrc");
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("proxy=");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("https-proxy=");

    protected override bool IsProxyLine(string line)
    {
        var t = line.Trim();
        return t.StartsWith("proxy=") || t.StartsWith("https-proxy=");
    }

    protected override string FormatProxyLine(string key, string value)
    {
        // npm 格式: proxy=http://host:port
        return $"{key}={value}";
    }
}

/// <summary>
/// Docker 配置器（仅 Linux/macOS）
/// </summary>
public class DockerConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "docker";
    public override string Category => "容器工具";

    protected override string? DetectConfigPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetEnvironmentVariable("PROGRAMDATA") ?? "C:\\ProgramData", "Docker", "config", "daemon.json")
            : "/etc/docker/daemon.json";
    }

    public override ProxyConfig? GetCurrentConfig()
    {
        var configPath = ConfigPath;
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return null;

        try
        {
            var json = JsonDocument.Parse(File.ReadAllText(configPath));
            if (json.RootElement.TryGetProperty("proxies", out var proxies))
            {
                var config = new ProxyConfig();
                if (proxies.TryGetProperty("httpProxy", out var http))
                    config.HttpProxy = http.GetString();
                if (proxies.TryGetProperty("httpsProxy", out var https))
                    config.HttpsProxy = https.GetString();
                if (proxies.TryGetProperty("noProxy", out var no))
                    config.NoProxy = no.GetString();
                return config;
            }
        }
        catch { }
        return null;
    }

    public override bool SetProxy(string proxyUrl)
    {
        // Docker 需要 root 权限，提示用户手动配置
        Console.WriteLine("注意：Docker 代理配置需要编辑 /etc/docker/daemon.json 并重启 Docker 服务");
        Console.WriteLine($"参考配置: {{ \"proxies\": {{ \"httpProxy\": \"{proxyUrl}\", \"httpsProxy\": \"{proxyUrl}\" }} }}");
        return false;
    }

    public override bool ClearProxy()
    {
        Console.WriteLine("注意：Docker 代理配置需要编辑 /etc/docker/daemon.json 并重启 Docker 服务");
        return false;
    }
}

/// <summary>
/// Homebrew 配置器 (macOS/Linux)
/// </summary>
public class HomebrewConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "brew";
    public override string Category => "包管理器";

    protected override string? DetectConfigPath()
    {
        var homebrewPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? "/opt/homebrew"
            : "/home/linuxbrew/.linuxbrew";
        
        if (Directory.Exists(homebrewPrefix))
            return Path.Combine(homebrewPrefix, "etc", "brew.conf");
        
        // 尝试环境变量
        var brewPrefix = Environment.GetEnvironmentVariable("HOMEBREW_PREFIX");
        if (!string.IsNullOrEmpty(brewPrefix))
            return Path.Combine(brewPrefix, "etc", "brew.conf");
        
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("export http_proxy=");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("export https_proxy=");

    protected override string FormatProxyLine(string key, string value)
    {
        // Homebrew 格式: export http_proxy="http://host:port"
        return $"export {key}=\"{value}\"";
    }
}

/// <summary>
/// Subversion (svn) 配置器
/// </summary>
public class SvnConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "svn";
    public override string Category => "版本控制";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
            return Path.Combine(home, ".subversion", "servers");
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("http-proxy-host");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("http-proxy-host"); // svn 用同一个

    protected override string FormatProxyLine(string key, string value)
    {
        // Subversion 格式: http-proxy-host = host
        // http-proxy-port = port
        var (host, port) = EnvVarManager.ParseProxyUrl(value) ?? ("", 0);
        return $"http-proxy-host = {host}\nhttp-proxy-port = {port}";
    }
}

/// <summary>
/// Wget 配置器
/// </summary>
public class WgetConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "wget";
    public override string Category => "下载工具";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
            return Path.Combine(home, ".wgetrc");
        return null;
    }

    // 允许在工具未安装时也允许设置代理（用于测试/配置场景）
    public override bool IsInstalled()
    {
        var path = DetectToolPath();
        if (!string.IsNullOrEmpty(path)) return true;
        
        // 如果工具未安装，但配置文件存在，也允许操作
        var configPath = ConfigPath;
        return !string.IsNullOrEmpty(configPath);
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("http_proxy =");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("https_proxy =");

    protected override List<string> FormatProxyLines(string proxyUrl)
    {
        return new List<string>
        {
            FormatProxyLine("http_proxy", proxyUrl),
            FormatProxyLine("https_proxy", proxyUrl)
        };
    }

    protected override string FormatProxyLine(string key, string value)
    {
        // Wget 格式: http_proxy = http://host:port
        return $"{key} = {value}";
    }
}

/// <summary>
/// cURL 配置器
/// </summary>
public class CurlConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "curl";
    public override string Category => "下载工具";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
            return Path.Combine(home, ".curlrc");
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("proxy =");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("proxy ="); // curl 用同一个

    protected override string FormatProxyLine(string key, string value)
    {
        // cURL 格式: proxy = http://host:port
        return $"proxy = {value}";
    }
}

/// <summary>
/// Ruby Gem 配置器
/// </summary>
public class GemConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "gem";
    public override string Category => "包管理器";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME")
            ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
            return Path.Combine(home, ".gemrc");
        return null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("http_proxy:");

    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("https_proxy:");

    protected override string FormatProxyLine(string key, string value)
    {
        // Ruby Gem 格式: http_proxy: http://host:port
        return $"{key}: {value}";
    }
}