using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProxyTool.Models;

namespace ProxyTool.PluginCore
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    public class PluginMetadata
    {
        /// <summary>
        /// 插件唯一标识
        /// </summary>
        public string Id { get; set; } = "";
        
        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 插件描述
        /// </summary>
        public string Description { get; set; } = "";
        
        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = "";
        
        /// <summary>
        /// 插件类型
        /// </summary>
        public PluginType Type { get; set; } = PluginType.ToolConfigurator;
        
        /// <summary>
        /// 依赖
        /// </summary>
        public List<string> Dependencies { get; set; } = new();
        
        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// 入口类名
        /// </summary>
        public string EntryPoint { get; set; } = "";
        
        /// <summary>
        /// DLL 路径（运行时加载）
        /// </summary>
        public string? DllPath { get; set; }
        
        /// <summary>
        /// 配置 Schema (JSON Schema)
        /// </summary>
        public string? ConfigSchema { get; set; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 安装时间
        /// </summary>
        public DateTime InstalledAt { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 插件类型
    /// </summary>
    public enum PluginType
    {
        /// <summary>
        /// 工具配置器
        /// </summary>
        ToolConfigurator,
        
        /// <summary>
        /// 代理测试器
        /// </summary>
        ProxyTester,
        
        /// <summary>
        /// 配置文件解析器
        /// </summary>
        ConfigParser,
        
        /// <summary>
        /// 存储后端
        /// </summary>
        StorageBackend,
        
        /// <summary>
        /// UI 扩展
        /// </summary>
        UiExtension,
        
        /// <summary>
        /// 通知服务
        /// </summary>
        NotificationService
    }
    
    /// <summary>
    /// 插件状态
    /// </summary>
    public enum PluginStatus
    {
        Unknown,
        Loaded,
        Enabled,
        Disabled,
        Error
    }
    
    /// <summary>
    /// 插件状态信息
    /// </summary>
    public class PluginState
    {
        public PluginMetadata Metadata { get; set; } = new();
        public PluginStatus Status { get; set; } = PluginStatus.Unknown;
        public string? ErrorMessage { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 插件接口 - 所有插件必须实现
    /// </summary>
    public interface IProxyToolPlugin
    {
        /// <summary>
        /// 获取插件元数据
        /// </summary>
        PluginMetadata GetMetadata();
        
        /// <summary>
        /// 初始化插件
        /// </summary>
        Task InitializeAsync(Dictionary<string, object> config);
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        Task ShutdownAsync();
        
        /// <summary>
        /// 获取插件状态
        /// </summary>
        PluginState GetState();
    }
    
    /// <summary>
    /// 工具配置器插件接口
    /// </summary>
    public interface IToolConfiguratorPlugin : IProxyToolPlugin
    {
        /// <summary>
        /// 获取工具名称
        /// </summary>
        string ToolName { get; }
        
        /// <summary>
        /// 获取分类
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// 检测配置路径
        /// </summary>
        string? DetectConfigPath();
        
        /// <summary>
        /// 检查工具是否安装
        /// </summary>
        bool IsInstalled();
        
        /// <summary>
        /// 获取当前配置
        /// </summary>
        ProxyConfig? GetCurrentConfig();
        
        /// <summary>
        /// 设置代理
        /// </summary>
        bool SetProxy(string proxyUrl);
        
        /// <summary>
        /// 清除代理
        /// </summary>
        bool ClearProxy();
    }
    
    /// <summary>
    /// 代理测试器插件接口
    /// </summary>
    public interface IProxyTesterPlugin : IProxyToolPlugin
    {
        /// <summary>
        /// 测试代理
        /// </summary>
        Task<ProxyTestResult> TestProxyAsync(string proxyUrl, string? testUrl = null, int timeoutSec = 10);
        
        /// <summary>
        /// 获取支持的网络协议
        /// </summary>
        List<string> SupportedProtocols { get; }
    }
    
    /// <summary>
    /// 插件加载器接口
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// 加载插件
        /// </summary>
        Task<IProxyToolPlugin?> LoadPluginAsync(string pluginPath);
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        Task UnloadPluginAsync(string pluginId);
        
        /// <summary>
        /// 获取所有已加载的插件
        /// </summary>
        IReadOnlyDictionary<string, IProxyToolPlugin> GetLoadedPlugins();
        
        /// <summary>
        /// 按类型获取插件
        /// </summary>
        IEnumerable<IProxyToolPlugin> GetPluginsByType(PluginType type);
    }
    
    /// <summary>
    /// 插件管理器 - 管理所有插件
    /// </summary>
    public class PluginManager : IPluginLoader
    {
        private readonly Dictionary<string, IProxyToolPlugin> _plugins = new();
        private readonly Dictionary<string, PluginState> _pluginStates = new();
        private readonly List<string> _pluginDirectories = new();
        
        /// <summary>
        /// 插件目录
        /// </summary>
        public IReadOnlyList<string> PluginDirectories => _pluginDirectories;
        
        /// <summary>
        /// 添加插件目录
        /// </summary>
        public void AddPluginDirectory(string path)
        {
            if (System.IO.Directory.Exists(path))
            {
                _pluginDirectories.Add(path);
            }
        }
        
        /// <summary>
        /// 扫描并加载目录中的所有插件
        /// </summary>
        public async Task<int> ScanAndLoadPluginsAsync()
        {
            var loaded = 0;
            
            foreach (var dir in _pluginDirectories)
            {
                var pluginFiles = System.IO.Directory.GetFiles(dir, "*.dll");
                
                foreach (var file in pluginFiles)
                {
                    try
                    {
                        var plugin = await LoadPluginAsync(file);
                        if (plugin != null)
                        {
                            loaded++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载插件失败 {file}: {ex.Message}");
                    }
                }
                
                // 也扫描 JSON 配置文件（轻量级插件）
                var configFiles = System.IO.Directory.GetFiles(dir, "*.plugin.json");
                foreach (var file in configFiles)
                {
                    try
                    {
                        await LoadPluginConfigAsync(file);
                        loaded++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载插件配置失败 {file}: {ex.Message}");
                    }
                }
            }
            
            return loaded;
        }
        
        /// <summary>
        /// 从 DLL 加载插件
        /// </summary>
        public async Task<IProxyToolPlugin?> LoadPluginAsync(string pluginPath)
        {
            // 简化的插件加载实现
            // 实际使用时需要使用 Assembly.LoadFrom 加载 DLL
            // 并通过反射创建插件实例
            
            Console.WriteLine($"[PluginManager] Loading plugin from: {pluginPath}");
            
            // 这里是一个示例实现
            // 实际项目中需要完整的 .NET 程序集加载逻辑
            await Task.CompletedTask;
            
            return null;
        }
        
        /// <summary>
        /// 从配置文件加载轻量级插件
        /// </summary>
        private async Task LoadPluginConfigAsync(string configPath)
        {
            var json = await System.IO.File.ReadAllTextAsync(configPath);
            var metadata = System.Text.Json.JsonSerializer.Deserialize<PluginMetadata>(json);
            
            if (metadata != null)
            {
                var state = new PluginState
                {
                    Metadata = metadata,
                    Status = metadata.Enabled ? PluginStatus.Enabled : PluginStatus.Disabled
                };
                
                _pluginStates[metadata.Id] = state;
            }
        }
        
        /// <summary>
        /// 卸载插件
        /// </summary>
        public async Task UnloadPluginAsync(string pluginId)
        {
            if (_plugins.TryGetValue(pluginId, out var plugin))
            {
                await plugin.ShutdownAsync();
                _plugins.Remove(pluginId);
            }
            
            _pluginStates.Remove(pluginId);
        }
        
        /// <summary>
        /// 获取已加载的插件
        /// </summary>
        public IReadOnlyDictionary<string, IProxyToolPlugin> GetLoadedPlugins()
        {
            return _plugins;
        }
        
        /// <summary>
        /// 获取插件状态
        /// </summary>
        public IReadOnlyDictionary<string, PluginState> GetPluginStates()
        {
            return _pluginStates;
        }
        
        /// <summary>
        /// 按类型获取插件
        /// </summary>
        public IEnumerable<IProxyToolPlugin> GetPluginsByType(PluginType type)
        {
            return _plugins.Values.Where(p => p.GetMetadata().Type == type);
        }
        
        /// <summary>
        /// 启用插件
        /// </summary>
        public async Task<bool> EnablePluginAsync(string pluginId)
        {
            if (_pluginStates.TryGetValue(pluginId, out var state))
            {
                state.Status = PluginStatus.Enabled;
                state.Metadata.Enabled = true;
                state.LastUpdated = DateTime.Now;
                
                await Task.CompletedTask;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 禁用插件
        /// </summary>
        public async Task<bool> DisablePluginAsync(string pluginId)
        {
            if (_pluginStates.TryGetValue(pluginId, out var state))
            {
                state.Status = PluginStatus.Disabled;
                state.Metadata.Enabled = false;
                state.LastUpdated = DateTime.Now;
                
                await Task.CompletedTask;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 注册插件（内部使用）
        /// </summary>
        internal void RegisterPlugin(IProxyToolPlugin plugin)
        {
            var metadata = plugin.GetMetadata();
            _plugins[metadata.Id] = plugin;
            
            _pluginStates[metadata.Id] = new PluginState
            {
                Metadata = metadata,
                Status = PluginStatus.Loaded
            };
        }
    }
    
    /// <summary>
    /// 插件仓库 - 远程插件注册表
    /// </summary>
    public class PluginRepository
    {
        private readonly string _localCachePath;
        private readonly HttpClient _httpClient;
        
        public PluginRepository(string localCachePath)
        {
            _localCachePath = localCachePath;
            _httpClient = new HttpClient();
            
            if (!System.IO.Directory.Exists(localCachePath))
            {
                System.IO.Directory.CreateDirectory(localCachePath);
            }
        }
        
        /// <summary>
        /// 官方插件仓库地址
        /// </summary>
        public string RegistryUrl { get; set; } = "https://raw.githubusercontent.com/proxytool/plugins/main";
        
        /// <summary>
        /// 获取远程插件列表
        /// </summary>
        public async Task<List<PluginMetadata>> GetRemotePluginsAsync()
        {
            try
            {
                var indexUrl = $"{RegistryUrl}/plugins.json";
                var response = await _httpClient.GetStringAsync(indexUrl);
                var plugins = System.Text.Json.JsonSerializer.Deserialize<List<PluginMetadata>>(response);
                return plugins ?? new List<PluginMetadata>();
            }
            catch
            {
                return new List<PluginMetadata>();
            }
        }
        
        /// <summary>
        /// 下载插件
        /// </summary>
        public async Task<string?> DownloadPluginAsync(string pluginId, string version)
        {
            try
            {
                var downloadUrl = $"{RegistryUrl}/releases/{pluginId}/{version}/{pluginId}.zip";
                var response = await _httpClient.GetByteArrayAsync(downloadUrl);
                
                var localPath = System.IO.Path.Combine(_localCachePath, $"{pluginId}.zip");
                await System.IO.File.WriteAllBytesAsync(localPath, response);
                
                return localPath;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 安装插件
        /// </summary>
        public async Task<bool> InstallPluginAsync(string pluginId, string version)
        {
            var zipPath = await DownloadPluginAsync(pluginId, version);
            if (zipPath == null) return false;
            
            // 解压到插件目录
            var extractPath = System.IO.Path.Combine(_localCachePath, pluginId);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
            
            return true;
        }
        
        /// <summary>
        /// 搜索插件
        /// </summary>
        public async Task<List<PluginMetadata>> SearchPluginsAsync(string keyword)
        {
            var plugins = await GetRemotePluginsAsync();
            
            if (string.IsNullOrEmpty(keyword))
                return plugins;
            
            return plugins.Where(p => 
                p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        
        /// <summary>
        /// 获取本地已安装的插件
        /// </summary>
        public List<PluginMetadata> GetLocalPlugins()
        {
            var plugins = new List<PluginMetadata>();
            
            if (!System.IO.Directory.Exists(_localCachePath))
                return plugins;
            
            foreach (var dir in System.IO.Directory.GetDirectories(_localCachePath))
            {
                var configFile = System.IO.Path.Combine(dir, "plugin.json");
                if (System.IO.File.Exists(configFile))
                {
                    var json = System.IO.File.ReadAllText(configFile);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<PluginMetadata>(json);
                    if (metadata != null)
                    {
                        plugins.Add(metadata);
                    }
                }
            }
            
            return plugins;
        }
    }
}