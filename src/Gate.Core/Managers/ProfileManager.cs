using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gate.Models;

namespace Gate.Managers;

/// <summary>预设（Profile）管理器 — 使用 GatePaths 统一路径</summary>
public static class ProfileManager
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static ProfileManager() => GatePaths.EnsureDir(GatePaths.ProfilesDir);

    public static void Save(Profile profile)
    {
        profile.UpdatedAt = DateTime.Now;
        File.WriteAllText(GetPath(profile.Name),
            JsonSerializer.Serialize(profile, _opts));
    }

    public static Profile? Load(string name)
    {
        var f = GetPath(name);
        if (!File.Exists(f)) return null;
        try { return JsonSerializer.Deserialize<Profile>(File.ReadAllText(f), _opts); }
        catch { return null; }
    }

    public static List<string> List()
    {
        if (!Directory.Exists(GatePaths.ProfilesDir)) return new();
        return Directory.GetFiles(GatePaths.ProfilesDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }

    public static bool Delete(string name)
    {
        var f = GetPath(name);
        if (!File.Exists(f)) return false;
        try { File.Delete(f); return true; }
        catch { return false; }
    }

    public static string? GetDefaultProfile()
    {
        if (!File.Exists(GatePaths.ConfigFile)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(GatePaths.ConfigFile));
            return doc.RootElement.TryGetProperty("defaultProfile", out var p)
                ? p.GetString() : null;
        }
        catch { return null; }
    }

    public static void SetDefaultProfile(string name)
    {
        GatePaths.EnsureDir(GatePaths.DataDir);
        File.WriteAllText(GatePaths.ConfigFile,
            JsonSerializer.Serialize(new { defaultProfile = name }, _opts));
    }

    private static string GetPath(string name) =>
        Path.Combine(GatePaths.ProfilesDir, $"{SanitizeName(name)}.json");

    private static string SanitizeName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
