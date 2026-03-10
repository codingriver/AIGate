using System;
using System.Net;
using ProxyTool.Models;

namespace ProxyTool.Managers
{
    /// <summary>
    /// 配置验证器
    /// </summary>
    public static class ConfigValidator
    {
        /// <summary>
        /// 验证代理 URL
        /// </summary>
        public static ValidationResult ValidateProxyUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return ValidationResult.Success();
            }
            
            // 检查协议
            var validSchemes = new[] { "http://", "https://", "socks4://", "socks5://", "socks://" };
            var hasValidScheme = false;
            
            foreach (var scheme in validSchemes)
            {
                if (url.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                {
                    hasValidScheme = true;
                    break;
                }
            }
            
            if (!hasValidScheme && !url.Contains("://"))
            {
                // 没有协议，默认添加 http://
                url = "http://" + url;
            }
            else if (!hasValidScheme)
            {
                return ValidationResult.Failure($"无效的代理协议。支持的协议: http, https, socks4, socks5");
            }
            
            // 解析 URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri == null)
            {
                return ValidationResult.Failure("无效的代理地址格式");
            }
            
            // 验证主机
            if (string.IsNullOrEmpty(uri.Host))
            {
                return ValidationResult.Failure("代理地址必须包含主机名");
            }

            // 拒绝明显无效的主机名：需为 IP、localhost 或包含点的域名
            var host = uri.Host;
            if (host != "localhost" &&
                !IPAddress.TryParse(host, out _) &&
                !host.Contains("."))
            {
                return ValidationResult.Failure("无效的代理主机名");
            }
            
            // 验证端口
            if (uri.Port < 1 || uri.Port > 65535)
            {
                return ValidationResult.Failure("代理端口必须在 1-65535 范围内");
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// 验证端口
        /// </summary>
        public static ValidationResult ValidatePort(int port)
        {
            if (port < 1 || port > 65535)
            {
                return ValidationResult.Failure("端口必须在 1-65535 范围内");
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// 验证 IP 地址
        /// </summary>
        public static ValidationResult ValidateIpAddress(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return ValidationResult.Success();
            }
            
            if (!IPAddress.TryParse(ip, out _))
            {
                return ValidationResult.Failure("无效的 IP 地址格式");
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// 验证完整代理配置
        /// </summary>
        public static ValidationResult ValidateProxyConfig(ProxyConfig config)
        {
            var httpResult = ValidateProxyUrl(config.HttpProxy);
            if (!httpResult.IsValid)
            {
                return ValidationResult.Failure($"HTTP 代理: {httpResult.ErrorMessage}");
            }
            
            var httpsResult = ValidateProxyUrl(config.HttpsProxy);
            if (!httpsResult.IsValid)
            {
                return ValidationResult.Failure($"HTTPS 代理: {httpsResult.ErrorMessage}");
            }
            
            var ftpResult = ValidateProxyUrl(config.FtpProxy);
            if (!ftpResult.IsValid)
            {
                return ValidationResult.Failure($"FTP 代理: {ftpResult.ErrorMessage}");
            }
            
            var socksResult = ValidateProxyUrl(config.SocksProxy);
            if (!socksResult.IsValid)
            {
                return ValidationResult.Failure($"SOCKS 代理: {socksResult.ErrorMessage}");
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }
        
        public static ValidationResult Failure(string message)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = message };
        }
    }
}
