using System;

namespace ProxyTool.UI
{
    /// <summary>
    /// 控制台输出样式
    /// </summary>
    public static class ConsoleStyle
    {
        // 颜色代码
        public const string RESET = "\u001b[0m";
        public const string BOLD = "\u001b[1m";
        public const string DIM = "\u001b[2m";
        
        // 前景色
        public const string FG_BLACK = "\u001b[30m";
        public const string FG_RED = "\u001b[31m";
        public const string FG_GREEN = "\u001b[32m";
        public const string FG_YELLOW = "\u001b[33m";
        public const string FG_BLUE = "\u001b[34m";
        public const string FG_MAGENTA = "\u001b[35m";
        public const string FG_CYAN = "\u001b[36m";
        public const string FG_WHITE = "\u001b[37m";
        
        // 背景色
        public const string BG_BLACK = "\u001b[40m";
        public const string BG_RED = "\u001b[41m";
        public const string BG_GREEN = "\u001b[42m";
        public const string BG_YELLOW = "\u001b[43m";
        public const string BG_BLUE = "\u001b[44m";
        public const string BG_MAGENTA = "\u001b[45m";
        public const string BG_CYAN = "\u001b[46m";
        
        /// <summary>
        /// 是否启用彩色输出
        /// </summary>
        public static bool EnableColors { get; set; } = true;
        
        /// <summary>
        /// 打印成功消息
        /// </summary>
        public static void Success(string message)
        {
            if (EnableColors)
                Console.WriteLine($"{FG_GREEN}✓{RESET} {message}");
            else
                Console.WriteLine($"✓ {message}");
        }
        
        /// <summary>
        /// 打印错误消息
        /// </summary>
        public static void Error(string message)
        {
            if (EnableColors)
                Console.WriteLine($"{FG_RED}✗{RESET} {message}");
            else
                Console.WriteLine($"✗ {message}");
        }
        
        /// <summary>
        /// 打印警告消息
        /// </summary>
        public static void Warning(string message)
        {
            if (EnableColors)
                Console.WriteLine($"{FG_YELLOW}⚠{RESET} {message}");
            else
                Console.WriteLine($"⚠ {message}");
        }
        
        /// <summary>
        /// 打印信息消息
        /// </summary>
        public static void Info(string message)
        {
            if (EnableColors)
                Console.WriteLine($"{FG_CYAN}ℹ{RESET} {message}");
            else
                Console.WriteLine($"ℹ {message}");
        }
        
        /// <summary>
        /// 打印标题
        /// </summary>
        public static void Title(string title)
        {
            if (EnableColors)
                Console.WriteLine($"\n{BOLD}{FG_CYAN}{title}{RESET}");
            else
                Console.WriteLine($"\n{title}");
        }
        
        /// <summary>
        /// 打印副标题
        /// </summary>
        public static void Subtitle(string subtitle)
        {
            if (EnableColors)
                Console.WriteLine($"{BOLD}{subtitle}{RESET}");
            else
                Console.WriteLine(subtitle);
        }
        
        /// <summary>
        /// 打印列表项
        /// </summary>
        public static void ListItem(string label, string value)
        {
            if (EnableColors)
                Console.WriteLine($"  {FG_WHITE}•{RESET} {label}: {FG_GREEN}{value}{RESET}");
            else
                Console.WriteLine($"  • {label}: {value}");
        }
        
        /// <summary>
        /// 打印进度
        /// </summary>
        public static void Progress(int current, int total, string message = "")
        {
            var percentage = (double)current / total * 100;
            var barLength = 30;
            var filled = (int)(barLength * current / total);
            var bar = new string('█', filled) + new string('░', barLength - filled);
            
            if (EnableColors)
                Console.Write($"\r  [{FG_CYAN}{bar}{RESET}] {percentage:F0}% {message}");
            else
                Console.Write($"\r  [{bar}] {percentage:F0}% {message}");
            
            if (current == total)
                Console.WriteLine();
        }
        
        /// <summary>
        /// 打印分隔线
        /// </summary>
        public static void Divider(char character = '─', int length = 50)
        {
            if (EnableColors)
                Console.WriteLine($"{FG_BLACK}{new string(character, length)}{RESET}");
            else
                Console.WriteLine(new string(character, length));
        }
        
        /// <summary>
        /// 打印表格行
        /// </summary>
        public static void TableRow(params string[] columns)
        {
            Console.Write("  ");
            foreach (var col in columns)
            {
                Console.Write(col.PadRight(15));
            }
            Console.WriteLine();
        }
        
        /// <summary>
        /// 打印带颜色的文本
        /// </summary>
        public static void Colored(string text, string colorCode)
        {
            if (EnableColors)
                Console.Write($"{colorCode}{text}{RESET}");
            else
                Console.Write(text);
        }
    }
}