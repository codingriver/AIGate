using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProxyTool.Models;

namespace ProxyTool.Managers;

/// <summary>
/// 配置集（Profile）管理器
/// </summary>
public static class ProfileManager
{
    private static readonly string ProfilesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProxyTool",
        "profiles"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static ProfileManager()
    {
        if (!Directory.Exists(ProfilesDir))
            Directory.CreateDirectory(ProfilesDir);
    }

    /// <summary>
    /// 保存配置集
    /// </summary>
    public static void Save(Profile profile)
    {
        profile.UpdatedAt = DateTime.Now;
        var filePath = GetProfilePath(profile.Name);
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 加载配置集
    /// </summary>
    public static Profile? Load(string name)
    {
        var filePath = GetProfilePath(name);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Profile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 列出所有配置集
    /// </summary>
    public static List<string> List()
    {
        if (!Directory.Exists(ProfilesDir))
            return new List<string>();

        return Directory.GetFiles(ProfilesDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }

    /// <summary>
    /// 删除配置集
    /// </summary>
    public static bool Delete(string name)
    {
        var filePath = GetProfilePath(name);
        if (!File.Exists(filePath))
            return false;

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取默认配置集名称
    /// </summary>
    public static string? GetDefaultProfile()
    {
        var configFile = Path.Combine(Path.GetDirectoryName(ProfilesDir)!, "config.json");
        if (!File.Exists(configFile))
            return null;

        try
        {
            var json = JsonDocument.Parse(File.ReadAllText(configFile));
            return json.RootElement.GetProperty("defaultProfile").GetString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 设置默认配置集
    /// </summary>
    public static void SetDefaultProfile(string name)
    {
        var configFile = Path.Combine(Path.GetDirectoryName(ProfilesDir)!, "config.json");
        var config = new { defaultProfile = name };
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configFile, json);
    }

    private static string GetProfilePath(string name)
    {
        return Path.Combine(ProfilesDir, $"{SanitizeFileName(name)}.json");
    }

    private static string SanitizeFileName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    }
}