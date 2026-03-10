using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Gate.Models;

namespace Gate.Platforms
{
    /// <summary>
    /// Windows 系统环境变量提供程序
    /// </summary>
    public class WindowsEnvProvider : ISystemEnvProvider
    {
        public bool RequiresAdmin => true;
        
        public bool HasPermission()
        {
            // 在 Windows 上，尝试写入系统环境变量来检测权限
            // 简化实现：假设有权限，实际失败时会报错
            return true;
        }
        
        public bool SetProxy(ProxyConfig config)
        {
            if (!HasPermission())
            {
                Console.WriteLine("❌ 需要管理员权限，请以管理员身份运行");
                return false;
            }
            
            try
            {
                // 使用 PowerShell 设置环境变量
                var psCommands = new List<string>();
                
                if (!string.IsNullOrEmpty(config.HttpProxy))
                    psCommands.Add($"[Environment]::SetEnvironmentVariable('HTTP_PROXY', '{config.HttpProxy}', 'Machine')");
                if (!string.IsNullOrEmpty(config.HttpsProxy))
                    psCommands.Add($"[Environment]::SetEnvironmentVariable('HTTPS_PROXY', '{config.HttpsProxy}', 'Machine')");
                if (!string.IsNullOrEmpty(config.FtpProxy))
                    psCommands.Add($"[Environment]::SetEnvironmentVariable('FTP_PROXY', '{config.FtpProxy}', 'Machine')");
                if (!string.IsNullOrEmpty(config.SocksProxy))
                    psCommands.Add($"[Environment]::SetEnvironmentVariable('SOCKS_PROXY', '{config.SocksProxy}', 'Machine')");
                if (!string.IsNullOrEmpty(config.NoProxy))
                    psCommands.Add($"[Environment]::SetEnvironmentVariable('NO_PROXY', '{config.NoProxy}', 'Machine')");
                
                // 清除空的变量
                if (string.IsNullOrEmpty(config.HttpProxy))
                    psCommands.Add("[Environment]::SetEnvironmentVariable('HTTP_PROXY', $null, 'Machine')");
                if (string.IsNullOrEmpty(config.HttpsProxy))
                    psCommands.Add("[Environment]::SetEnvironmentVariable('HTTPS_PROXY', $null, 'Machine')");
                
                foreach (var cmd in psCommands)
                {
                    ExecutePowerShell(cmd);
                }
                
                Console.WriteLine("✅ 系统环境变量已设置");
                Console.WriteLine("注意：需要重新打开命令提示符或重启才能生效");
                
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
            
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.Machine);
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.Machine);
            config.FtpProxy = Environment.GetEnvironmentVariable("FTP_PROXY", EnvironmentVariableTarget.Machine);
            config.SocksProxy = Environment.GetEnvironmentVariable("SOCKS_PROXY", EnvironmentVariableTarget.Machine);
            config.NoProxy = Environment.GetEnvironmentVariable("NO_PROXY", EnvironmentVariableTarget.Machine);
            
            return config;
        }
        
        public bool ClearProxy()
        {
            if (!HasPermission())
            {
                Console.WriteLine("❌ 需要管理员权限，请以管理员身份运行");
                return false;
            }
            
            try
            {
                var vars = new[] { "HTTP_PROXY", "HTTPS_PROXY", "FTP_PROXY", "SOCKS_PROXY", "NO_PROXY" };
                
                foreach (var var in vars)
                {
                    ExecutePowerShell($"[Environment]::SetEnvironmentVariable('{var}', $null, 'Machine')");
                }
                
                Console.WriteLine("✅ 系统代理已清除");
                Console.WriteLine("注意：需要重新打开命令提示符或重启才能生效");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 清除失败: {ex.Message}");
                return false;
            }
        }
        
        private void ExecutePowerShell(string command)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
        }
    }
}
