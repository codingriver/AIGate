using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gate.Managers;

/// <summary>代理地址历史记录（最近 20 条，去重）</summary>
public static class ProxyHistory
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
    private const int MaxEntries = 20;

    public static List<string> Load()
    {
        var f = GatePaths.HistoryFile;
        if (!File.Exists(f)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(
                File.ReadAllText(f)) ?? new();
        }
        catch { return new(); }
    }

    public static void Push(string proxyUrl)
    {
        if (string.IsNullOrWhiteSpace(proxyUrl)) return;
        var list = Load();
        list.RemoveAll(x => x.Equals(proxyUrl, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, proxyUrl);
        if (list.Count > MaxEntries) list = list.Take(MaxEntries).ToList();
        GatePaths.EnsureDir(GatePaths.DataDir);
        File.WriteAllText(GatePaths.HistoryFile, JsonSerializer.Serialize(list, _opts));
    }

    public static void Clear()
    {
        if (File.Exists(GatePaths.HistoryFile))
            File.Delete(GatePaths.HistoryFile);
    }
}
