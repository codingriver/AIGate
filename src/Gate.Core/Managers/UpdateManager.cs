using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gate.Managers
{
    /// <summary>
    /// 版本信息
    /// </summary>
    public class VersionInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
    }
    
    /// <summary>
    /// 更新管理器
    /// </summary>
    public static class UpdateManager
    {
        private const string CurrentVersion = "1.0.0";
        private const string UpdateCheckUrl = "https://api.github.com/repos/yourname/proxy-tool/releases/latest";
        
        /// <summary>
        /// 获取当前版本
        /// </summary>
        public static string GetCurrentVersion() => CurrentVersion;
        
        /// <summary>
        /// 检查更新
        /// </summary>
        public static async Task<VersionInfo?> CheckForUpdateAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ProxyTool-UpdateChecker");
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetStringAsync(UpdateCheckUrl);
                var release = JsonSerializer.Deserialize<JsonElement>(response);
                
                var latestVersion = release.GetProperty("tag_name").GetString()?.TrimStart('v');
                if (string.IsNullOrEmpty(latestVersion))
                    return null;
                
                // 比较版本
                if (IsNewerVersion(latestVersion, CurrentVersion))
                {
                    return new VersionInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = release.GetProperty("html_url").GetString() ?? "",
                        ReleaseNotes = release.GetProperty("body").GetString() ?? "",
                        ReleaseDate = release.GetProperty("published_at").GetDateTime()
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("检查更新失败", ex);
                return null;
            }
        }
        
        /// <summary>
        /// 比较版本号
        /// </summary>
        private static bool IsNewerVersion(string newVersion, string currentVersion)
        {
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
            
            for (int i = 0; i < Math.Max(newParts.Length, currentParts.Length); i++)
            {
                int newPart = i < newParts.Length ? newParts[i] : 0;
                int currentPart = i < currentParts.Length ? currentParts[i] : 0;
                
                if (newPart > currentPart)
                    return true;
                if (newPart < currentPart)
                    return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// 打印更新信息
        /// </summary>
        public static void PrintUpdateInfo(VersionInfo info)
        {
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine("║         发现新版本!                ║");
            Console.WriteLine("╚════════════════════════════════════╝");
            Console.WriteLine($"当前版本: {CurrentVersion}");
            Console.WriteLine($"最新版本: {info.Version}");
            Console.WriteLine($"发布日期: {info.ReleaseDate:yyyy-MM-dd}");
            Console.WriteLine();
            Console.WriteLine("更新内容:");
            Console.WriteLine(info.ReleaseNotes);
            Console.WriteLine();
            Console.WriteLine($"下载地址: {info.DownloadUrl}");
        }
    }
}
