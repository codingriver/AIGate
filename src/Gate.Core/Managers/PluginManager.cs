using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Gate.Models;

namespace Gate.Managers;

/// <summary>
/// Gate 插件管理器。
/// 插件格式：~/.local/share/gate/plugins/<toolname>/tool.json
/// 插件索引（社区仓库）：https://raw.githubusercontent.com/gate-community/gate-tools/main/index.json
/// </summary>
public static class PluginManager
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public class PluginIndexEntry
    {
        public string Id          { get; set; } = "";
        public string ToolName    { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version     { get; set; } = "";
        public string Author      { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Sha256      { get; set; } = "";
        public List<string> Tags  { get; set; } = new();
    }

    public class PluginIndex
    {
        public int Version      { get; set; }
        public string Updated   { get; set; } = "";
        public List<PluginIndexEntry> Plugins { get; set; } = new();
    }

    // ── 本地已安装插件 ────────────────────────────────────────────────────────

    public static List<string> ListInstalled()
    {
        var dir = GatePaths.PluginsDir;
        if (!Directory.Exists(dir)) return new();
        var result = new List<string>();
        foreach (var d in Directory.GetDirectories(dir))
        {
            var f = Path.Combine(d, "tool.json");
            if (File.Exists(f)) result.Add(Path.GetFileName(d));
        }
        return result;
    }

    public static bool IsInstalled(string toolName) =>
        File.Exists(Path.Combine(GatePaths.PluginsDir, toolName, "tool.json"));

    // ── 安装（从本地 JSON 文件） ───────────────────────────────────────────────

    public static bool InstallFromFile(string jsonPath, out string? error)
    {
        error = null;
        if (!File.Exists(jsonPath)) { error = $"文件不存在: {jsonPath}"; return false; }
        try
        {
            var json = File.ReadAllText(jsonPath);
            var desc = JsonSerializer.Deserialize<Gate.Models.ToolDescriptor>(json, _opts);
            if (desc == null || string.IsNullOrEmpty(desc.ToolName))
            { error = "tool.json 格式无效：缺少 toolName"; return false; }

            // 验证 JSON Schema（基础）
            if (!Validate(desc, out var valErr)) { error = valErr; return false; }

            var destDir = Path.Combine(GatePaths.PluginsDir, desc.ToolName);
            Directory.CreateDirectory(destDir);
            File.Copy(jsonPath, Path.Combine(destDir, "tool.json"), overwrite: true);

            // 立即注册到 ToolRegistry（热加载，无需重启）
            ToolRegistry.Register(new Gate.Configurators.DeclarativeToolConfigurator(desc));
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    // ── 卸载 ─────────────────────────────────────────────────────────────────

    public static bool Remove(string toolName, out string? error)
    {
        error = null;
        var dir = Path.Combine(GatePaths.PluginsDir, toolName);
        if (!Directory.Exists(dir)) { error = $"插件 '{toolName}' 未安装"; return false; }
        try { Directory.Delete(dir, recursive: true); return true; }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    // ── 校验 ─────────────────────────────────────────────────────────────────

    public static bool Validate(Gate.Models.ToolDescriptor desc, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(desc.ToolName))
        { error = "toolName 不能为空"; return false; }
        if (desc.ToolName != desc.ToolName.ToLowerInvariant())
        { error = "toolName 必须为小写字母+连字符"; return false; }
        if (desc.Config == null && desc.EnvProxy == null)
        { error = "必须指定 config 或 envProxy 中至少一个"; return false; }
        if (desc.Config?.ProxyLines != null)
        {
            foreach (var pl in desc.Config.ProxyLines)
                if (string.IsNullOrWhiteSpace(pl.Key))
                { error = "proxyLines 中存在空 key"; return false; }
        }
        return true;
    }

    public static bool ValidateFile(string jsonPath, out string? error)
    {
        error = null;
        if (!File.Exists(jsonPath)) { error = $"文件不存在: {jsonPath}"; return false; }
        try
        {
            var desc = JsonSerializer.Deserialize<Gate.Models.ToolDescriptor>(
                File.ReadAllText(jsonPath), _opts);
            if (desc == null) { error = "JSON 解析失败"; return false; }
            return Validate(desc, out error);
        }
        catch (Exception ex) { error = $"JSON 格式错误: {ex.Message}"; return false; }
    }
}
