using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Gate.Models;

namespace Gate.Platforms
{
    /// <summary>
    /// Linux 系统环境变量提供程序
    /// </summary>
    public class LinuxEnvProvider : ISystemEnvProvider
    {
        public bool RequiresAdmin => true;
        
        public bool HasPermission()
        {
            // 检查是否为 root
            return getuid() == 0;
        }
        
        public bool SetProxy(ProxyConfig config)
        {
            if (!HasPermission())
            {
                Console.WriteLine("❌ 需要 root 权限，请使用 sudo 运行");
                return false;
            }
            
            try
            {
                // 备份原文件
                if (File.Exists("/etc/environment"))
                {
                    File.Copy("/etc/environment", "/etc/environment.bak", overwrite: true);
                }
                
                // 读取现有内容
                var lines = File.Exists("/etc/environment") 
                    ? new List<string>(File.ReadAllLines("/etc/environment"))
                    : new List<string>();
                
                // 移除旧的代理设置
                lines.RemoveAll(l => l.StartsWith("HTTP_PROXY=") || 
                                     l.StartsWith("HTTPS_PROXY=") ||
                                     l.StartsWith("FTP_PROXY=") ||
                                     l.StartsWith("SOCKS_PROXY=") ||
                                     l.StartsWith("NO_PROXY="));
                
                // 添加新的代理设置
                if (!string.IsNullOrEmpty(config.HttpProxy))
                    lines.Add($"HTTP_PROXY={config.HttpProxy}");
                if (!string.IsNullOrEmpty(config.HttpsProxy))
                    lines.Add($"HTTPS_PROXY={config.HttpsProxy}");
                if (!string.IsNullOrEmpty(config.FtpProxy))
                    lines.Add($"FTP_PROXY={config.FtpProxy}");
                if (!string.IsNullOrEmpty(config.SocksProxy))
                    lines.Add($"SOCKS_PROXY={config.SocksProxy}");
                if (!string.IsNullOrEmpty(config.NoProxy))
                    lines.Add($"NO_PROXY={config.NoProxy}");
                
                // 写入文件
                File.WriteAllLines("/etc/environment", lines);
                
                Console.WriteLine("✅ 系统环境变量已设置");
                Console.WriteLine("注意：需要重新登录或重启才能生效");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置失败: {ex.Message}");
                return false;
            }
        }
        
        public ProxyConfig GetProxy()
        {
            var config = new ProxyConfig();
            
            if (File.Exists("/etc/environment"))
            {
                var lines = File.ReadAllLines("/etc/environment");
                foreach (var line in lines)
                {
                    if (line.StartsWith("HTTP_PROXY="))
                        config.HttpProxy = line.Substring("HTTP_PROXY=".Length).Trim('"');
                    else if (line.StartsWith("HTTPS_PROXY="))
                        config.HttpsProxy = line.Substring("HTTPS_PROXY=".Length).Trim('"');
                    else if (line.StartsWith("FTP_PROXY="))
                        config.FtpProxy = line.Substring("FTP_PROXY=".Length).Trim('"');
                    else if (line.StartsWith("SOCKS_PROXY="))
                        config.SocksProxy = line.Substring("SOCKS_PROXY=".Length).Trim('"');
                    else if (line.StartsWith("NO_PROXY="))
                        config.NoProxy = line.Substring("NO_PROXY=".Length).Trim('"');
                }
            }
            
            return config;
        }
        
        public bool ClearProxy()
        {
            if (!HasPermission())
            {
                Console.WriteLine("❌ 需要 root 权限，请使用 sudo 运行");
                return false;
            }
            
            try
            {
                if (!File.Exists("/etc/environment"))
                {
                    return true;
                }
                
                // 备份原文件
                File.Copy("/etc/environment", "/etc/environment.bak", overwrite: true);
                
                // 读取并过滤掉代理设置
                var lines = File.ReadAllLines("/etc/environment");
                var filtered = lines.Where(l => !l.StartsWith("HTTP_PROXY=") && 
                                                !l.StartsWith("HTTPS_PROXY=") &&
                                                !l.StartsWith("FTP_PROXY=") &&
                                                !l.StartsWith("SOCKS_PROXY=") &&
                                                !l.StartsWith("NO_PROXY=")).ToArray();
                
                File.WriteAllLines("/etc/environment", filtered);
                
                Console.WriteLine("✅ 系统代理已清除");
                Console.WriteLine("注意：需要重新登录或重启才能生效");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 清除失败: {ex.Message}");
                return false;
            }
        }
        
        [DllImport("libc")]
        private static extern uint getuid();
    }
}
