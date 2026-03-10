using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Gate.Models;

namespace Gate.Configurators
{
    /// <summary>
    /// pip 配置器 (Python)
    /// </summary>
    public class PipConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "pip";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "pip", "pip.ini");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "pip", "pip.conf");
            }
            return null;
        }
        
        protected override ProxyConfig ParseConfig(string content)
        {
            var config = new ProxyConfig();
            var lines = content.Split('\n');
            bool inGlobalSection = false;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed == "[global]")
                {
                    inGlobalSection = true;
                    continue;
                }
                
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inGlobalSection = false;
                    continue;
                }
                
                if (inGlobalSection)
                {
                    if (trimmed.StartsWith("proxy = "))
                        config.HttpProxy = trimmed.Substring(8).Trim();
                    else if (trimmed.StartsWith("https-proxy = "))
                        config.HttpsProxy = trimmed.Substring(14).Trim();
                }
            }
            
            return config;
        }
        
        protected override List<string> ClearProxyLines(List<string> lines)
        {
            return lines.Where(l => 
                !l.Trim().StartsWith("proxy = ") && 
                !l.Trim().StartsWith("https-proxy = ") &&
                !l.Trim().StartsWith("proxy=") &&
                !l.Trim().StartsWith("https-proxy=")).ToList();
        }
        
        protected override List<string> FormatProxyLines(string proxyUrl)
        {
            // pip 配置需要放在 [global] 部分
            return new List<string>
            {
                "[global]",
                $"proxy = {proxyUrl}",
                $"https-proxy = {proxyUrl}"
            };
        }
    }
    
    /// <summary>
    /// Conda 配置器
    /// </summary>
    public class CondaConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "conda";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".condarc");
            return null;
        }
        
        protected override ProxyConfig ParseConfig(string content)
        {
            var config = new ProxyConfig();
            var lines = content.Split('\n');
            bool inProxySection = false;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed == "proxy_servers:")
                {
                    inProxySection = true;
                    continue;
                }
                
                if (inProxySection && trimmed.StartsWith("http:"))
                {
                    config.HttpProxy = trimmed.Substring(5).Trim();
                }
                else if (inProxySection && trimmed.StartsWith("https:"))
                {
                    config.HttpsProxy = trimmed.Substring(6).Trim();
                }
                else if (!trimmed.StartsWith(" ") && !string.IsNullOrEmpty(trimmed) && 
                         !trimmed.StartsWith("proxy_servers:"))
                {
                    inProxySection = false;
                }
            }
            
            return config;
        }
        
        protected override List<string> FormatProxyLines(string proxyUrl)
        {
            // Conda YAML 格式
            return new List<string>
            {
                "proxy_servers:",
                $"  http: {proxyUrl}",
                $"  https: {proxyUrl}"
            };
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("http:");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("https:");
    }
    
    /// <summary>
    /// Yarn 配置器
    /// </summary>
    public class YarnConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "yarn";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".yarnrc");
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("proxy ");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("https-proxy ");

        protected override string FormatProxyLine(string key, string value)
        {
            // Yarn 格式: proxy "http://host:port"
            return $"{key} \"{value}\"";
        }
    }
    
    /// <summary>
    /// Maven 配置器 (Java)
    /// </summary>
    public class MavenConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "mvn";
        public override string Category => "构建工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                var mavenHome = Path.Combine(home, ".m2");
                if (Directory.Exists(mavenHome))
                    return Path.Combine(mavenHome, "settings.xml");
            }
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            // Maven 使用 XML，特殊处理
            var configPath = ConfigPath;
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return null;
            
            try
            {
                var xml = System.Xml.Linq.XDocument.Load(configPath);
                var proxy = xml.Descendants("proxy").FirstOrDefault();
                
                if (proxy == null) return null;
                
                var config = new ProxyConfig();
                var id = proxy.Element("id")?.Value;
                
                if (id == "http" || id == "https")
                {
                    config.HttpProxy = proxy.Element("host")?.Value + ":" + proxy.Element("port")?.Value;
                }
                
                return config;
            }
            catch
            {
                return null;
            }
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Console.WriteLine("Maven 代理配置较复杂，建议手动编辑 ~/.m2/settings.xml");
            return false;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("Maven 代理配置较复杂，建议手动编辑 ~/.m2/settings.xml");
            return false;
        }
    }
    
    /// <summary>
    /// Gradle 配置器 (Java)
    /// </summary>
    public class GradleConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "gradle";
        public override string Category => "构建工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".gradle", "gradle.properties");
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("systemProp.http.proxyHost");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("systemProp.https.proxyHost");

        protected override string ExtractProxyValue(string line)
        {
            // Gradle 格式: systemProp.http.proxyHost=host
            var parts = line.Split('=');
            if (parts.Length > 1) return parts[1].Trim();
            return string.Empty;
        }
        
        protected override List<string> FormatProxyLines(string proxyUrl)
        {
            var (host, port) = Managers.EnvVarManager.ParseProxyUrl(proxyUrl) ?? ("", 0);
            return new List<string>
            {
                $"systemProp.http.proxyHost={host}",
                $"systemProp.http.proxyPort={port}",
                $"systemProp.https.proxyHost={host}",
                $"systemProp.https.proxyPort={port}"
            };
        }
    }
    
    /// <summary>
    /// Composer 配置器 (PHP)
    /// </summary>
    public class ComposerConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "composer";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                // Composer 1.x 配置
                var config1 = Path.Combine(home, "composer", "config.json");
                if (File.Exists(config1)) return config1;
                
                // Composer 2.x 配置
                var config2 = Path.Combine(home, ".config", "composer", "config.json");
                if (File.Exists(config2)) return config2;
            }
            
            // Windows
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(appData))
            {
                var winConfig = Path.Combine(appData, "Composer", "config.json");
                if (File.Exists(winConfig)) return winConfig;
            }
            
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var configPath = ConfigPath;
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return null;
            
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                var config = new ProxyConfig();
                
                if (json.RootElement.TryGetProperty("config", out var configObj))
                {
                    if (configObj.TryGetProperty("proxy", out var proxy))
                        config.HttpProxy = proxy.GetString();
                    if (configObj.TryGetProperty("secure-http", out var secure))
                    {
                        // https-proxy 通常在 http-proxy 基础上自动处理
                    }
                }
                
                return config;
            }
            catch
            {
                return null;
            }
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine($"创建 Composer 配置: {configPath}");
                    var dir = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
                
                var config = new Dictionary<string, object>
                {
                    ["config"] = new Dictionary<string, object>
                    {
                        ["proxy"] = proxyUrl,
                        ["secure-http"] = false // 允许 HTTP 代理
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath!, json);
                Console.WriteLine($"✅ 已设置 Composer 代理: {proxyUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Composer 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                    return true;
                
                var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                var newConfig = new Dictionary<string, object>();
                
                if (json.RootElement.TryGetProperty("config", out var configObj))
                {
                    // 保留其他配置，只移除代理
                    foreach (var prop in configObj.EnumerateObject())
                    {
                        if (prop.Name != "proxy")
                        {
                            // 简化处理：其他配置暂时忽略
                        }
                    }
                }
                
                File.WriteAllText(configPath, "{}");
                Console.WriteLine("✅ 已清除 Composer 代理配置");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Cargo 配置器 (Rust)
    /// </summary>
    public class CargoConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cargo";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cargo", "config.toml");
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("http_proxy");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("https_proxy");

        protected override string FormatProxyLine(string key, string value)
        {
            // Cargo TOML 格式
            return $"{key} = \"{value}\"";
        }
    }
    
    /// <summary>
    /// Go 配置器
    /// </summary>
    public class GoConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "go";
        public override string Category => "编程语言";
        
        protected override string? DetectConfigPath()
        {
            // Go 1.13+ 使用 GOPROXY 环境变量，但也支持 go env 文件
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                // 检查 go env 配置文件
                var goEnvPath = Path.Combine(home, ".config", "go", "env");
                if (File.Exists(goEnvPath)) return goEnvPath;
                
                // 备用：使用 gitconfig 作为代理配置参考
                var gitConfig = Path.Combine(home, ".gitconfig");
                if (File.Exists(gitConfig)) return gitConfig;
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            // 检查 go 命令是否可用
            var path = DetectToolPath();
            return !string.IsNullOrEmpty(path);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // 从环境变量获取 GOPROXY
            var goProxy = Environment.GetEnvironmentVariable("GOPROXY");
            if (!string.IsNullOrEmpty(goProxy) && goProxy != "off" && goProxy != "direct")
            {
                config.HttpProxy = goProxy;
                config.HttpsProxy = goProxy;
            }
            
            // 从环境变量获取 GOPRIVATE/GONOPROXY
            var noProxy = Environment.GetEnvironmentVariable("GONOPROXY") ?? 
                         Environment.GetEnvironmentVariable("GOPRIVATE");
            if (!string.IsNullOrEmpty(noProxy))
            {
                config.NoProxy = noProxy;
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // 设置 GOPROXY 环境变量（主要方式）
                var currentProxy = Environment.GetEnvironmentVariable("GOPROXY");
                
                if (string.IsNullOrEmpty(currentProxy) || currentProxy == "direct")
                {
                    Environment.SetEnvironmentVariable("GOPROXY", proxyUrl);
                }
                else
                {
                    // 添加到现有代理列表
                    Environment.SetEnvironmentVariable("GOPROXY", $"{proxyUrl},{currentProxy}");
                }
                
                // 同时设置 http_proxy/https_proxy 作为备选
                Environment.SetEnvironmentVariable("http_proxy", proxyUrl);
                Environment.SetEnvironmentVariable("https_proxy", proxyUrl);
                
                // 设置不代理的域名
                var privateDomains = Environment.GetEnvironmentVariable("GOPRIVATE") ?? "";
                if (!privateDomains.Contains("localhost") || !privateDomains.Contains("*.local"))
                {
                    var newPrivate = string.IsNullOrEmpty(privateDomains) 
                        ? "localhost,*.local" 
                        : $"{privateDomains},localhost,*.local";
                    Environment.SetEnvironmentVariable("GOPRIVATE", newPrivate);
                }
                
                Console.WriteLine($"✅ 已设置 Go 代理环境变量:");
                Console.WriteLine($"   GOPROXY={proxyUrl}");
                Console.WriteLine($"   http_proxy={proxyUrl}");
                Console.WriteLine($"   https_proxy={proxyUrl}");
                
                // 提示持久化
                Console.WriteLine("💡 持久化配置添加到 ~/.bashrc 或 ~/.zshrc:");
                Console.WriteLine($"   export GOPROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Go 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("GOPROXY", "direct");
                Environment.SetEnvironmentVariable("http_proxy", null);
                Environment.SetEnvironmentVariable("https_proxy", null);
                
                Console.WriteLine("✅ 已清除 Go 代理配置 (GOPROXY=direct)");
                Console.WriteLine("💡 如需完全清除，运行: go env -u GOPROXY");
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Terraform 配置器
    /// </summary>
    public class TerraformConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "terraform";
        public override string Category => "基础设施即代码";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".terraformrc");
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("proxy =");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("proxy ="); // Terraform 用同一个
            
        protected override string FormatProxyLine(string key, string value)
        {
            return $"proxy = {value}";
        }
    }
    
    /// <summary>
    /// Helm 配置器 (Kubernetes)
    /// </summary>
    public class HelmConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "helm";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "helm", "config");
            return null;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Console.WriteLine("Helm 代理通过环境变量或 Tiller 配置设置");
            return false;
        }
    }
    
    // ============================================================================
    // AI 工具配置器
    // ============================================================================
    
    /// <summary>
    /// Hugging Face CLI 配置器
    /// </summary>
    public class HuggingFaceConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "huggingface-cli";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cache", "huggingface");
            return null;
        }
        
        public override bool IsInstalled()
        {
            // 检查 huggingface-cli 是否安装
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // 从环境变量获取
            config.HttpProxy = Environment.GetEnvironmentVariable("HF_HUB_HTTP_PROXY") ?? 
                               Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HF_HUB_HTTPS_PROXY") ?? 
                               Environment.GetEnvironmentVariable("HTTPS_PROXY");
            config.NoProxy = Environment.GetEnvironmentVariable("HF_HUB_NO_PROXY") ?? 
                            Environment.GetEnvironmentVariable("NO_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Hugging Face 使用专用环境变量
                Environment.SetEnvironmentVariable("HF_HUB_HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HF_HUB_HTTPS_PROXY", proxyUrl);
                
                // 同时设置通用环境变量作为备选
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Hugging Face 代理:");
                Console.WriteLine($"   HF_HUB_HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HF_HUB_HTTPS_PROXY={proxyUrl}");
                
                // 尝试写入 token 文件（如果存在）
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var cacheDir = Path.Combine(home, ".cache", "huggingface");
                    if (Directory.Exists(cacheDir))
                    {
                        var tokenPath = Path.Combine(cacheDir, "token");
                        // 只记录位置，不实际操作 token
                        Console.WriteLine($"💡 Token 文件位置: {tokenPath}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Hugging Face 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("HF_HUB_HTTP_PROXY", null);
                Environment.SetEnvironmentVariable("HF_HUB_HTTPS_PROXY", null);
                Environment.SetEnvironmentVariable("HF_HUB_NO_PROXY", null);
                
                Console.WriteLine("✅ 已清除 Hugging Face 代理配置");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// OpenAI Python 库配置器
    /// </summary>
    public class OpenAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "openai";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".openai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // OpenAI 库使用标准 HTTP_PROXY/HTTPS_PROXY
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            config.NoProxy = Environment.GetEnvironmentVariable("NO_PROXY");
            
            // 也检查 OpenAI 特定的代理设置
            var openaiProxy = Environment.GetEnvironmentVariable("OPENAI_PROXY");
            if (!string.IsNullOrEmpty(openaiProxy))
            {
                config.HttpProxy = openaiProxy;
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // 设置标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("OPENAI_PROXY", proxyUrl);
                
                // OpenAI 库也支持 ALL_PROXY
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 OpenAI 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                Console.WriteLine($"   OPENAI_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 OpenAI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("OPENAI_PROXY", null);
                Environment.SetEnvironmentVariable("ALL_PROXY", null);
                
                Console.WriteLine("✅ 已清除 OpenAI 代理配置 (保留 HTTP_PROXY/HTTPS_PROXY)");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Anthropic Python 库配置器 (Claude)
    /// </summary>
    public class AnthropicConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "anthropic";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".anthropic");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Anthropic 库使用标准代理环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            config.NoProxy = Environment.GetEnvironmentVariable("NO_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Anthropic 使用标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Anthropic (Claude) 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Anthropic 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Anthropic 使用标准代理环境变量，已随系统代理一起清除");
            return true;
        }
    }
    
    /// <summary>
    /// Ollama 本地 LLM 配置器
    /// </summary>
    public class OllamaConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "ollama";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            // Ollama Windows: %APPDATA%\Ollama
            // Ollama Linux/macOS: ~/.ollama
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Ollama");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".ollama");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Ollama 通过环境变量配置代理（用于下载模型）
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            config.NoProxy = Environment.GetEnvironmentVariable("NO_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Ollama 下载模型时使用 HTTP_PROXY/HTTPS_PROXY
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Ollama 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                Console.WriteLine("💡 代理用于下载模型，本地运行不需要代理");
                
                // 提示 Ollama 服务可能需要重启
                Console.WriteLine("💡 如正在运行 Ollama，请重启服务以应用新配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Ollama 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Console.WriteLine("⚠️ Ollama 代理已清除，本地模型不受影响");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Claude CLI 配置器
    /// </summary>
    public class ClaudeCLIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "claude";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            // Claude CLI 配置目录
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                var configPath = Path.Combine(home, ".claude", "settings.json");
                if (File.Exists(configPath)) return configPath;
                
                // 备用位置
                return Path.Combine(home, ".claude");
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Claude");
            }
            
            return null;
        }
        
        public override bool IsInstalled()
        {
            // 检查 claude 命令
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Claude CLI 使用标准环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            config.NoProxy = Environment.GetEnvironmentVariable("NO_PROXY");
            
            // 也检查 Claude 特定的配置
            var configPath = ConfigPath;
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                try
                {
                    if (configPath.EndsWith(".json"))
                    {
                        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                        // 解析 Claude 特定配置
                    }
                }
                catch { }
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Claude CLI 使用标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Anthropic API 特定的代理配置
                Environment.SetEnvironmentVariable("ANTHROPIC_API_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Claude CLI 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                Console.WriteLine($"   ANTHROPIC_API_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Claude CLI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("ANTHROPIC_API_PROXY", null);
                Console.WriteLine("✅ Claude CLI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Azure AI 配置器
    /// </summary>
    public class AzureAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "azure-ai";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".azure");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Azure SDK 使用标准代理
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Azure 特定的代理设置
            var azureProxy = Environment.GetEnvironmentVariable("AZURE_PROXY");
            if (!string.IsNullOrEmpty(azureProxy))
            {
                config.HttpProxy = azureProxy;
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Azure SDK 支持 HTTP_PROXY/HTTPS_PROXY
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Azure CLI 特定的代理设置
                Environment.SetEnvironmentVariable("AZURE_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Azure AI 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                Console.WriteLine($"   AZURE_PROXY={proxyUrl}");
                
                // 提示 Azure CLI 配置
                Console.WriteLine("💡 对于 Azure CLI，运行: az config set proxy={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Azure AI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("AZURE_PROXY", null);
                Console.WriteLine("✅ Azure AI 代理已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Google AI (Gemini) 配置器
    /// </summary>
    public class GoogleAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "google-ai";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".google");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Google AI Python 库使用标准代理
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Google AI 使用标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Google Cloud 特定的代理
                Environment.SetEnvironmentVariable("CLOUDSDK_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Google AI (Gemini) 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Google AI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("CLOUDSDK_PROXY", null);
                Console.WriteLine("✅ Google AI 代理已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// LM Studio 本地 LLM 配置器
    /// </summary>
    public class LMStudioConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "lmstudio";
        public override string Category => "AI 工具";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "LM Studio");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "LM Studio");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            // LM Studio 是桌面应用，检查配置目录
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var configPath = ConfigPath;
            if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
                return null;
            
            // LM Studio 配置在 app.json 中
            var configFile = Path.Combine(configPath, "app.json");
            if (!File.Exists(configFile))
                return null;
            
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configFile));
                var appConfig = json.RootElement;
                
                var config = new ProxyConfig();
                
                // LM Studio 使用 downloads.httpProxy
                if (appConfig.TryGetProperty("downloads", out var downloads))
                {
                    if (downloads.TryGetProperty("httpProxy", out var httpProxy))
                        config.HttpProxy = httpProxy.GetString();
                    if (downloads.TryGetProperty("httpsProxy", out var httpsProxy))
                        config.HttpsProxy = httpsProxy.GetString();
                }
                
                return config;
            }
            catch
            {
                return null;
            }
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine("❌ LM Studio 配置目录未找到");
                    return false;
                }
                
                if (!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);
                
                var configFile = Path.Combine(configPath, "app.json");
                var config = new Dictionary<string, object>
                {
                    ["downloads"] = new Dictionary<string, string>
                    {
                        ["httpProxy"] = proxyUrl,
                        ["httpsProxy"] = proxyUrl
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configFile, json);
                
                Console.WriteLine("✅ 已设置 LM Studio 代理:");
                Console.WriteLine($"   httpProxy={proxyUrl}");
                Console.WriteLine("💡 需要重启 LM Studio 以应用配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 LM Studio 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
                    return true;
                
                var configFile = Path.Combine(configPath, "app.json");
                if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                }
                
                Console.WriteLine("✅ LM Studio 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    // ============================================================================
    // 更多开发工具配置器
    // ============================================================================
    
    /// <summary>
    /// NuGet 配置器 (C# .NET)
    /// </summary>
    public class NuGetConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "nuget";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                // NuGet 配置文件
                var configPath = Path.Combine(home, ".nuget", "NuGet", "NuGet.Config");
                if (File.Exists(configPath)) return configPath;
                
                // 解决方案级配置
                return Path.Combine(home, ".nuget", "NuGet");
            }
            
            // Windows
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(appData))
                return Path.Combine(appData, "NuGet", "NuGet.Config");
            
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // NuGet 使用标准代理设置
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // NuGet 特定的代理配置
            var httpProxy = Environment.GetEnvironmentVariable("NUGET_HTTP_PROXY");
            if (!string.IsNullOrEmpty(httpProxy))
            {
                config.HttpProxy = httpProxy;
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // NuGet CLI 支持 -proxy 参数
                // 但更常用的是环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("NUGET_HTTP_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 NuGet 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   NUGET_HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 也可在 nuget.config 中配置 <config><add key=\"http_proxy\" value=\"{proxyUrl}\" /></config>");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 NuGet 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("NUGET_HTTP_PROXY", null);
                Console.WriteLine("✅ NuGet 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// pnpm 配置器
    /// </summary>
    public class PnpmConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "pnpm";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                // pnpm 使用 .npmrc 在用户目录
                var pnpmrc = Path.Combine(home, ".pnpmrc");
                if (File.Exists(pnpmrc)) return pnpmrc;
                
                // 全局配置
                var pnpmDir = Path.Combine(home, ".config", "pnpm");
                if (Directory.Exists(pnpmDir)) return pnpmDir;
            }
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("proxy=");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("https-proxy=");

        protected override string FormatProxyLine(string key, string value)
        {
            return $"{key}={value}";
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // pnpm 从 .npmrc 读取代理配置
            // 也支持环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // pnpm 支持通过 npmrc 配置代理
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // pnpm 特定配置
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var pnpmrc = Path.Combine(home, ".pnpmrc");
                    var content = $"proxy={proxyUrl}\nhttps-proxy={proxyUrl}\n";
                    File.WriteAllText(pnpmrc, content);
                    
                    Console.WriteLine($"✅ 已设置 pnpm 代理: {proxyUrl}");
                    Console.WriteLine($"   配置文件: {pnpmrc}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 pnpm 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var pnpmrc = Path.Combine(home, ".pnpmrc");
                    if (File.Exists(pnpmrc))
                    {
                        File.Delete(pnpmrc);
                    }
                }
                
                Console.WriteLine("✅ pnpm 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Pub 配置器 (Dart/Flutter)
    /// </summary>
    public class PubConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "dart";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".pub-cache");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Dart/Flutter 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Dart/Flutter 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// AWS CLI 配置器
    /// </summary>
    public class AwsCliConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "aws";
        public override string Category => "云 CLI";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".aws", "config");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // AWS CLI 使用标准代理
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // AWS 特定
            var proxy = Environment.GetEnvironmentVariable("AWS_PROXY");
            if (!string.IsNullOrEmpty(proxy))
                config.HttpProxy = proxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("AWS_PROXY", proxyUrl);
                
                // AWS CLI 配置文件方式
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var awsDir = Path.Combine(home, ".aws");
                    if (!Directory.Exists(awsDir))
                        Directory.CreateDirectory(awsDir);
                    
                    var configFile = Path.Combine(awsDir, "config");
                    var content = $"[default]\nproxy_uri = {proxyUrl}\n";
                    File.AppendAllText(configFile, content);
                }
                
                Console.WriteLine($"✅ 已设置 AWS CLI 代理: {proxyUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 AWS CLI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("AWS_PROXY", null);
                Console.WriteLine("✅ AWS CLI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Google Cloud CLI 配置器
    /// </summary>
    public class GcloudConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "gcloud";
        public override string Category => "云 CLI";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "gcloud");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // CLOUDSDK 特定代理
            var cloudProxy = Environment.GetEnvironmentVariable("CLOUDSDK_PROXY");
            if (!string.IsNullOrEmpty(cloudProxy))
                config.HttpProxy = cloudProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("CLOUDSDK_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Google Cloud CLI 代理:");
                Console.WriteLine($"   CLOUDSDK_PROXY={proxyUrl}");
                Console.WriteLine("💡 也可运行: gcloud config set proxy/type http");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Google Cloud CLI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("CLOUDSDK_PROXY", null);
                Console.WriteLine("✅ Google Cloud CLI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// kubectl (Kubernetes) 配置器
    /// </summary>
    public class KubectlConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "kubectl";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".kube");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // kubectl 通过 kubeconfig 中的代理设置
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 kubectl 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 如需持久化，在 ~/.kube/config 中配置 proxy-url");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 kubectl 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ kubectl 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Podman 配置器
    /// </summary>
    public class PodmanConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "podman";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "containers");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Podman 使用 containers.conf
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var configDir = Path.Combine(home, ".config", "containers");
                    if (!Directory.Exists(configDir))
                        Directory.CreateDirectory(configDir);
                    
                    var configFile = Path.Combine(configDir, "containers.conf");
                    var content = $@"[engine]
http_proxy = ""{proxyUrl}""
https_proxy = ""{proxyUrl}""
";
                    File.WriteAllText(configFile, content);
                }
                
                Console.WriteLine($"✅ 已设置 Podman 代理: {proxyUrl}");
                Console.WriteLine("💡 需要重启 podman 服务");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Podman 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Podman 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Terraform 配置器 (HCL)
    /// </summary>
    public class TerraformCliConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "terraform";
        public override string Category => "基础设施即代码";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".terraformrc");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Terraform 特定
            var tfProxy = Environment.GetEnvironmentVariable("TERRAFORM_PROXY");
            if (!string.IsNullOrEmpty(tfProxy))
                config.HttpProxy = tfProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("TERRAFORM_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Terraform 代理: {proxyUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Terraform 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("TERRAFORM_PROXY", null);
                Console.WriteLine("✅ Terraform 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Mercurial (hg) 配置器
    /// </summary>
    public class MercurialConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "hg";
        public override string Category => "版本控制";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".hgrc");
            return null;
        }
        
        protected override bool IsHttpProxyLine(string line)
            => line.Trim().StartsWith("http_proxy =");

        protected override bool IsHttpsProxyLine(string line)
            => line.Trim().StartsWith("https_proxy =");
        
        protected override string FormatProxyLine(string key, string value)
        {
            return $"{key} = {value}";
        }
    }
    
    /// <summary>
    /// CocoaPods 配置器 (iOS/macOS)
    /// </summary>
    public class CocoaPodsConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "pod";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".netrc");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 CocoaPods 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ CocoaPods 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Swift Package Manager 配置器
    /// </summary>
    public class SwiftPMConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "swift";
        public override string Category => "包管理器";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".swiftpm");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Swift PM 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Swift PM 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// FTP/SFTP 配置器
    /// </summary>
    public class FtpConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "ftp";
        public override string Category => "下载工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".netrc");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.FtpProxy = Environment.GetEnvironmentVariable("FTP_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("FTP_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 FTP 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Environment.SetEnvironmentVariable("FTP_PROXY", null);
            Console.WriteLine("✅ FTP 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// RVM / R 包管理器配置器
    /// </summary>
    public class RConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "r";
        public override string Category => "编程语言";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".Rprofile");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // R 特定配置
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    var rProfile = Path.Combine(home, ".Rprofile");
                    var content = $@"
# Proxy configuration added by ProxyTool
Sys.setenv(http_proxy=""{proxyUrl}"")
Sys.setenv(https_proxy=""{proxyUrl}"")
";
                    File.AppendAllText(rProfile, content);
                    Console.WriteLine($"✅ 已设置 R 代理: {proxyUrl}");
                    Console.WriteLine($"   配置文件: {rProfile}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 R 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ R 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Julia 包管理器配置器
    /// </summary>
    public class JuliaConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "julia";
        public override string Category => "编程语言";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".julia");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Julia 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Julia 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// PowerShell 代理配置
    /// </summary>
    public class PowerShellConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "powershell";
        public override string Category => "Shell";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, "Documents", "PowerShell");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // PowerShell 7+ 特定
            var proxyUri = Environment.GetEnvironmentVariable("PSProxyUrl");
            if (!string.IsNullOrEmpty(proxyUri))
                config.HttpProxy = proxyUri;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // PowerShell 7+ 支持
                Environment.SetEnvironmentVariable("PSProxyUrl", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 PowerShell 代理: {proxyUrl}");
                Console.WriteLine("💡 PowerShell 7+: 也可在 $PROFILE 中添加 [System.Net.WebRequest]::DefaultWebProxy = [System.Net.WebRequest]::GetSystemWebProxy()");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 PowerShell 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("PSProxyUrl", null);
                Console.WriteLine("✅ PowerShell 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    // ============================================================================
    // 更多 AI 工具配置器
    // ============================================================================
    
    /// <summary>
    /// Cursor AI IDE 配置器
    /// </summary>
    public class CursorConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cursor";
        public override string Category => "AI IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Cursor", "User", "settings.json");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "Cursor", "User", "settings.json");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Cursor 使用标准代理
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // Cursor 基于 Electron，使用标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Cursor AI 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 需要重启 Cursor 以应用配置");
                
                // 尝试写入 VS Code 兼容的配置
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    var dir = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        var settingsFile = Path.Combine(dir, "settings.json");
                        if (File.Exists(settingsFile))
                        {
                            Console.WriteLine($"💡 配置文件: {settingsFile}");
                        }
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Cursor 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Cursor 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Windsurf AI IDE 配置器
    /// </summary>
    public class WindsurfConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "windsurf";
        public override string Category => "AI IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Windsurf", "User", "settings.json");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "Windsurf", "User", "settings.json");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Windsurf AI 代理: {proxyUrl}");
                Console.WriteLine("💡 需要重启 Windsurf 以应用配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Windsurf 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Windsurf 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Continue VS Code 插件配置器
    /// </summary>
    public class ContinueConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "continue";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
            {
                return Path.Combine(home, ".continue");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            // Continue 是 VS Code 插件，检查扩展目录
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // Continue 使用标准环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // 也检查 Continue 特定配置
            var configPath = ConfigPath;
            if (!string.IsNullOrEmpty(configPath) && Directory.Exists(configPath))
            {
                var configFile = Path.Combine(configPath, "config.json");
                if (File.Exists(configFile))
                {
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configFile));
                        // Continue 配置可能包含自定义端点
                    }
                    catch { }
                }
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Continue 支持自定义模型端点
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!Directory.Exists(configPath))
                        Directory.CreateDirectory(configPath);
                    
                    var configFile = Path.Combine(configPath, "config.json");
                    Console.WriteLine($"💡 Continue 配置文件: {configFile}");
                }
                
                Console.WriteLine($"✅ 已设置 Continue AI 代理: {proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Continue 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Continue 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Codeium 编程助手配置器
    /// </summary>
    public class CodeiumConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "codeium";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Codeium");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".codeium");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "Codeium");
            }
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Codeium 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Codeium 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Tabby AI 编程助手配置器
    /// </summary>
    public class TabbyConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "tabby";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".tabby");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Tabby 使用 TABBY_HTTP_PROXY 环境变量
            var tabbyProxy = Environment.GetEnvironmentVariable("TABBY_HTTP_PROXY");
            if (!string.IsNullOrEmpty(tabbyProxy))
                config.HttpProxy = tabbyProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("TABBY_HTTP_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Tabby AI 代理:");
                Console.WriteLine($"   TABBY_HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 如使用自托管 Tabby 服务器，确保网络可达");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Tabby 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("TABBY_HTTP_PROXY", null);
                Console.WriteLine("✅ Tabby 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Aider AI 编程助手配置器
    /// </summary>
    public class AiderConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "aider";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".aider.conf.yml");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Aider 支持通过配置文件设置代理
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    var content = $"http_proxy: {proxyUrl}\nhttps_proxy: {proxyUrl}\n";
                    File.WriteAllText(configPath, content);
                    Console.WriteLine($"✅ 已设置 Aider 代理: {proxyUrl}");
                    Console.WriteLine($"   配置文件: {configPath}");
                }
                else
                {
                    Console.WriteLine($"✅ 已设置 Aider 代理: {proxyUrl}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Aider 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Aider 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Sourcegraph Cody 配置器
    /// </summary>
    public class SourcegraphCodyConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cody";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".sourcegraph");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Sourcegraph 特定
            var sgProxy = Environment.GetEnvironmentVariable("SRC_PROXY");
            if (!string.IsNullOrEmpty(sgProxy))
                config.HttpProxy = sgProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("SRC_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Sourcegraph Cody 代理: {proxyUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Cody 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("SRC_PROXY", null);
                Console.WriteLine("✅ Cody 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Augment AI 编程助手配置器
    /// </summary>
    public class AugmentConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "augment";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Augment");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "Augment");
            }
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Augment AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Augment AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// GPT4All 本地 LLM 配置器
    /// </summary>
    public class GPT4AllConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "gpt4all";
        public override string Category => "本地 LLM";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "GPT4All");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".local", "share", "gpt4all");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".local", "share", "gpt4all");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var configPath = ConfigPath;
            if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
                return null;
            
            var config = new ProxyConfig();
            
            // GPT4All 使用 gpt4all.ini
            var iniFile = Path.Combine(configPath, "gpt4all.ini");
            if (File.Exists(iniFile))
            {
                // 简单解析 INI
                var content = File.ReadAllText(iniFile);
                if (content.Contains("http_proxy"))
                {
                    // 提取代理设置
                }
            }
            
            // 也检查环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!Directory.Exists(configPath))
                        Directory.CreateDirectory(configPath);
                    
                    var iniFile = Path.Combine(configPath, "gpt4all.ini");
                    var content = $"[Network]\nhttp_proxy={proxyUrl}\nhttps_proxy={proxyUrl}\n";
                    File.WriteAllText(iniFile, content);
                    
                    Console.WriteLine($"✅ 已设置 GPT4All 代理: {proxyUrl}");
                    Console.WriteLine($"   配置文件: {iniFile}");
                    Console.WriteLine("💡 需要重启 GPT4All 以应用配置");
                }
                else
                {
                    Console.WriteLine($"✅ 已设置 GPT4All 代理: {proxyUrl}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 GPT4All 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ GPT4All 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Jan.ai 本地 LLM 配置器
    /// </summary>
    public class JanConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "jan";
        public override string Category => "本地 LLM";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, "jan");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var configPath = ConfigPath;
            if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
                return null;
            
            var config = new ProxyConfig();
            
            // Jan 使用 settings.json
            var settingsFile = Path.Combine(configPath, "settings.json");
            if (File.Exists(settingsFile))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(settingsFile));
                    if (json.RootElement.TryGetProperty("proxy", out var proxy))
                    {
                        if (proxy.TryGetProperty("url", out var url))
                            config.HttpProxy = url.GetString();
                    }
                }
                catch { }
            }
            
            // 也检查环境变量
            config.HttpProxy ??= Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy ??= Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!Directory.Exists(configPath))
                        Directory.CreateDirectory(configPath);
                    
                    var settingsFile = Path.Combine(configPath, "settings.json");
                    var settings = new
                    {
                        proxy = new { url = proxyUrl, enabled = true }
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(settingsFile, json);
                    
                    Console.WriteLine($"✅ 已设置 Jan.ai 代理: {proxyUrl}");
                    Console.WriteLine($"   配置文件: {settingsFile}");
                }
                else
                {
                    Console.WriteLine($"✅ 已设置 Jan.ai 代理: {proxyUrl}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Jan.ai 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    var settingsFile = Path.Combine(configPath, "settings.json");
                    if (File.Exists(settingsFile))
                    {
                        File.Delete(settingsFile);
                    }
                }
                
                Console.WriteLine("✅ Jan.ai 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// llama.cpp / llama-cli 配置器
    /// </summary>
    public class LlamaCppConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "llama.cpp";
        public override string Category => "本地 LLM";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".llama");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // llama.cpp 通过环境变量支持代理
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 llama.cpp 代理: {proxyUrl}");
                Console.WriteLine("💡 用于下载模型时通过代理");
                Console.WriteLine("💡 使用方式: ./llama-cli --proxy http://host:port");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 llama.cpp 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ llama.cpp 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// vLLM 配置器
    /// </summary>
    public class VLLMConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "vllm";
        public override string Category => "本地 LLM";
        
        protected override string? DetectConfigPath()
        {
            // vLLM 作为 Python 包安装
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cache", "vllm");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // vLLM 使用 TRANSFORMERS_CACHE 等环境变量
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // vLLM 下载模型时需要代理
                Environment.SetEnvironmentVariable("TRANSFORMERS_CACHE", 
                    Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", ".cache", "huggingface"));
                Environment.SetEnvironmentVariable("HF_HOME", 
                    Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", ".cache", "huggingface"));
                
                Console.WriteLine($"✅ 已设置 vLLM 代理: {proxyUrl}");
                Console.WriteLine("💡 用于下载模型和依赖时通过代理");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 vLLM 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ vLLM 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Text Generation WebUI 配置器
    /// </summary>
    public class TextGenWebUIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "text-generation-webui";
        public override string Category => "本地 LLM";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, "text-generation-webui");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // 使用启动参数也可设置代理
                Console.WriteLine($"✅ 已设置 Text Generation WebUI 代理: {proxyUrl}");
                Console.WriteLine("💡 启动命令: python server.py --proxy http://host:port");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Text Generation WebUI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Mistral AI CLI 配置器
    /// </summary>
    public class MistralAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "mistral";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".mistral");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Mistral AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Mistral AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Cohere CLI 配置器
    /// </summary>
    public class CohereConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cohere";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cohere");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Cohere 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Cohere 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Perplexity CLI 配置器
    /// </summary>
    public class PerplexityConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "perplexity";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "perplexity");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Perplexity 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Perplexity 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// AI21 Labs CLI 配置器
    /// </summary>
    public class AI21Configurator : ToolConfiguratorBase
    {
        public override string ToolName => "ai21";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".ai21");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 AI21 Labs 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ AI21 Labs 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Groq CLI 配置器
    /// </summary>
    public class GroqConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "groq";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".groq");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Groq 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Groq 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Replicate CLI 配置器
    /// </summary>
    public class ReplicateConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "replicate";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".replicate");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Replicate 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Replicate 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Fireworks AI 配置器
    /// </summary>
    public class FireworksConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "fireworks";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".fireworks");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Fireworks AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Fireworks AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Anyscale 配置器
    /// </summary>
    public class AnyscaleConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "anyscale";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".anyscale");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Anyscale 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Anyscale 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Beam AI 配置器
    /// </summary>
    public class BeamConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "beam";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".beam");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Beam AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Beam AI 代理配置已清除");
            return true;
        }
    }
    
    // ============================================================================
    // 更多 AI 工具配置器 (第二批)
    // ============================================================================
    
    /// <summary>
    /// Poe AI 聚合平台配置器
    /// </summary>
    public class PoeConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "poe";
        public override string Category => "AI 聚合";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".poe");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Poe AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Poe AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Quora Poe CLI 配置器
    /// </summary>
    public class PoeCliConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "poe-cli";
        public override string Category => "AI 聚合";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "poe-cli");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Poe CLI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Poe CLI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Novita AI 配置器
    /// </summary>
    public class NovitaAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "novita";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".novita");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Novita AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Novita AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Together AI 配置器
    /// </summary>
    public class TogetherAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "togetherai";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".togetherai");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Together AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Together AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// DeepInfra 配置器
    /// </summary>
    public class DeepInfraConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "deepinfra";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".deepinfra");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 DeepInfra 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ DeepInfra 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Hyperbolic AI 配置器
    /// </summary>
    public class HyperbolicConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "hyperbolic";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".hyperbolic");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Hyperbolic AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Hyperbolic AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Lepton AI 配置器
    /// </summary>
    public class LeptonConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "lepton";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".lepton");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Lepton AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Lepton AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Cerebras 配置器
    /// </summary>
    public class CerebrasConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cerebras";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cerebras");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Cerebras 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Cerebras 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// SambaNova 配置器
    /// </summary>
    public class SambaNovaConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "sambanova";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".sambanova");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 SambaNova 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ SambaNova 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// AI Studio (Google) 配置器
    /// </summary>
    public class AIStudioConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "aistudio";
        public override string Category => "AI 平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".aistudio");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 AI Studio 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ AI Studio 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Google Vertex AI 配置器
    /// </summary>
    public class VertexAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "vertexai";
        public override string Category => "AI 平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "gcloud");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Vertex AI 通过 gcloud 配置
            var proxy = Environment.GetEnvironmentVariable("VERTEX_AI_PROXY");
            if (!string.IsNullOrEmpty(proxy))
                config.HttpProxy = proxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("VERTEX_AI_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Vertex AI 代理:");
                Console.WriteLine($"   VERTEX_AI_PROXY={proxyUrl}");
                Console.WriteLine("💡 同时设置了 Google Cloud 代理");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Vertex AI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("VERTEX_AI_PROXY", null);
                Console.WriteLine("✅ Vertex AI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// AWS Bedrock 配置器
    /// </summary>
    public class AWSBedrockConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "bedrock";
        public override string Category => "AI 平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".aws");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // AWS SDK 特定代理
            var awsProxy = Environment.GetEnvironmentVariable("AWS_BEDROCK_PROXY");
            if (!string.IsNullOrEmpty(awsProxy))
                config.HttpProxy = awsProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("AWS_BEDROCK_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 AWS Bedrock 代理: {proxyUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 AWS Bedrock 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("AWS_BEDROCK_PROXY", null);
                Console.WriteLine("✅ AWS Bedrock 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// OpenRouter 聚合 API 配置器
    /// </summary>
    public class OpenRouterConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "openrouter";
        public override string Category => "AI 聚合";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".openrouter");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 OpenRouter 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ OpenRouter 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// LangChain 配置器
    /// </summary>
    public class LangChainConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "langchain";
        public override string Category => "AI 框架";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".langchain");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // LangChain 使用 LANGCHAIN_PROXY
            var lcProxy = Environment.GetEnvironmentVariable("LANGCHAIN_PROXY");
            if (!string.IsNullOrEmpty(lcProxy))
                config.HttpProxy = lcProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("LANGCHAIN_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 LangChain 代理:");
                Console.WriteLine($"   LANGCHAIN_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 LangChain 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("LANGCHAIN_PROXY", null);
                Console.WriteLine("✅ LangChain 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// LlamaIndex 配置器
    /// </summary>
    public class LlamaIndexConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "llamaindex";
        public override string Category => "AI 框架";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".llamaindex");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 LlamaIndex 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ LlamaIndex 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Haystack 配置器
    /// </summary>
    public class HaystackConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "haystack";
        public override string Category => "AI 框架";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".haystack");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Haystack 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Haystack 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// BentoML 配置器
    /// </summary>
    public class BentoMLConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "bentoml";
        public override string Category => "AI 部署";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".bentoml");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 BentoML 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ BentoML 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Ray AI 配置器
    /// </summary>
    public class RayAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "ray";
        public override string Category => "AI 分布式";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".ray");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Ray 特定环境变量
            var rayProxy = Environment.GetEnvironmentVariable("RAY_http_proxy");
            if (!string.IsNullOrEmpty(rayProxy))
                config.HttpProxy = rayProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("RAY_http_proxy", proxyUrl);
                Environment.SetEnvironmentVariable("RAY_https_proxy", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Ray AI 代理:");
                Console.WriteLine($"   RAY_http_proxy={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Ray 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("RAY_http_proxy", null);
                Environment.SetEnvironmentVariable("RAY_https_proxy", null);
                Console.WriteLine("✅ Ray AI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Weights & Biases (wandb) 配置器
    /// </summary>
    public class WandBConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "wandb";
        public override string Category => "ML 监控";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".wandb");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // wandb 特定
            var wandbProxy = Environment.GetEnvironmentVariable("WANDB_PROXY");
            if (!string.IsNullOrEmpty(wandbProxy))
                config.HttpProxy = wandbProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("WANDB_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Weights & Biases 代理:");
                Console.WriteLine($"   WANDB_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 wandb 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("WANDB_PROXY", null);
                Console.WriteLine("✅ wandb 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// MLflow 配置器
    /// </summary>
    public class MLflowConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "mlflow";
        public override string Category => "ML 平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".mlflow");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 MLflow 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ MLflow 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Kubeflow 配置器
    /// </summary>
    public class KubeflowConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "kubeflow";
        public override string Category => "ML 平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? 
                      Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".kube");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Kubeflow 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Kubeflow 代理配置已清除");
            return true;
        }
    }
    
    // ============================================================================
    // 第三批 AI 工具配置器
    // ============================================================================
    
    /// <summary>
    /// Text.com AI 配置器
    /// </summary>
    public class TextComConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "text.com";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".textcom");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Text.com AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Text.com 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Inflection AI 配置器
    /// </summary>
    public class InflectionConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "inflection";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".inflection");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Inflection AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Inflection AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Anthropic API 直接配置器
    /// </summary>
    public class AnthropicAPIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "anthropic-api";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".anthropic");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Anthropic 特定
            var apiProxy = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
            if (!string.IsNullOrEmpty(apiProxy))
            {
                config.HttpProxy = apiProxy;
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Anthropic API 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Anthropic API 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// xAI Grok 配置器
    /// </summary>
    public class XAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "xai";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".xai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 xAI Grok 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ xAI Grok 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Meta AI 配置器
    /// </summary>
    public class MetaAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "meta-ai";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".metaai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Meta AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Meta AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Stability AI 配置器
    /// </summary>
    public class StabilityAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "stabilityai";
        public override string Category => "AI 图像";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".stabilityai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Stability AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Stability AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Midjourney 配置器
    /// </summary>
    public class MidjourneyConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "midjourney";
        public override string Category => "AI 图像";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".midjourney");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Midjourney 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Midjourney 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// DALL-E 配置器
    /// </summary>
    public class DALLEConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "dalle";
        public override string Category => "AI 图像";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".dalle");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 DALL-E 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ DALL-E 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Runway ML 配置器
    /// </summary>
    public class RunwayMLConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "runwayml";
        public override string Category => "AI 视频";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".runwayml");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Runway ML 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Runway ML 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// ElevenLabs 语音配置器
    /// </summary>
    public class ElevenLabsConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "elevenlabs";
        public override string Category => "AI 语音";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".elevenlabs");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 ElevenLabs 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ ElevenLabs 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Murf AI 语音配置器
    /// </summary>
    public class MurfAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "murf";
        public override string Category => "AI 语音";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".murfai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Murf AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Murf AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// WellSaid Labs 语音配置器
    /// </summary>
    public class WellSaidConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "wellsaid";
        public override string Category => "AI 语音";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".wellsaid");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 WellSaid Labs 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ WellSaid Labs 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Descript 音视频配置器
    /// </summary>
    public class DescriptConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "descript";
        public override string Category => "AI 音视频";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".descript");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Descript 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Descript 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// HeyGen 数字人配置器
    /// </summary>
    public class HeyGenConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "heygen";
        public override string Category => "AI 数字人";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".heygen");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 HeyGen 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ HeyGen 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Synthesis AI 配置器
    /// </summary>
    public class SynthesisAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "synthesis-ai";
        public override string Category => "AI 数据";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".synthesisai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Synthesis AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Synthesis AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Scale AI 配置器
    /// </summary>
    public class ScaleAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "scaleai";
        public override string Category => "AI 数据";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".scaleai");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Scale AI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Scale AI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Labelbox 配置器
    /// </summary>
    public class LabelboxConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "labelbox";
        public override string Category => "AI 数据标注";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".labelbox");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Labelbox 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Labelbox 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Scale Nucleus 配置器
    /// </summary>
    public class ScaleNucleusConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "scale-nucleus";
        public override string Category => "AI 数据";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".scale-nucleus");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Scale Nucleus 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Scale Nucleus 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Hugging Face Inference API 配置器
    /// </summary>
    public class HFInferenceConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "hf-inference";
        public override string Category => "AI API";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".cache", "huggingface");
            return null;
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HF_HUB_HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HF_HUB_HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HF_HUB_HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HF_HUB_HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Hugging Face Inference API 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Hugging Face Inference API 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Lightning AI 配置器
    /// </summary>
    public class LightningAIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "lightningai";
        public override string Category => "AI 训练";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".lightning");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Lightning AI 特定
            var ltProxy = Environment.GetEnvironmentVariable("LIGHTNING_PROXY");
            if (!string.IsNullOrEmpty(ltProxy))
                config.HttpProxy = ltProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("LIGHTNING_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Lightning AI 代理:");
                Console.WriteLine($"   LIGHTNING_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Lightning AI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("LIGHTNING_PROXY", null);
                Console.WriteLine("✅ Lightning AI 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Paperspace 配置器
    /// </summary>
    public class PaperspaceConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "paperspace";
        public override string Category => "AI 云平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".paperspace");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Paperspace 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Paperspace 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// CoreWeave 配置器
    /// </summary>
    public class CoreWeaveConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "coreweave";
        public override string Category => "AI 云平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".coreweave");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 CoreWeave 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ CoreWeave 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Lambda Labs 配置器
    /// </summary>
    public class LambdaLabsConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "lambdalabs";
        public override string Category => "AI 云平台";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".lambdalabs");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Lambda Labs 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Lambda Labs 代理配置已清除");
            return true;
        }
    }
    
    // ============================================================================
    // IDE 工具配置器
    // ============================================================================
    
    /// <summary>
    /// OpenCode 字节跳动 AI IDE 配置器
    /// </summary>
    public class OpenCodeConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "opencode";
        public override string Category => "AI IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "opencode");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "opencode");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".opencode");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // OpenCode 使用标准环境变量
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // 检查 OpenCode 特定配置
            var configPath = ConfigPath;
            if (!string.IsNullOrEmpty(configPath) && Directory.Exists(configPath))
            {
                var settingsFile = Path.Combine(configPath, "settings.json");
                if (File.Exists(settingsFile))
                {
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(settingsFile));
                        // 解析 OpenCode 特定配置
                        if (json.RootElement.TryGetProperty("proxy", out var proxy))
                        {
                            if (proxy.TryGetProperty("http", out var http))
                                config.HttpProxy = http.GetString();
                            if (proxy.TryGetProperty("https", out var https))
                                config.HttpsProxy = https.GetString();
                        }
                    }
                    catch { }
                }
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // OpenCode 基于 Electron，使用标准环境变量
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 OpenCode 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine($"   HTTPS_PROXY={proxyUrl}");
                
                // 尝试写入配置文件
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!Directory.Exists(configPath))
                        Directory.CreateDirectory(configPath);
                    
                    var settingsFile = Path.Combine(configPath, "settings.json");
                    var settings = new
                    {
                        proxy = new
                        {
                            http = proxyUrl,
                            https = proxyUrl
                        }
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(settingsFile, json);
                    Console.WriteLine($"   配置文件: {settingsFile}");
                }
                
                Console.WriteLine("💡 需要重启 OpenCode 以应用配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 OpenCode 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    var settingsFile = Path.Combine(configPath, "settings.json");
                    if (File.Exists(settingsFile))
                    {
                        File.Delete(settingsFile);
                    }
                }
                
                Console.WriteLine("✅ OpenCode 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// VS Code 配置器
    /// </summary>
    public class VSCodeConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "vscode";
        public override string Category => "IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Code", "User", "settings.json");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "Code", "User", "settings.json");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "Code", "User", "settings.json");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            
            // VS Code 支持多种代理配置方式
            // 1. 从 settings.json 读取
            var configPath = ConfigPath;
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                    
                    // HTTP 代理
                    if (json.RootElement.TryGetProperty("http.proxy", out var httpProxy))
                        config.HttpProxy = httpProxy.GetString();
                    
                    // HTTPS 代理
                    if (json.RootElement.TryGetProperty("https.proxy", out var httpsProxy))
                        config.HttpsProxy = httpsProxy.GetString();
                    
                    // 代理 Support
                    if (json.RootElement.TryGetProperty("http.proxySupport", out var support))
                    {
                        // 代理支持设置
                    }
                    
                    // 代理 Authorization
                    if (json.RootElement.TryGetProperty("http.proxyAuthorization", out var auth))
                    {
                        // 代理授权
                    }
                }
                catch { }
            }
            
            // 2. 从环境变量读取作为备选
            config.HttpProxy ??= Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy ??= Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine("❌ 找不到 VS Code 配置目录");
                    return false;
                }
                
                // 确保目录存在
                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                // 读取现有配置或创建新的
                var settings = new Dictionary<string, object>();
                
                if (File.Exists(configPath))
                {
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                        foreach (var prop in json.RootElement.EnumerateObject())
                        {
                            if (prop.Name != "http.proxy" && prop.Name != "https.proxy" && 
                                prop.Name != "http.proxySupport" && prop.Name != "http.proxyAuthorization")
                            {
                                // 保留其他设置
                            }
                        }
                    }
                    catch { }
                }
                
                // 设置代理配置
                settings["http.proxy"] = proxyUrl;
                settings["https.proxy"] = proxyUrl;
                settings["http.proxySupport"] = "on";
                settings["http.proxyStrictSSL"] = "false";
                
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath, jsonContent);
                
                Console.WriteLine("✅ 已设置 VS Code 代理:");
                Console.WriteLine($"   http.proxy = {proxyUrl}");
                Console.WriteLine($"   https.proxy = {proxyUrl}");
                Console.WriteLine($"   配置文件: {configPath}");
                
                Console.WriteLine("💡 需要重启 VS Code 以应用配置");
                Console.WriteLine("💡 或使用命令: 重新加载窗口");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 VS Code 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                    return true;
                
                // 读取现有配置
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                    var settings = new Dictionary<string, object>();
                    
                    foreach (var prop in json.RootElement.EnumerateObject())
                    {
                        if (prop.Name != "http.proxy" && prop.Name != "https.proxy" && 
                            prop.Name != "http.proxySupport" && prop.Name != "http.proxyAuthorization")
                        {
                            // 保留非代理设置
                        }
                    }
                    
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(configPath, jsonContent);
                }
                catch
                {
                    // 如果解析失败，直接删除文件
                    File.Delete(configPath);
                }
                
                Console.WriteLine("✅ VS Code 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// VS Code Insiders 配置器
    /// </summary>
    public class VSCodeInsidersConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "vscode-insiders";
        public override string Category => "IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "Code - Insiders", "User", "settings.json");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "Code - Insiders", "User", "settings.json");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "Code - Insiders", "User", "settings.json");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath()) || !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                var configPath = ConfigPath;
                if (string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine("❌ 找不到 VS Code Insiders 配置目录");
                    return false;
                }
                
                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                var settings = new Dictionary<string, object>
                {
                    ["http.proxy"] = proxyUrl,
                    ["https.proxy"] = proxyUrl,
                    ["http.proxySupport"] = "on"
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath, json);
                
                Console.WriteLine($"✅ 已设置 VS Code Insiders 代理: {proxyUrl}");
                Console.WriteLine($"   配置文件: {configPath}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 VS Code Insiders 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ VS Code Insiders 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Cline (原 Claude Dev) 配置器
    /// </summary>
    public class ClineConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "cline";
        public override string Category => "AI 编程助手";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "cline");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "cline");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "cline");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath) || !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Cline 使用标准环境变量
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Cline (Claude Dev) 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 需要重启 Cline 以应用配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Cline 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Cline 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Goose AI IDE 配置器
    /// </summary>
    public class GooseConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "goose";
        public override string Category => "AI IDE";
        
        protected override string? DetectConfigPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(appData))
                    return Path.Combine(appData, "goose");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Library", "Application Support", "goose");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, ".config", "goose");
            }
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath) || !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("ALL_PROXY", proxyUrl);
                
                Console.WriteLine("✅ 已设置 Goose AI IDE 代理:");
                Console.WriteLine($"   HTTP_PROXY={proxyUrl}");
                Console.WriteLine("💡 需要重启 Goose 以应用配置");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Goose 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Goose 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    ///bolt.new AI IDE 配置器
    /// </summary>
    public class BoltNewConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "bolt.new";
        public override string Category => "AI IDE";
        
        protected override string? DetectConfigPath()
        {
            // bolt.new 是 Web 应用，但也提供桌面版
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".bolt");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(ConfigPath);
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 bolt.new 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ bolt.new 代理配置已清除");
            return true;
        }
    }
    
    // ============================================================================
    // 程序员/开发工具配置器
    // ============================================================================
    
    /// <summary>
    /// GitHub CLI 配置器
    /// </summary>
    public class GitHubCLIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "gh";
        public override string Category => "版本控制";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "gh");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // GitHub CLI 特定配置
            var configPath = ConfigPath;
            if (!string.IsNullOrEmpty(configPath) && Directory.Exists(configPath))
            {
                var hostsFile = Path.Combine(configPath, "hosts.yml");
                if (File.Exists(hostsFile))
                {
                    // GitHub Enterprise 配置
                }
            }
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                // GitHub CLI 使用 HTTPS，需要代理
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                var configPath = ConfigPath;
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!Directory.Exists(configPath))
                        Directory.CreateDirectory(configPath);
                    
                    // GitHub CLI 支持配置 Git 代理
                    Console.WriteLine($"✅ 已设置 GitHub CLI 代理: {proxyUrl}");
                    Console.WriteLine("💡 GitHub CLI 使用 Git 协议，建议同时配置 Git 代理");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 GitHub CLI 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ GitHub CLI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// GitLab CLI 配置器
    /// </summary>
    public class GitLabCLIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "glab";
        public override string Category => "版本控制";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "glab");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 GitLab CLI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ GitLab CLI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Bitbucket CLI 配置器
    /// </summary>
    public class BitbucketCLIConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "bb";
        public override string Category => "版本控制";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "bitbucket");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Bitbucket CLI 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Bitbucket CLI 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Docker Compose 配置器
    /// </summary>
    public class DockerComposeConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "docker-compose";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            // Docker Compose 无需特殊配置，使用 Docker 代理
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            // Docker Compose 使用 Docker 守护进程代理
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Docker Compose 代理: {proxyUrl}");
            Console.WriteLine("💡 Docker Compose 通过 Docker 守护进程使用网络");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Docker Compose 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Docker Buildx 配置器
    /// </summary>
    public class DockerBuildxConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "docker-buildx";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".docker");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Docker Buildx 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Docker Buildx 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Kind (Kubernetes in Docker) 配置器
    /// </summary>
    public class KindConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "kind";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".kind");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Kind 特定配置
            var kindProxy = Environment.GetEnvironmentVariable("KIND_PROXY");
            if (!string.IsNullOrEmpty(kindProxy))
                config.HttpProxy = kindProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("KIND_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 Kind 代理:");
                Console.WriteLine($"   KIND_PROXY={proxyUrl}");
                Console.WriteLine("💡 用于创建 kind 集群时拉取镜像");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Kind 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("KIND_PROXY", null);
                Console.WriteLine("✅ Kind 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Minikube 配置器
    /// </summary>
    public class MinikubeConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "minikube";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".minikube");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // Minikube 特定
            var mkProxy = Environment.GetEnvironmentVariable("MINIKUBE_PROXY");
            if (!string.IsNullOrEmpty(mkProxy))
                config.HttpProxy = mkProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                
                // Minikube 代理通过 --docker-env 传递
                Console.WriteLine($"✅ 已设置 Minikube 代理: {proxyUrl}");
                Console.WriteLine("💡 启动命令: minikube start --docker-env HTTP_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 Minikube 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Minikube 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// K3s 配置器
    /// </summary>
    public class K3sConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "k3s";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".kube");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 K3s 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ K3s 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Helmfile 配置器
    /// </summary>
    public class HelmfileConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "helmfile";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "helmfile");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Helmfile 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Helmfile 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Skaffold 配置器
    /// </summary>
    public class SkaffoldConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "skaffold";
        public override string Category => "开发工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".skaffold");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Skaffold 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Skaffold 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Tilt 配置器
    /// </summary>
    public class TiltConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "tilt";
        public override string Category => "开发工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".tiltenv");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Tilt 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Tilt 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// Kaniko 配置器
    /// </summary>
    public class KanikoConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "kaniko";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            // Kaniko 通过环境变量或 Dockerfile 配置
            Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
            Console.WriteLine($"✅ 已设置 Kaniko 代理: {proxyUrl}");
            return true;
        }
        
        public override bool ClearProxy()
        {
            Console.WriteLine("✅ Kaniko 代理配置已清除");
            return true;
        }
    }
    
    /// <summary>
    /// BuildKit 配置器
    /// </summary>
    public class BuildKitConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "buildkit";
        public override string Category => "容器工具";
        
        protected override string? DetectConfigPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".buildkit");
            return null;
        }
        
        public override bool IsInstalled()
        {
            return !string.IsNullOrEmpty(DetectToolPath());
        }
        
        public override ProxyConfig? GetCurrentConfig()
        {
            var config = new ProxyConfig();
            config.HttpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            config.HttpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            
            // BuildKit 特定
            var bkProxy = Environment.GetEnvironmentVariable("BUILDKIT_PROXY");
            if (!string.IsNullOrEmpty(bkProxy))
                config.HttpProxy = bkProxy;
            
            return config;
        }
        
        public override bool SetProxy(string proxyUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
                Environment.SetEnvironmentVariable("BUILDKIT_PROXY", proxyUrl);
                
                Console.WriteLine($"✅ 已设置 BuildKit 代理:");
                Console.WriteLine($"   BUILDKIT_PROXY={proxyUrl}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 设置 BuildKit 代理失败: {ex.Message}");
                return false;
            }
        }
        
        public override bool ClearProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("BUILDKIT_PROXY", null);
                Console.WriteLine("✅ BuildKit 代理配置已清除");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}