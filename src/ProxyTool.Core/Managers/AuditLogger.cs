using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ProxyTool.Managers
{
    /// <summary>
    /// 审计日志条目
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; } = "";
        public string ToolName { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string Username { get; set; } = "";
    }
    
    /// <summary>
    /// 审计日志管理器
    /// </summary>
    public static class AuditLogger
    {
        private static readonly string AuditDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProxyTool",
            "audit"
        );
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        static AuditLogger()
        {
            if (!Directory.Exists(AuditDir))
                Directory.CreateDirectory(AuditDir);
        }
        
        /// <summary>
        /// 记录审计日志
        /// </summary>
        public static void Log(string operation, string toolName, string? oldValue, string? newValue, bool success, string? errorMessage = null)
        {
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                Operation = operation,
                ToolName = toolName,
                OldValue = oldValue,
                NewValue = newValue,
                Success = success,
                ErrorMessage = errorMessage,
                Username = Environment.UserName
            };
            
            var auditFile = Path.Combine(AuditDir, $"audit_{DateTime.Now:yyyyMM}.json");
            
            List<AuditLogEntry> entries;
            if (File.Exists(auditFile))
            {
                var json = File.ReadAllText(auditFile);
                entries = JsonSerializer.Deserialize<List<AuditLogEntry>>(json, JsonOptions) ?? new List<AuditLogEntry>();
            }
            else
            {
                entries = new List<AuditLogEntry>();
            }
            
            entries.Add(entry);
            
            // 保留最近1000条记录
            if (entries.Count > 1000)
                entries = entries.Skip(entries.Count - 1000).ToList();
            
            File.WriteAllText(auditFile, JsonSerializer.Serialize(entries, JsonOptions));
        }
        
        /// <summary>
        /// 查询审计日志
        /// </summary>
        public static List<AuditLogEntry> Query(DateTime? since = null, DateTime? until = null, string? toolName = null)
        {
            var results = new List<AuditLogEntry>();
            
            var files = Directory.GetFiles(AuditDir, "audit_*.json");
            foreach (var file in files)
            {
                if (!File.Exists(file))
                    continue;
                
                var json = File.ReadAllText(file);
                var entries = JsonSerializer.Deserialize<List<AuditLogEntry>>(json, JsonOptions);
                
                if (entries != null)
                {
                    results.AddRange(entries);
                }
            }
            
            // 过滤
            if (since.HasValue)
                results = results.Where(e => e.Timestamp >= since.Value).ToList();
            
            if (until.HasValue)
                results = results.Where(e => e.Timestamp <= until.Value).ToList();
            
            if (!string.IsNullOrEmpty(toolName))
                results = results.Where(e => e.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase)).ToList();
            
            return results.OrderByDescending(e => e.Timestamp).ToList();
        }
        
        /// <summary>
        /// 获取最近的操作
        /// </summary>
        public static List<AuditLogEntry> GetRecent(int count = 10)
        {
            return Query().Take(count).ToList();
        }
    }
}
