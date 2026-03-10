using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gate.Managers
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
    
    /// <summary>
    /// 日志管理器
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gate",
            "logs"
        );
        
        private static LogLevel _minimumLevel = LogLevel.Info;
        
        /// <summary>
        /// 最小日志级别
        /// </summary>
        public static LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }
        
        static Logger()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
        }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }
        
        /// <summary>
        /// 信息日志
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }
        
        /// <summary>
        /// 警告日志
        /// </summary>
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }
        
        /// <summary>
        /// 错误日志
        /// </summary>
        public static void Error(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            Log(LogLevel.Error, fullMessage);
        }
        
        /// <summary>
        /// 写入日志
        /// </summary>
        private static void Log(LogLevel level, string message)
        {
            if (level < _minimumLevel)
                return;
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var levelStr = level.ToString().ToUpper();
            var logLine = $"[{timestamp}] [{levelStr}] {message}";
            
            // 写入文件
            var logFile = Path.Combine(LogDir, $"gate_{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(logFile, logLine + Environment.NewLine);
            
            // 控制台输出（Error和Warning）
            if (level >= LogLevel.Warning)
            {
                Console.WriteLine(logLine);
            }
        }
        
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public static string GetLogFilePath()
        {
            return Path.Combine(LogDir, $"gate_{DateTime.Now:yyyyMMdd}.log");
        }
        
        /// <summary>
        /// 查看日志
        /// </summary>
        public static string[] GetLogs(int lines = 50)
        {
            var logFile = GetLogFilePath();
            if (!File.Exists(logFile))
                return new string[0];
            
            var allLines = File.ReadAllLines(logFile);
            if (allLines.Length <= lines)
                return allLines;
            
            // 手动实现 Skip
            var result = new string[lines];
            Array.Copy(allLines, allLines.Length - lines, result, 0, lines);
            return result;
        }
    }
}
