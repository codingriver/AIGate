using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Gate.Models;

namespace Gate.Managers;

/// <summary>
/// 从程序集嵌入资源加载内置工具描述符。
/// 工具 JSON 文件编译时以 EmbeddedResource 方式打包进 Gate.Core.dll。
/// </summary>
public static class EmbeddedToolDescriptors
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true,
        Converters = { new PlatformStringConverter() }
    };

    /// <summary>加载所有内置工具描述符</summary>
    public static IReadOnlyList<ToolDescriptor> LoadAll()
    {
        var results  = new List<ToolDescriptor>();
        var assembly = typeof(EmbeddedToolDescriptors).Assembly;

        foreach (var name in assembly.GetManifestResourceNames())
        {
            // 只加载 tools/ 目录下的 JSON
            if (!name.Contains(".tools.") || !name.EndsWith(".json",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream == null) continue;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var desc = JsonSerializer.Deserialize<ToolDescriptor>(json, _opts);
                if (desc != null && !string.IsNullOrWhiteSpace(desc.ToolName))
                    results.Add(desc);
            }
            catch
            {
                // 单个文件解析失败不影响其他工具
            }
        }

        return results;
    }

    /// <summary>从用户插件目录加载额外描述符（~/.local/share/gate/plugins/）</summary>
    public static IReadOnlyList<ToolDescriptor> LoadFromPluginsDir()
    {
        var results    = new List<ToolDescriptor>();
        var pluginsDir = GatePaths.PluginsDir;
        if (!Directory.Exists(pluginsDir)) return results;

        foreach (var jsonFile in Directory.GetFiles(pluginsDir, "tool.json",
                     SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(jsonFile);
                var desc = JsonSerializer.Deserialize<ToolDescriptor>(json, _opts);
                if (desc != null && !string.IsNullOrWhiteSpace(desc.ToolName))
                    results.Add(desc);
            }
            catch { }
        }

        return results;
    }
}
