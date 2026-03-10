using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gate.Models;

namespace Gate.Managers
{
    /// <summary>
    /// 配置模板管理器
    /// </summary>
    public class TemplateManager
    {
        private static readonly List<ProxyTemplate> _builtInTemplates = new()
        {
            // 公司网络代理
            new ProxyTemplate
            {
                Id = "corporate",
                Name = "公司网络",
                Description = "适用于公司内网环境",
                Category = "企业",
                ProxyUrl = "http://proxy.company.com:8080",
                NoProxy = "localhost,127.0.0.1,.company.com",
                Tags = new List<string> { "企业", "公司" }
            },
            
            // 机场代理
            new ProxyTemplate
            {
                Id = "airport",
                Name = "机场/代理服务",
                Description = "适用于机场或代理服务提供商",
                Category = "代理服务",
                ProxyUrl = "http://localhost:7890",
                NoProxy = "localhost,127.0.0.1",
                Tags = new List<string> { "机场", "代理", "翻墙" }
            },
            
            // 直连/无代理
            new ProxyTemplate
            {
                Id = "direct",
                Name = "直连",
                Description = "不使用代理，直连网络",
                Category = "网络",
                ProxyUrl = "",
                NoProxy = "",
                Tags = new List<string> { "直连", "无代理" }
            },
            
            // V2Ray
            new ProxyTemplate
            {
                Id = "v2ray",
                Name = "V2Ray 本地代理",
                Description = "V2Ray 默认本地端口",
                Category = "代理服务",
                ProxyUrl = "http://127.0.0.1:62789",
                NoProxy = "localhost,127.0.0.1,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16",
                Tags = new List<string> { "V2Ray", "VMess" }
            },
            
            // Clash
            new ProxyTemplate
            {
                Id = "clash",
                Name = "Clash 默认代理",
                Description = "Clash 默认 HTTP 端口",
                Category = "代理服务",
                ProxyUrl = "http://127.0.0.1:7890",
                NoProxy = "localhost,127.0.0.1",
                Tags = new List<string> { "Clash", "代理" }
            },
            
            // SSH 隧道
            new ProxyTemplate
            {
                Id = "ssh",
                Name = "SSH 隧道",
                Description = "SSH 本地 SOCKS5 代理",
                Category = "代理服务",
                ProxyUrl = "socks5://127.0.0.1:1080",
                NoProxy = "localhost,127.0.0.1",
                Tags = new List<string> { "SSH", "隧道" }
            },
            
            // 移动网络
            new ProxyTemplate
            {
                Id = "mobile",
                Name = "移动网络",
                Description = "适用于移动网络环境",
                Category = "网络",
                ProxyUrl = "",
                NoProxy = "*",
                Tags = new List<string> { "移动", "4G", "5G" }
            }
        };

        /// <summary>
        /// 获取所有内置模板
        /// </summary>
        public static IReadOnlyList<ProxyTemplate> GetBuiltInTemplates() => _builtInTemplates;

        /// <summary>
        /// 按分类获取模板
        /// </summary>
        public static IReadOnlyList<ProxyTemplate> GetByCategory(string category)
        {
            return _builtInTemplates.Where(t => t.Category == category).ToList();
        }

        /// <summary>
        /// 按标签搜索模板
        /// </summary>
        public static IReadOnlyList<ProxyTemplate> SearchByTag(string tag)
        {
            return _builtInTemplates
                .Where(t => t.Tags.Any(t => t.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// 按名称搜索模板
        /// </summary>
        public static IReadOnlyList<ProxyTemplate> SearchByName(string keyword)
        {
            return _builtInTemplates
                .Where(t => t.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           t.Description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>
        /// 根据 ID 获取模板
        /// </summary>
        public static ProxyTemplate? GetById(string id)
        {
            return _builtInTemplates.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public static IReadOnlyList<string> GetCategories()
        {
            return _builtInTemplates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// 导出模板到文件
        /// </summary>
        public static bool ExportTemplate(ProxyTemplate template, string filePath)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(template, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从文件导入模板
        /// </summary>
        public static ProxyTemplate? ImportTemplate(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                
                var json = File.ReadAllText(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<ProxyTemplate>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 代理配置模板
    /// </summary>
    public class ProxyTemplate
    {
        /// <summary>
        /// 模板唯一标识
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 模板描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 代理服务器地址
        /// </summary>
        public string ProxyUrl { get; set; } = "";

        /// <summary>
        /// 跳过代理的地址
        /// </summary>
        public string NoProxy { get; set; } = "";

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 转换为 ProxyConfig
        /// </summary>
        public ProxyConfig ToProxyConfig()
        {
            return new ProxyConfig
            {
                HttpProxy = ProxyUrl,
                HttpsProxy = ProxyUrl,
                NoProxy = NoProxy
            };
        }
    }
}