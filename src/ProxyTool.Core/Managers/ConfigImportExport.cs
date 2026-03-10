using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProxyTool.Models;

namespace ProxyTool.Managers
{
    /// <summary>
    /// 配置文件格式
    /// </summary>
    public enum ConfigFormat
    {
        Json,
        Yaml,
        Env,
        Text
    }

    /// <summary>
    /// 配置导入器
    /// </summary>
    public static class ConfigImporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        
        /// <summary>
        /// 从 JSON 文件导入
        /// </summary>
        public static Profile? ImportFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ 文件不存在: {filePath}");
                return null;
            }
            
            try
            {
                var json = File.ReadAllText(filePath);
                var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
                
                if (profile != null)
                {
                    profile.UpdatedAt = DateTime.Now;
                }
                
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 导入失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从 YAML 文件导入
        /// </summary>
        public static Profile? ImportFromYaml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ 文件不存在: {filePath}");
                return null;
            }
            
            try
            {
                var yaml = File.ReadAllText(filePath);
                return ParseYamlToProfile(yaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ YAML 导入失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从环境变量导入
        /// </summary>
        public static Profile? ImportFromEnvVars()
        {
            try
            {
                var config = EnvVarManager.GetProxyConfig(EnvLevel.User);
                return new Profile
                {
                    Name = "环境变量配置",
                    EnvVars = config,
                    UpdatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 环境变量导入失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从文件自动检测格式并导入
        /// </summary>
        public static Profile? AutoImport(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ 文件不存在: {filePath}");
                return null;
            }
            
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".json" => ImportFromJson(filePath),
                ".yaml" or ".yml" => ImportFromYaml(filePath),
                ".env" => ImportFromEnvVars(),
                _ => ImportFromJson(filePath) // 默认尝试 JSON
            };
        }

        /// <summary>
        /// 批量导入多个配置文件
        /// </summary>
        public static List<Profile> BatchImport(IEnumerable<string> filePaths)
        {
            var results = new List<Profile>();
            
            foreach (var filePath in filePaths)
            {
                var profile = AutoImport(filePath);
                if (profile != null)
                {
                    results.Add(profile);
                    Console.WriteLine($"✅ 导入成功: {profile.Name} ({Path.GetFileName(filePath)})");
                }
                else
                {
                    Console.WriteLine($"❌ 导入失败: {Path.GetFileName(filePath)}");
                }
            }
            
            return results;
        }

        /// <summary>
        /// 从目录批量导入
        /// </summary>
        public static List<Profile> ImportFromDirectory(string directoryPath, bool recursive = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"❌ 目录不存在: {directoryPath}");
                return new List<Profile>();
            }
            
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = new List<string>();
            
            // 支持的格式
            files.AddRange(Directory.GetFiles(directoryPath, "*.json", searchOption));
            files.AddRange(Directory.GetFiles(directoryPath, "*.yaml", searchOption));
            files.AddRange(Directory.GetFiles(directoryPath, "*.yml", searchOption));
            files.AddRange(Directory.GetFiles(directoryPath, "*.env", searchOption));
            
            Console.WriteLine($"📁 发现 {files.Count} 个配置文件");
            
            return BatchImport(files);
        }
        
        /// <summary>
        /// 从 URL 导入
        /// </summary>
        public static async Task<Profile?> ImportFromUrlAsync(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var json = await client.GetStringAsync(url);
                var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
                
                if (profile != null)
                {
                    profile.UpdatedAt = DateTime.Now;
                }
                
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 从 URL 导入失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 验证导入的配置
        /// </summary>
        public static bool ValidateImport(Profile profile)
        {
            if (string.IsNullOrEmpty(profile.Name))
            {
                Console.WriteLine("❌ 配置缺少名称");
                return false;
            }
            
            // 验证环境变量
            if (profile.EnvVars != null)
            {
                var validation = ConfigValidator.ValidateProxyConfig(profile.EnvVars);
                if (!validation.IsValid)
                {
                    Console.WriteLine($"❌ 环境变量配置无效: {validation.ErrorMessage}");
                    return false;
                }
            }
            
            // 验证工具配置
            if (profile.ToolConfigs != null)
            {
                foreach (var kvp in profile.ToolConfigs)
                {
                    var validation = ConfigValidator.ValidateProxyConfig(kvp.Value);
                    if (!validation.IsValid)
                    {
                        Console.WriteLine($"❌ 工具 {kvp.Key} 配置无效: {validation.ErrorMessage}");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 简单 YAML 解析
        /// </summary>
        private static Profile? ParseYamlToProfile(string yaml)
        {
            var profile = new Profile();
            var lines = yaml.Split('\n');
            var currentSection = "";
            var toolName = "";
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                
                // 顶级键
                if (trimmed.IndexOf(':') >= 0 && !trimmed.StartsWith(" ") && !trimmed.StartsWith("\t"))
                {
                    var colonIdx = trimmed.IndexOf(':');
                    var key = trimmed.Substring(0, colonIdx).Trim();
                    var value = colonIdx < trimmed.Length - 1 ? trimmed.Substring(colonIdx + 1).Trim() : "";
                    
                    switch (key.ToLower())
                    {
                        case "name":
                            profile.Name = value;
                            break;
                        case "description":
                            profile.Description = value;
                            break;
                    }
                }
                // 代理配置部分
                else if (trimmed.StartsWith("http_proxy:") || trimmed.StartsWith("https_proxy:") || 
                         trimmed.StartsWith("no_proxy:"))
                {
                    var colonIndex = trimmed.IndexOf(':');
                    currentSection = trimmed.Substring(0, colonIndex).Trim();
                    var value = colonIndex < trimmed.Length - 1 ? trimmed.Substring(colonIndex + 1).Trim() : "";
                    
                    profile.EnvVars ??= new ProxyConfig();
                    
                    if (currentSection == "http_proxy")
                        profile.EnvVars.HttpProxy = value;
                    else if (currentSection == "https_proxy")
                        profile.EnvVars.HttpsProxy = value;
                    else if (currentSection == "no_proxy")
                        profile.EnvVars.NoProxy = value;
                }
                // 工具配置部分
                else if (trimmed.EndsWith(":") && !trimmed.Contains(" "))
                {
                    toolName = trimmed.TrimEnd(':');
                    currentSection = "tool";
                }
                else if (currentSection == "tool" && toolName != "")
                {
                    if (trimmed.IndexOf(':') >= 0)
                    {
                        profile.ToolConfigs ??= new Dictionary<string, ProxyConfig>();
                        if (!profile.ToolConfigs.ContainsKey(toolName))
                            profile.ToolConfigs[toolName] = new ProxyConfig();
                        
                        var colonIdx = trimmed.IndexOf(':');
                        var key = trimmed.Substring(0, colonIdx).Trim();
                        var value = colonIdx < trimmed.Length - 1 ? trimmed.Substring(colonIdx + 1).Trim() : "";
                        
                        if (key == "http_proxy")
                            profile.ToolConfigs[toolName].HttpProxy = value;
                        else if (key == "https_proxy")
                            profile.ToolConfigs[toolName].HttpsProxy = value;
                    }
                }
            }
            
            return profile;
        }
    }
    
    /// <summary>
    /// 配置导出器
    /// </summary>
    public static class ConfigExporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        /// <summary>
        /// 导出到 JSON 文件
        /// </summary>
        public static bool ExportToJson(Profile profile, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(profile, JsonOptions);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 导出失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 导出到 YAML 文件
        /// </summary>
        public static bool ExportToYaml(Profile profile, string filePath)
        {
            try
            {
                var yaml = new StringBuilder();
                yaml.AppendLine($"# 代理配置文件");
                yaml.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                yaml.AppendLine();
                yaml.AppendLine($"name: {profile.Name}");
                if (!string.IsNullOrEmpty(profile.Description))
                    yaml.AppendLine($"description: {profile.Description}");
                yaml.AppendLine();
                
                // 导出环境变量配置
                if (profile.EnvVars != null)
                {
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpProxy))
                        yaml.AppendLine($"http_proxy: {profile.EnvVars.HttpProxy}");
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpsProxy))
                        yaml.AppendLine($"https_proxy: {profile.EnvVars.HttpsProxy}");
                    if (!string.IsNullOrEmpty(profile.EnvVars.NoProxy))
                        yaml.AppendLine($"no_proxy: {profile.EnvVars.NoProxy}");
                    yaml.AppendLine();
                }
                
                // 导出工具配置
                if (profile.ToolConfigs != null && profile.ToolConfigs.Count > 0)
                {
                    yaml.AppendLine("# 工具配置");
                    foreach (var kvp in profile.ToolConfigs)
                    {
                        yaml.AppendLine($"{kvp.Key}:");
                        if (!string.IsNullOrEmpty(kvp.Value.HttpProxy))
                            yaml.AppendLine($"  http_proxy: {kvp.Value.HttpProxy}");
                        if (!string.IsNullOrEmpty(kvp.Value.HttpsProxy))
                            yaml.AppendLine($"  https_proxy: {kvp.Value.HttpsProxy}");
                    }
                }
                
                File.WriteAllText(filePath, yaml.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ YAML 导出失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 导出到环境变量格式
        /// </summary>
        public static bool ExportToEnvVars(Profile profile, string filePath)
        {
            try
            {
                var env = new StringBuilder();
                env.AppendLine("# 代理环境变量配置");
                env.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                env.AppendLine();
                
                if (profile.EnvVars != null)
                {
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpProxy))
                        env.AppendLine($"export http_proxy=\"{profile.EnvVars.HttpProxy}\"");
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpsProxy))
                        env.AppendLine($"export https_proxy=\"{profile.EnvVars.HttpsProxy}\"");
                    if (!string.IsNullOrEmpty(profile.EnvVars.NoProxy))
                        env.AppendLine($"export no_proxy=\"{profile.EnvVars.NoProxy}\"");
                    
                    // Windows 格式
                    env.AppendLine();
                    env.AppendLine("# Windows 格式");
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpProxy))
                        env.AppendLine($"set http_proxy={profile.EnvVars.HttpProxy}");
                    if (!string.IsNullOrEmpty(profile.EnvVars.HttpsProxy))
                        env.AppendLine($"set https_proxy={profile.EnvVars.HttpsProxy}");
                }
                
                File.WriteAllText(filePath, env.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 环境变量导出失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 根据格式导出
        /// </summary>
        public static bool Export(Profile profile, string filePath, ConfigFormat format)
        {
            return format switch
            {
                ConfigFormat.Json => ExportToJson(profile, filePath),
                ConfigFormat.Yaml => ExportToYaml(profile, filePath),
                ConfigFormat.Env => ExportToEnvVars(profile, filePath),
                _ => ExportToJson(profile, filePath)
            };
        }
        
        /// <summary>
        /// 导出当前配置
        /// </summary>
        public static Profile ExportCurrentConfig(string name)
        {
            var profile = new Profile
            {
                Name = name,
                EnvVars = EnvVarManager.GetProxyConfig(EnvLevel.User),
                ToolConfigs = new Dictionary<string, ProxyConfig>()
            };
            
            // 收集所有工具配置
            foreach (var tool in ToolRegistry.GetAllTools())
            {
                var config = tool.GetCurrentConfig();
                if (config != null && !config.IsEmpty)
                {
                    profile.ToolConfigs[tool.ToolName] = config;
                }
            }
            
            return profile;
        }
    }
}
