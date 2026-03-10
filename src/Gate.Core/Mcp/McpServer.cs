using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gate.Managers;
using Gate.Models;

namespace Gate.Mcp
{
    /// <summary>
    /// MCP 工具定义
    /// </summary>
    public class McpTool
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, McpParameter> Parameters { get; set; } = new();
    }
    
    /// <summary>
    /// MCP 参数定义
    /// </summary>
    public class McpParameter
    {
        public string Type { get; set; } = "string";
        public string Description { get; set; } = "";
        public bool Required { get; set; } = true;
    }
    
    /// <summary>
    /// MCP 服务器（简化版）
    /// </summary>
    public static class McpServer
    {
        /// <summary>
        /// 获取可用的 MCP 工具列表
        /// </summary>
        public static List<McpTool> GetTools()
        {
            return new List<McpTool>
            {
                new McpTool
                {
                    Name = "set_proxy",
                    Description = "设置代理配置",
                    Parameters = new Dictionary<string, McpParameter>
                    {
                        ["http_proxy"] = new McpParameter { Type = "string", Description = "HTTP代理地址", Required = false },
                        ["https_proxy"] = new McpParameter { Type = "string", Description = "HTTPS代理地址", Required = false },
                        ["tools"] = new McpParameter { Type = "array", Description = "要配置的工具列表", Required = false }
                    }
                },
                new McpTool
                {
                    Name = "test_proxy",
                    Description = "测试代理连通性",
                    Parameters = new Dictionary<string, McpParameter>
                    {
                        ["proxy_url"] = new McpParameter { Type = "string", Description = "代理地址", Required = true }
                    }
                },
                new McpTool
                {
                    Name = "get_current_config",
                    Description = "获取当前代理配置",
                    Parameters = new Dictionary<string, McpParameter>()
                },
                new McpTool
                {
                    Name = "clear_proxy",
                    Description = "清除代理配置",
                    Parameters = new Dictionary<string, McpParameter>
                    {
                        ["tools"] = new McpParameter { Type = "array", Description = "要清除的工具列表", Required = false }
                    }
                }
            };
        }
        
        /// <summary>
        /// 执行 MCP 工具
        /// </summary>
        public static async Task<object> InvokeTool(string name, Dictionary<string, object> args)
        {
            return name switch
            {
                "set_proxy" => await InvokeSetProxy(args),
                "test_proxy" => await InvokeTestProxy(args),
                "get_current_config" => InvokeGetCurrentConfig(),
                "clear_proxy" => await InvokeClearProxy(args),
                _ => new { success = false, error = $"未知工具: {name}" }
            };
        }
        
        private static async Task<object> InvokeSetProxy(Dictionary<string, object> args)
        {
            var httpProxy = args.ContainsKey("http_proxy") ? args["http_proxy"]?.ToString() : null;
            var httpsProxy = args.ContainsKey("https_proxy") ? args["https_proxy"]?.ToString() : null;
            var tools = args.ContainsKey("tools") ? args["tools"] as List<object> : null;
            
            var config = new ProxyConfig
            {
                HttpProxy = httpProxy,
                HttpsProxy = httpsProxy ?? httpProxy
            };
            
            // 设置环境变量
            EnvVarManager.SetProxyForCurrentProcess(config);
            
            // 设置工具代理
            if (tools != null)
            {
                foreach (var toolName in tools)
                {
                    var tool = ToolRegistry.GetByName(toolName.ToString());
                    if (tool != null)
                    {
                        await Task.Run(() => tool.SetProxy(httpProxy));
                    }
                }
            }
            
            return new { success = true, message = "代理已设置" };
        }
        
        private static async Task<object> InvokeTestProxy(Dictionary<string, object> args)
        {
            var proxyUrl = args["proxy_url"].ToString();
            var result = await ProxyTester.TestProxyAsync(proxyUrl);
            
            return new
            {
                success = result.Success,
                response_time_ms = result.ResponseTimeMs,
                error = result.ErrorMessage
            };
        }
        
        private static object InvokeGetCurrentConfig()
        {
            var envConfig = EnvVarManager.GetProxyConfig(EnvLevel.User);
            
            var toolConfigs = new Dictionary<string, object>();
            foreach (var tool in ToolRegistry.GetAllTools())
            {
                var config = tool.GetCurrentConfig();
                if (config != null)
                {
                    toolConfigs[tool.ToolName] = new
                    {
                        http_proxy = config.HttpProxy,
                        https_proxy = config.HttpsProxy
                    };
                }
            }
            
            return new
            {
                success = true,
                env_vars = new
                {
                    http_proxy = envConfig.HttpProxy,
                    https_proxy = envConfig.HttpsProxy
                },
                tools = toolConfigs
            };
        }
        
        private static async Task<object> InvokeClearProxy(Dictionary<string, object> args)
        {
            var tools = args.ContainsKey("tools") ? args["tools"] as List<object> : null;
            
            // 清除环境变量
            EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig());
            
            // 清除工具代理
            if (tools != null)
            {
                foreach (var toolName in tools)
                {
                    var tool = ToolRegistry.GetByName(toolName.ToString());
                    if (tool != null)
                    {
                        await Task.Run(() => tool.ClearProxy());
                    }
                }
            }
            
            return new { success = true, message = "代理已清除" };
        }
        
        /// <summary>
        /// 输出 MCP 配置
        /// </summary>
        public static void PrintMcpConfig()
        {
            var config = new
            {
                name = "proxy-tool",
                version = "1.0.0",
                tools = GetTools()
            };
            
            Console.WriteLine(JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
