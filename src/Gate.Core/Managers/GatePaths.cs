using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gate.Managers;

/// <summary>
/// Gate 所有存储路径的统一入口。
/// Windows : %APPDATA%\gate\
/// Linux   : $XDG_DATA_HOME/gate/  (默认 ~/.local/share/gate/)
/// macOS   : ~/Library/Application Support/gate/
/// </summary>
public static class GatePaths
{
    // ── Root ────────────────────────────────────────────────────────────────

    public static string DataDir { get; } = ResolveDataDir();

    private static string ResolveDataDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appdata = Environment.GetEnvironmentVariable("APPDATA")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appdata, "gate");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? "~";
            return Path.Combine(home, "Library", "Application Support", "gate");
        }

        // Linux / other Unix — respect XDG_DATA_HOME
        var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrEmpty(xdgData))
            return Path.Combine(xdgData, "gate");

        var linuxHome = Environment.GetEnvironmentVariable("HOME") ?? "~";
        return Path.Combine(linuxHome, ".local", "share", "gate");
    }

    // ── Sub-directories ──────────────────────────────────────────────────────

    /// <summary>预设目录（profiles/*.json）</summary>
    public static string ProfilesDir => Path.Combine(DataDir, "profiles");

    /// <summary>用户安装的插件目录（plugins/{name}/tool.json）</summary>
    public static string PluginsDir => Path.Combine(DataDir, "plugins");

    /// <summary>工具自定义路径（tool_paths.json）</summary>
    public static string ToolPathsFile => Path.Combine(DataDir, "tool_paths.json");

    /// <summary>全局配置文件（config.json，含默认预设等）</summary>
    public static string ConfigFile => Path.Combine(DataDir, "config.json");

    /// <summary>代理历史记录（history.json）</summary>
    public static string HistoryFile => Path.Combine(DataDir, "history.json");

    /// <summary>审计日志目录（audit/）</summary>
    public static string AuditDir => Path.Combine(DataDir, "audit");

    /// <summary>日志目录（logs/）</summary>
    public static string LogsDir => Path.Combine(DataDir, "logs");

    // ── Helper ───────────────────────────────────────────────────────────────

    /// <summary>确保目录存在（不存在则创建）</summary>
    public static void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>确保所有核心目录存在</summary>
    public static void EnsureAllDirs()
    {
        EnsureDir(DataDir);
        EnsureDir(ProfilesDir);
        EnsureDir(PluginsDir);
        EnsureDir(AuditDir);
        EnsureDir(LogsDir);
    }
}
