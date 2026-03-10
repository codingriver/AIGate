using System;
using ProxyTool.Models;

namespace ProxyTool.Platforms
{
    /// <summary>
    /// 系统环境变量提供程序接口
    /// </summary>
    public interface ISystemEnvProvider
    {
        /// <summary>
        /// 设置系统代理
        /// </summary>
        bool SetProxy(ProxyConfig config);
        
        /// <summary>
        /// 获取系统代理
        /// </summary>
        ProxyConfig GetProxy();
        
        /// <summary>
        /// 清除系统代理
        /// </summary>
        bool ClearProxy();
        
        /// <summary>
        /// 是否需要管理员权限
        /// </summary>
        bool RequiresAdmin { get; }
        
        /// <summary>
        /// 检查当前是否有权限
        /// </summary>
        bool HasPermission();
    }
}
