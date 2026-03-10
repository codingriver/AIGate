using System;
using System.Collections.Generic;

namespace ProxyTool.UI
{
    /// <summary>
    /// 错误提示与自动修复建议
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public enum ErrorType
        {
            FileNotFound,
            PermissionDenied,
            InvalidConfig,
            NetworkError,
            ToolNotFound,
            UnsupportedOperation,
            Unknown
        }

        /// <summary>
        /// 获取错误类型对应的建议
        /// </summary>
        public static ErrorSuggestion GetSuggestion(ErrorType errorType, string? details = null)
        {
            return errorType switch
            {
                ErrorType.FileNotFound => new ErrorSuggestion
                {
                    Title = "文件未找到",
                    Description = details ?? "指定的配置文件不存在",
                    Suggestions = new List<string>
                    {
                        "检查文件路径是否正确",
                        "确认文件是否被移动或删除",
                        "使用绝对路径而非相对路径"
                    }
                },
                
                ErrorType.PermissionDenied => new ErrorSuggestion
                {
                    Title = "权限不足",
                    Description = "没有足够的权限执行此操作",
                    Suggestions = new List<string>
                    {
                        "以管理员身份运行程序",
                        "检查文件/目录权限设置",
                        "对于系统级配置，可能需要 root 权限"
                    }
                },
                
                ErrorType.InvalidConfig => new ErrorSuggestion
                {
                    Title = "配置无效",
                    Description = details ?? "代理配置格式不正确",
                    Suggestions = new List<string>
                    {
                        "检查代理地址格式 (http://host:port)",
                        "确认端口号在 1-65535 范围内",
                        "验证 URL 格式是否正确"
                    },
                    AutoFix = "可以使用 ConfigValidator 验证配置"
                },
                
                ErrorType.NetworkError => new ErrorSuggestion
                {
                    Title = "网络错误",
                    Description = "无法连接到代理服务器",
                    Suggestions = new List<string>
                    {
                        "检查代理服务器是否运行",
                        "验证网络连接是否正常",
                        "确认防火墙没有阻止连接"
                    }
                },
                
                ErrorType.ToolNotFound => new ErrorSuggestion
                {
                    Title = "工具未找到",
                    Description = $"未找到指定的工具: {details}",
                    Suggestions = new List<string>
                    {
                        "确认工具已正确安装",
                        "检查 PATH 环境变量",
                        "使用 --detect 选项自动检测"
                    }
                },
                
                ErrorType.UnsupportedOperation => new ErrorSuggestion
                {
                    Title = "不支持的操作",
                    Description = details ?? "当前平台不支持此操作",
                    Suggestions = new List<string>
                    {
                        "检查是否使用正确的平台",
                        "某些操作需要特定操作系统"
                    }
                },
                
                _ => new ErrorSuggestion
                {
                    Title = "未知错误",
                    Description = details ?? "发生了未知错误",
                    Suggestions = new List<string>
                    {
                        "查看详细日志了解更多信息",
                        "尝试重启程序"
                    }
                }
            };
        }

        /// <summary>
        /// 打印错误和建议
        /// </summary>
        public static void PrintError(ErrorType errorType, string? details = null)
        {
            var suggestion = GetSuggestion(errorType, details);
            
            ConsoleStyle.Error(suggestion.Title);
            Console.WriteLine($"  {suggestion.Description}");
            Console.WriteLine();
            
            ConsoleStyle.Subtitle("建议:");
            foreach (var s in suggestion.Suggestions)
            {
                Console.WriteLine($"  • {s}");
            }
            
            if (!string.IsNullOrEmpty(suggestion.AutoFix))
            {
                Console.WriteLine();
                ConsoleStyle.Info($"自动修复: {suggestion.AutoFix}");
            }
        }
        
        /// <summary>
        /// 打印详细错误信息（用于调试）
        /// </summary>
        public static void PrintDetailedError(Exception ex, bool showStackTrace = false)
        {
            ConsoleStyle.Error("发生错误");
            Console.WriteLine($"  类型: {ex.GetType().Name}");
            Console.WriteLine($"  消息: {ex.Message}");
            
            if (showStackTrace && !string.IsNullOrEmpty(ex.StackTrace))
            {
                Console.WriteLine();
                ConsoleStyle.Subtitle("堆栈跟踪:");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    /// <summary>
    /// 错误建议
    /// </summary>
    public class ErrorSuggestion
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Suggestions { get; set; } = new();
        public string? AutoFix { get; set; }
    }
}