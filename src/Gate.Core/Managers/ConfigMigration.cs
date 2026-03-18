using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Gate.Models;

namespace Gate.Managers;

/// <summary>
/// 完整配置迁移：export-all / import-all
/// 导出内容：全局代理环境变量 + 所有预设 + 工具自定义路径
/// </summary>
public static class ConfigMigration
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public class MigrationBundle
    {
        public string ExportedAt  { get; set; } = DateTime.Now.ToString("O");
        public string GateVersion { get; set; } = "1.0";
        public ProxyConfig? GlobalProxy { get; set; }
        public List<Profile> Profiles { get; set; } = new();
        public Dictionary<string, CustomPathEntry> ToolPaths { get; set; } = new();
    }

    /// <summary>导出全部配置到 JSON 文件</summary>
    public static void ExportAll(string outputPath)
    {
        var bundle = new MigrationBundle
        {
            GlobalProxy = EnvVarManager.GetProxyConfig(EnvLevel.User),
            ToolPaths   = new Dictionary<string, CustomPathEntry>(
                              ToolRegistry.GetCustomPaths())
        };

        foreach (var name in ProfileManager.List())
        {
            var p = ProfileManager.Load(name);
            if (p != null) bundle.Profiles.Add(p);
        }

        File.WriteAllText(outputPath,
            JsonSerializer.Serialize(bundle, _opts));
    }

    /// <summary>从 JSON 文件导入全部配置</summary>
    public static (int profiles, int paths, bool globalProxy) ImportAll(
        string inputPath, bool overwrite = true)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"文件不存在: {inputPath}");

        var bundle = JsonSerializer.Deserialize<MigrationBundle>(
            File.ReadAllText(inputPath), _opts)
            ?? throw new InvalidOperationException("文件格式无效");

        // 全局代理
        var hasGlobal = bundle.GlobalProxy != null && !bundle.GlobalProxy.IsEmpty;
        if (hasGlobal)
            EnvVarManager.SetProxyForCurrentProcess(bundle.GlobalProxy!);

        // 预设
        var profileCount = 0;
        foreach (var p in bundle.Profiles)
        {
            if (!overwrite && ProfileManager.Load(p.Name) != null) continue;
            ProfileManager.Save(p);
            profileCount++;
        }

        // 工具路径
        foreach (var kv in bundle.ToolPaths)
            ToolRegistry.SetCustomPath(kv.Key, kv.Value.Exec, kv.Value.Config);

        return (profileCount, bundle.ToolPaths.Count, hasGlobal);
    }
}
