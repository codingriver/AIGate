using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gate.Models;
using Gate.Configurators;
using Gate.Managers;

namespace Gate.Backup
{
    /// <summary>
    /// 备份管理器 - 自动备份与版本管理
    /// </summary>
    public class BackupManager
    {
        private static readonly string BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Gate", "backups");
        
        private static readonly int MaxBackupCount = 50;
        
        /// <summary>
        /// 备份配置
        /// </summary>
        public class BackupEntry
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Description { get; set; } = "";
            public string BackupPath { get; set; } = "";
            public List<string> BackedUpTools { get; set; } = new();
            public long SizeBytes { get; set; }
        }
        
        /// <summary>
        /// 创建备份
        /// </summary>
        public static BackupEntry? CreateBackup(string? description = null)
        {
            try
            {
                EnsureBackupDirectory();
                
                var timestamp = DateTime.Now;
                var backupId = timestamp.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(BackupDirectory, $"backup_{backupId}");
                Directory.CreateDirectory(backupPath);
                
                var entry = new BackupEntry
                {
                    Id = backupId,
                    Timestamp = timestamp,
                    Description = description ?? "自动备份",
                    BackupPath = backupPath
                };
                
                // 备份环境变量配置
                var envConfig = EnvVarManager.GetProxyConfig(EnvLevel.User);
                if (!envConfig.IsEmpty)
                {
                    var envPath = Path.Combine(backupPath, "env.json");
                    File.WriteAllText(envPath, JsonSerializer.Serialize(envConfig, new JsonSerializerOptions { WriteIndented = true }));
                    entry.BackedUpTools.Add("env");
                }
                
                // 备份各工具配置
                foreach (var tool in ToolRegistry.GetAllTools())
                {
                    var config = tool.GetCurrentConfig();
                    if (config != null && !config.IsEmpty)
                    {
                        var toolPath = Path.Combine(backupPath, $"tool_{tool.ToolName}.json");
                        File.WriteAllText(toolPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
                        entry.BackedUpTools.Add(tool.ToolName);
                    }
                }
                
                // 计算大小
                entry.SizeBytes = Directory.GetFiles(backupPath).Sum(f => new FileInfo(f).Length);
                
                // 保存备份元信息
                var metaPath = Path.Combine(backupPath, "meta.json");
                File.WriteAllText(metaPath, JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
                
                // 清理旧备份
                CleanupOldBackups();
                
                return entry;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"备份失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 列出所有备份
        /// </summary>
        public static List<BackupEntry> ListBackups()
        {
            var backups = new List<BackupEntry>();
            
            if (!Directory.Exists(BackupDirectory))
                return backups;
            
            foreach (var dir in Directory.GetDirectories(BackupDirectory))
            {
                var metaPath = Path.Combine(dir, "meta.json");
                if (File.Exists(metaPath))
                {
                    try
                    {
                        var json = File.ReadAllText(metaPath);
                        var entry = JsonSerializer.Deserialize<BackupEntry>(json);
                        if (entry != null)
                        {
                            entry.BackupPath = dir;
                            backups.Add(entry);
                        }
                    }
                    catch { }
                }
            }
            
            return backups.OrderByDescending(b => b.Timestamp).ToList();
        }
        
        /// <summary>
        /// 恢复备份
        /// </summary>
        public static bool RestoreBackup(string backupId)
        {
            var backups = ListBackups();
            var backup = backups.FirstOrDefault(b => b.Id == backupId || b.Timestamp.ToString("yyyyMMdd_HHmmss") == backupId);
            
            if (backup == null)
            {
                Console.WriteLine($"未找到备份: {backupId}");
                return false;
            }
                
            try
            {
                var backupPath = backup.BackupPath;
                
                // 恢复环境变量
                var envPath = Path.Combine(backupPath, "env.json");
                if (File.Exists(envPath))
                {
                    var json = File.ReadAllText(envPath);
                    var config = JsonSerializer.Deserialize<ProxyConfig>(json);
                    if (config != null)
                    {
                        EnvVarManager.SetProxyForCurrentProcess(config);
                    }
                }
                
                // 恢复工具配置
                foreach (var toolName in backup.BackedUpTools)
                {
                    if (toolName == "env") continue;
                    
                    var toolPath = Path.Combine(backupPath, $"tool_{toolName}.json");
                    if (File.Exists(toolPath))
                    {
                        var tool = ToolRegistry.GetByName(toolName);
                        if (tool != null)
                        {
                            var json = File.ReadAllText(toolPath);
                            var config = JsonSerializer.Deserialize<ProxyConfig>(json);
                            if (config != null && !string.IsNullOrEmpty(config.HttpProxy))
                            {
                                tool.SetProxy(config.HttpProxy);
                            }
                        }
                    }
                }
                
                Console.WriteLine($"✅ 已恢复到备份: {backup.Timestamp:yyyy-MM-dd HH:mm:ss}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 删除备份
        /// </summary>
        public static bool DeleteBackup(string backupId)
        {
            var backups = ListBackups();
            var backup = backups.FirstOrDefault(b => b.Id == backupId);
            
            if (backup == null)
            {
                Console.WriteLine($"未找到备份: {backupId}");
                return false;
            }
            
            try
            {
                Directory.Delete(backup.BackupPath, true);
                Console.WriteLine($"✅ 已删除备份: {backupId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 清理旧备份
        /// </summary>
        private static void CleanupOldBackups()
        {
            var backups = ListBackups();
            
            if (backups.Count > MaxBackupCount)
            {
                var toDelete = backups.Skip(MaxBackupCount);
                foreach (var backup in toDelete)
                {
                    try
                    {
                        Directory.Delete(backup.BackupPath, true);
                    }
                    catch { }
                }
            }
        }
        
        /// <summary>
        /// 确保备份目录存在
        /// </summary>
        private static void EnsureBackupDirectory()
        {
            if (!Directory.Exists(BackupDirectory))
                Directory.CreateDirectory(BackupDirectory);
        }
        
        /// <summary>
        /// 获取备份目录路径
        /// </summary>
        public static string GetBackupDirectory() => BackupDirectory;
    }
}