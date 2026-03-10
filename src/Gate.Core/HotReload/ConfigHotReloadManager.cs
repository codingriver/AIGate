using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Gate.HotReload
{
    /// <summary>
    /// 配置热重载管理器
    /// </summary>
    public class ConfigHotReloadManager : IDisposable
    {
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        private readonly Dictionary<string, DateTime> _lastModified = new();
        private readonly List<IConfigChangeListener> _listeners = new();
        private Timer? _debounceTimer;
        private string? _pendingChange;
        private readonly object _lock = new();
        
        /// <summary>
        /// 配置变更监听器接口
        /// </summary>
        public interface IConfigChangeListener
        {
            void OnConfigChanged(string configPath);
        }
        
        /// <summary>
        /// 监听配置文件目录
        /// </summary>
        public void WatchDirectory(string directoryPath, string filter = "*.json")
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // 避免重复监听
            if (_watchers.ContainsKey(directoryPath))
                return;
            
            var watcher = new FileSystemWatcher(directoryPath)
            {
                Filter = filter,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            
            _watchers[directoryPath] = watcher;
        }
        
        /// <summary>
        /// 监听单个配置文件
        /// </summary>
        public void WatchFile(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            
            if (string.IsNullOrEmpty(directory))
                return;
            
            if (!_watchers.ContainsKey(directory))
            {
                WatchDirectory(directory, fileName);
            }
            
            // 记录初始修改时间
            if (File.Exists(filePath))
            {
                _lastModified[filePath] = File.GetLastWriteTime(filePath);
            }
        }
        
        /// <summary>
        /// 添加监听器
        /// </summary>
        public void AddListener(IConfigChangeListener listener)
        {
            _listeners.Add(listener);
        }
        
        /// <summary>
        /// 移除监听器
        /// </summary>
        public void RemoveListener(IConfigChangeListener listener)
        {
            _listeners.Remove(listener);
        }
        
        /// <summary>
        /// 停止监听
        /// </summary>
        public void StopWatching(string? path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                // 停止所有
                foreach (var watcher in _watchers.Values)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                _watchers.Clear();
            }
            else if (_watchers.ContainsKey(path))
            {
                var watcher = _watchers[path];
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(path);
            }
        }
        
        /// <summary>
        /// 触发配置重新加载
        /// </summary>
        public void Reload(string configPath)
        {
            Console.WriteLine($"🔄 检测到配置变更: {Path.GetFileName(configPath)}");
            
            foreach (var listener in _listeners)
            {
                try
                {
                    listener.OnConfigChanged(configPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"配置重载监听器错误: {ex.Message}");
                }
            }
        }
        
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            HandleFileChange(e.FullPath);
        }
        
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            HandleFileChange(e.FullPath);
        }
        
        private void HandleFileChange(string filePath)
        {
            lock (_lock)
            {
                // 防抖处理：500ms 内多次变更只触发一次
                _pendingChange = filePath;
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ =>
                {
                    string? change = null;
                    lock (_lock)
                    {
                        change = _pendingChange;
                        _pendingChange = null;
                    }
                    
                    if (change != null && File.Exists(change))
                    {
                        var currentModified = File.GetLastWriteTime(change);
                        
                        // 检查是否真的变更了
                        if (_lastModified.TryGetValue(change, out var lastTime))
                        {
                            if (Math.Abs((currentModified - lastTime).TotalMilliseconds) < 100)
                                return;
                        }
                        
                        _lastModified[change] = currentModified;
                        Reload(change);
                    }
                }, null, 500, Timeout.Infinite);
            }
        }
        
        /// <summary>
        /// 获取所有监听的路径
        /// </summary>
        public IReadOnlyList<string> GetWatchedPaths()
        {
            return new List<string>(_watchers.Keys);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _debounceTimer?.Dispose();
            foreach (var watcher in _watchers.Values)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
        }
    }
    
    /// <summary>
    /// 默认配置变更监听器实现
    /// </summary>
    public class DefaultConfigReloader : ConfigHotReloadManager.IConfigChangeListener
    {
        private readonly Action<string> _onChange;
        
        public DefaultConfigReloader(Action<string> onChange)
        {
            _onChange = onChange;
        }
        
        public void OnConfigChanged(string configPath)
        {
            _onChange?.Invoke(configPath);
        }
    }
}