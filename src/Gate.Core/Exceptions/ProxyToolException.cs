using System;

namespace Gate.Exceptions
{
    /// <summary>
    /// 代理工具错误类型
    /// </summary>
    public enum ProxyToolError
    {
        ToolNotInstalled,
        ConfigFileNotFound,
        ConfigFileLocked,
        PermissionDenied,
        InvalidProxyUrl,
        BackupFailed,
        RestoreFailed,
        NetworkError,
        ValidationFailed,
        TransactionFailed
    }
    
    /// <summary>
    /// 代理工具异常
    /// </summary>
    public class ProxyToolException : Exception
    {
        public ProxyToolError ErrorCode { get; }
        public string ToolName { get; }
        public string SuggestedFix { get; }
        
        public ProxyToolException(ProxyToolError errorCode, string message, string toolName = "", string suggestedFix = "")
            : base(message)
        {
            ErrorCode = errorCode;
            ToolName = toolName;
            SuggestedFix = suggestedFix;
        }
        
        /// <summary>
        /// 获取错误恢复建议
        /// </summary>
        public static string GetSuggestedFix(ProxyToolError errorCode)
        {
            return errorCode switch
            {
                ProxyToolError.ToolNotInstalled => "请先安装该工具",
                ProxyToolError.ConfigFileNotFound => "配置文件不存在，尝试手动指定路径",
                ProxyToolError.ConfigFileLocked => "请关闭占用该配置文件的程序",
                ProxyToolError.PermissionDenied => "请使用管理员权限或 sudo 运行",
                ProxyToolError.InvalidProxyUrl => "请检查代理地址格式（例如：http://host:port）",
                ProxyToolError.BackupFailed => "请检查磁盘空间和权限",
                ProxyToolError.RestoreFailed => "备份文件可能已损坏，请手动恢复",
                ProxyToolError.NetworkError => "请检查网络连接和代理设置",
                ProxyToolError.ValidationFailed => "请检查配置参数",
                ProxyToolError.TransactionFailed => "部分工具配置失败，已回滚",
                _ => "请查看详细错误信息"
            };
        }
    }
}
