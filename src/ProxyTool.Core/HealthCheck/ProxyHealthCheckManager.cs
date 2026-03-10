using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProxyTool.Models;
using ProxyTool.Managers;

namespace ProxyTool.HealthCheck
{
    /// <summary>
    /// 代理健康检查与自动切换管理器
    /// </summary>
    public class ProxyHealthCheckManager : IDisposable
    {
        private readonly Dictionary<string, ProxyHealthStatus> _proxyStatus = new();
        private Timer? _healthCheckTimer;
        private readonly object _lock = new();
        
        /// <summary>
        /// 代理健康状态
        /// </summary>
        public class ProxyHealthStatus
        {
            public string ProxyUrl { get; set; } = "";
            public bool IsHealthy { get; set; }
            public int FailCount { get; set; }
            public int SuccessCount { get; set; }
            public int AvgLatencyMs { get; set; }
            public DateTime LastChecked { get; set; }
            public DateTime LastSuccess { get; set; }
            public DateTime LastFailure { get; set; }
            public string? LastError { get; set; }
            public int TotalChecks { get; set; }
        }
        
        /// <summary>
        /// 健康检查配置
        /// </summary>
        public class HealthCheckConfig
        {
            public int IntervalSeconds { get; set; } = 60;
            public int TimeoutSeconds { get; set; } = 10;
            public int MaxFailures { get; set; } = 3;
            public string TestUrl { get; set; } = "https://www.google.com/generate_204";
            public int MaxLatencyMs { get; set; } = 5000;
        }
        
        private HealthCheckConfig _config = new();
        
        /// <summary>
        /// 健康检查事件
        /// </summary>
        public event EventHandler<ProxyHealthEventArgs>? OnHealthChanged;
        public event EventHandler<ProxyHealthEventArgs>? OnProxyFailed;
        
        /// <summary>
        /// 配置健康检查
        /// </summary>
        public void Configure(HealthCheckConfig config)
        {
            _config = config;
        }
        
        /// <summary>
        /// 启动健康检查
        /// </summary>
        public void Start(IEnumerable<string> proxyUrls)
        {
            Stop();
            
            // 初始化代理状态
            foreach (var url in proxyUrls)
            {
                if (!_proxyStatus.ContainsKey(url))
                {
                    _proxyStatus[url] = new ProxyHealthStatus
                    {
                        ProxyUrl = url,
                        IsHealthy = true,
                        LastChecked = DateTime.Now
                    };
                }
            }
            
            // 启动定时检查
            _healthCheckTimer = new Timer(
                async _ => await CheckAllProxiesAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_config.IntervalSeconds));
        }
        
        /// <summary>
        /// 停止健康检查
        /// </summary>
        public void Stop()
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
        }
        
        /// <summary>
        /// 检查所有代理
        /// </summary>
        private async Task CheckAllProxiesAsync()
        {
            var proxies = _proxyStatus.Keys.ToList();
            
            foreach (var url in proxies)
            {
                await CheckProxyAsync(url);
            }
        }
        
        /// <summary>
        /// 检查单个代理
        /// </summary>
        public async Task<ProxyHealthStatus> CheckProxyAsync(string proxyUrl)
        {
            var status = _proxyStatus.ContainsKey(proxyUrl) ? _proxyStatus[proxyUrl] : new ProxyHealthStatus { ProxyUrl = proxyUrl };
            
            try
            {
                var result = await ProxyTester.TestProxyAsync(proxyUrl, _config.TestUrl, _config.TimeoutSeconds);
                
                status.TotalChecks++;
                status.LastChecked = DateTime.Now;
                
                if (result.Success && result.ResponseTimeMs <= _config.MaxLatencyMs)
                {
                    // 健康
                    status.SuccessCount++;
                    status.IsHealthy = true;
                    status.FailCount = 0;
                    status.AvgLatencyMs = (status.AvgLatencyMs + result.ResponseTimeMs) / 2;
                    status.LastSuccess = DateTime.Now;
                    status.LastError = null;
                }
                else
                {
                    // 失败
                    status.FailCount++;
                    status.IsHealthy = status.FailCount < _config.MaxFailures;
                    status.LastFailure = DateTime.Now;
                    status.LastError = result.ErrorMessage ?? "超时或延迟过高";
                    
                    OnProxyFailed?.Invoke(this, new ProxyHealthEventArgs
                    {
                        ProxyUrl = proxyUrl,
                        IsHealthy = status.IsHealthy,
                        LatencyMs = result.ResponseTimeMs,
                        ErrorMessage = status.LastError
                    });
                }
            }
            catch (Exception ex)
            {
                status.FailCount++;
                status.IsHealthy = status.FailCount < _config.MaxFailures;
                status.LastChecked = DateTime.Now;
                status.LastFailure = DateTime.Now;
                status.LastError = ex.Message;
            }
            
            _proxyStatus[proxyUrl] = status;
            
            // 触发状态变更事件
            OnHealthChanged?.Invoke(this, new ProxyHealthEventArgs
            {
                ProxyUrl = proxyUrl,
                IsHealthy = status.IsHealthy,
                LatencyMs = status.AvgLatencyMs,
                ErrorMessage = status.LastError
            });
            
            return status;
        }
        
        /// <summary>
        /// 获取健康的代理
        /// </summary>
        public List<string> GetHealthyProxies()
        {
            return _proxyStatus
                .Where(kvp => kvp.Value.IsHealthy)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// 获取最佳代理（最低延迟）
        /// </summary>
        public string? GetBestProxy()
        {
            return _proxyStatus
                .Where(kvp => kvp.Value.IsHealthy && kvp.Value.AvgLatencyMs > 0)
                .OrderBy(kvp => kvp.Value.AvgLatencyMs)
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// 获取代理状态
        /// </summary>
        public ProxyHealthStatus? GetStatus(string proxyUrl)
        {
            return _proxyStatus.ContainsKey(proxyUrl) ? _proxyStatus[proxyUrl] : null;
        }
        
        /// <summary>
        /// 获取所有代理状态
        /// </summary>
        public IReadOnlyDictionary<string, ProxyHealthStatus> GetAllStatus()
        {
            return _proxyStatus;
        }
        
        /// <summary>
        /// 手动触发一次检查
        /// </summary>
        public async Task ForceCheckAsync(IEnumerable<string> proxyUrls)
        {
            foreach (var url in proxyUrls)
            {
                await CheckProxyAsync(url);
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
    
    /// <summary>
    /// 健康检查事件参数
    /// </summary>
    public class ProxyHealthEventArgs : EventArgs
    {
        public string ProxyUrl { get; set; } = "";
        public bool IsHealthy { get; set; }
        public int LatencyMs { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// 自动切换代理管理器
    /// </summary>
    public class AutoSwitchManager : IDisposable
    {
        private readonly ProxyHealthCheckManager _healthCheck;
        private string? _activeProxy;
        private Timer? _switchTimer;
        
        public AutoSwitchManager(ProxyHealthCheckManager healthCheck)
        {
            _healthCheck = healthCheck;
            _healthCheck.OnProxyFailed += OnProxyFailed;
        }
        
        /// <summary>
        /// 启动自动切换
        /// </summary>
        public void Start(IEnumerable<string> proxies, int checkIntervalSeconds = 30)
        {
            _healthCheck.Start(proxies);
            
            _switchTimer = new Timer(_ =>
            {
                TrySwitchToBetterProxy();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(checkIntervalSeconds));
        }
        
        /// <summary>
        /// 停止自动切换
        /// </summary>
        public void Stop()
        {
            _switchTimer?.Dispose();
            _switchTimer = null;
        }
        
        /// <summary>
        /// 获取当前活跃代理
        /// </summary>
        public string? GetActiveProxy() => _activeProxy;
        
        /// <summary>
        /// 手动切换到指定代理
        /// </summary>
        public bool SwitchTo(string proxyUrl)
        {
            var status = _healthCheck.GetStatus(proxyUrl);
            if (status == null || !status.IsHealthy)
            {
                Console.WriteLine($"❌ 代理不可用: {proxyUrl}");
                return false;
            }
            
            _activeProxy = proxyUrl;
            Console.WriteLine($"✅ 已切换到代理: {proxyUrl}");
            return true;
        }
        
        private void OnProxyFailed(object? sender, ProxyHealthEventArgs e)
        {
            TrySwitchToBetterProxy();
        }
        
        private void TrySwitchToBetterProxy()
        {
            var best = _healthCheck.GetBestProxy();
            if (best != null && best != _activeProxy)
            {
                SwitchTo(best);
            }
        }
        
        public void Dispose()
        {
            Stop();
            _healthCheck.OnProxyFailed -= OnProxyFailed;
        }
    }
}