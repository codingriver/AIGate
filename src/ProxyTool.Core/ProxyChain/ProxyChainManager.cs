using System;
using System.Collections.Generic;
using System.Linq;
using ProxyTool.Models;
using ProxyTool.Managers;

namespace ProxyTool.ProxyChain
{
    /// <summary>
    /// 代理链管理器
    /// </summary>
    public class ProxyChainManager
    {
        /// <summary>
        /// 代理链配置
        /// </summary>
        public class ProxyChain
        {
            public string Name { get; set; } = "";
            public List<ProxyNode> Nodes { get; set; } = new();
            public bool Enabled { get; set; } = true;
        }
        
        /// <summary>
        /// 代理节点
        /// </summary>
        public class ProxyNode
        {
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
            public ProxyType Type { get; set; } = ProxyType.HTTP;
            public int Order { get; set; }
            public int TimeoutSeconds { get; set; } = 10;
        }
        
        /// <summary>
        /// 代理类型
        /// </summary>
        public enum ProxyType
        {
            HTTP,
            HTTPS,
            SOCKS4,
            SOCKS5
        }
        
        /// <summary>
        /// 链式代理结果
        /// </summary>
        public class ChainResult
        {
            public bool Success { get; set; }
            public string FinalProxyUrl { get; set; } = "";
            public int TotalLatencyMs { get; set; }
            public List<NodeTestResult> NodeResults { get; set; } = new();
            public string? ErrorMessage { get; set; }
        }
        
        /// <summary>
        /// 单节点测试结果
        /// </summary>
        public class NodeTestResult
        {
            public string NodeName { get; set; } = "";
            public bool Success { get; set; }
            public int LatencyMs { get; set; }
            public string? Error { get; set; }
        }
        
        /// <summary>
        /// 测试代理链
        /// </summary>
        public static async System.Threading.Tasks.Task<ChainResult> TestChainAsync(ProxyChain chain, string testUrl = "https://www.google.com/generate_204")
        {
            var result = new ChainResult
            {
                NodeResults = new List<NodeTestResult>()
            };
            
            var currentLatency = 0;
            var currentProxy = "";
            
            foreach (var node in chain.Nodes.OrderBy(n => n.Order))
            {
                var nodeResult = new NodeTestResult
                {
                    NodeName = node.Name
                };
                
                try
                {
                    // 测试当前节点
                    var testProxy = GetProxyUrl(node);
                    var testResult = await ProxyTester.TestProxyAsync(testProxy, testUrl);
                    
                    nodeResult.Success = testResult.Success;
                    nodeResult.LatencyMs = testResult.ResponseTimeMs;
                    
                    if (testResult.Success)
                    {
                        currentLatency += testResult.ResponseTimeMs;
                        currentProxy = testProxy;
                    }
                    else
                    {
                        nodeResult.Error = testResult.ErrorMessage;
                        result.Success = false;
                        result.ErrorMessage = $"节点 {node.Name} 测试失败: {testResult.ErrorMessage}";
                        break;
                    }
                }
                catch (Exception ex)
                {
                    nodeResult.Success = false;
                    nodeResult.Error = ex.Message;
                    result.Success = false;
                    result.ErrorMessage = $"节点 {node.Name} 异常: {ex.Message}";
                    break;
                }
                
                result.NodeResults.Add(nodeResult);
            }
            
            result.TotalLatencyMs = currentLatency;
            result.Success = chain.Nodes.All(n => result.NodeResults.Any(r => r.NodeName == n.Name && r.Success));
            result.FinalProxyUrl = currentProxy;
            
            return result;
        }
        
        /// <summary>
        /// 组合代理链为单一代理 URL
        /// </summary>
        public static string? CombineChain(ProxyChain chain)
        {
            if (!chain.Enabled || chain.Nodes.Count == 0)
                return null;
            
            // 按顺序组合代理
            // 注意：HTTP 代理不支持链式，需要使用 SOCKS 链
            var firstNode = chain.Nodes.OrderBy(n => n.Order).First();
            return GetProxyUrl(firstNode);
        }
        
        /// <summary>
        /// 创建简单的两节点代理链
        /// </summary>
        public static ProxyChain CreateSimpleChain(string name, string localProxy, string upstreamProxy)
        {
            return new ProxyChain
            {
                Name = name,
                Nodes = new List<ProxyNode>
                {
                    new ProxyNode
                    {
                        Name = "本地代理",
                        Url = localProxy,
                        Type = DetermineProxyType(localProxy),
                        Order = 1
                    },
                    new ProxyNode
                    {
                        Name = "上游代理",
                        Url = upstreamProxy,
                        Type = DetermineProxyType(upstreamProxy),
                        Order = 2
                    }
                }
            };
        }
        
        /// <summary>
        /// 从 URL 确定代理类型
        /// </summary>
        private static ProxyType DetermineProxyType(string url)
        {
            if (url.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
                return ProxyType.SOCKS5;
            if (url.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase))
                return ProxyType.SOCKS4;
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return ProxyType.HTTPS;
            return ProxyType.HTTP;
        }
        
        /// <summary>
        /// 获取代理 URL
        /// </summary>
        private static string GetProxyUrl(ProxyNode node)
        {
            return node.Url;
        }
    }
    
    /// <summary>
    /// 代理池管理器
    /// </summary>
    public class ProxyPoolManager
    {
        private readonly List<PooledProxy> _proxies = new();
        private int _currentIndex = 0;
        
        /// <summary>
        /// 池中代理
        /// </summary>
        public class PooledProxy
        {
            public string Url { get; set; } = "";
            public string Name { get; set; } = "";
            public bool IsHealthy { get; set; } = true;
            public int FailCount { get; set; }
            public DateTime LastTested { get; set; }
            public int AvgLatencyMs { get; set; }
        }
        
        /// <summary>
        /// 添加代理到池
        /// </summary>
        public void AddProxy(string url, string? name = null)
        {
            _proxies.Add(new PooledProxy
            {
                Url = url,
                Name = name ?? url,
                LastTested = DateTime.Now
            });
        }
        
        /// <summary>
        /// 获取下一个可用代理（轮询）
        /// </summary>
        public PooledProxy? GetNext()
        {
            var healthy = _proxies.Where(p => p.IsHealthy).ToList();
            if (healthy.Count == 0)
                return null;
            
            var proxy = healthy[_currentIndex % healthy.Count];
            _currentIndex++;
            return proxy;
        }
        
        /// <summary>
        /// 获取最低延迟代理
        /// </summary>
        public PooledProxy? GetFastest()
        {
            var healthy = _proxies.Where(p => p.IsHealthy && p.AvgLatencyMs > 0).ToList();
            return healthy.OrderBy(p => p.AvgLatencyMs).FirstOrDefault();
        }
        
        /// <summary>
        /// 标记代理失败
        /// </summary>
        public void MarkFailed(PooledProxy proxy)
        {
            proxy.FailCount++;
            if (proxy.FailCount >= 3)
            {
                proxy.IsHealthy = false;
            }
        }
        
        /// <summary>
        /// 标记代理成功
        /// </summary>
        public void MarkSuccess(PooledProxy proxy, int latencyMs)
        {
            proxy.FailCount = 0;
            proxy.IsHealthy = true;
            proxy.AvgLatencyMs = (proxy.AvgLatencyMs + latencyMs) / 2;
            proxy.LastTested = DateTime.Now;
        }
        
        /// <summary>
        /// 获取池状态
        /// </summary>
        public PoolStatus GetStatus()
        {
            return new PoolStatus
            {
                Total = _proxies.Count,
                Healthy = _proxies.Count(p => p.IsHealthy),
                Unhealthy = _proxies.Count(p => !p.IsHealthy)
            };
        }
        
        public class PoolStatus
        {
            public int Total { get; set; }
            public int Healthy { get; set; }
            public int Unhealthy { get; set; }
        }
    }
}