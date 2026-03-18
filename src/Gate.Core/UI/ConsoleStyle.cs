using System;

namespace Gate.UI
{
    /// <summary>
    /// 控制台彩色输出工具类
    /// </summary>
    public static class ConsoleStyle
    {
        // ANSI 颜色码
        public const string RESET     = "\u001b[0m";
        public const string BOLD      = "\u001b[1m";
        public const string DIM       = "\u001b[2m";
        public const string FG_BLACK  = "\u001b[30m";
        public const string FG_RED    = "\u001b[31m";
        public const string FG_GREEN  = "\u001b[32m";
        public const string FG_YELLOW = "\u001b[33m";
        public const string FG_BLUE   = "\u001b[34m";
        public const string FG_MAGENTA= "\u001b[35m";
        public const string FG_CYAN   = "\u001b[36m";
        public const string FG_WHITE  = "\u001b[37m";
        public const string BG_BLACK  = "\u001b[40m";
        public const string BG_RED    = "\u001b[41m";
        public const string BG_GREEN  = "\u001b[42m";
        public const string BG_BLUE   = "\u001b[44m";
        public const string BG_CYAN   = "\u001b[46m";

        /// <summary>是否启用彩色输出（由 OutputSettings.NoColor 统一控制）</summary>
        public static bool EnableColors =>
            !OutputSettings.NoColor;

        // ── 语义化输出方法 ───────────────────────────────────────────────────

        /// <summary>成功（绿色 ✓）</summary>
        public static void Success(string message)
        {
            if (EnableColors) Console.WriteLine($"{FG_GREEN}✓{RESET} {message}");
            else              Console.WriteLine($"[OK] {message}");
        }

        /// <summary>错误（红色 ✗）</summary>
        public static void Error(string message)
        {
            if (EnableColors) Console.WriteLine($"{FG_RED}✗{RESET} {message}");
            else              Console.WriteLine($"[ERR] {message}");
        }

        /// <summary>警告（黄色 ⚠）</summary>
        public static void Warning(string message)
        {
            if (EnableColors) Console.WriteLine($"{FG_YELLOW}⚠{RESET}  {message}");
            else              Console.WriteLine($"[WARN] {message}");
        }

        /// <summary>信息（青色 ℹ）</summary>
        public static void Info(string message)
        {
            if (EnableColors) Console.WriteLine($"{FG_CYAN}ℹ{RESET}  {message}");
            else              Console.WriteLine($"[INFO] {message}");
        }

        /// <summary>一级标题（粗体青色，前置空行）</summary>
        public static void Title(string title)
        {
            var divider = new string('─', Math.Min(title.Length + 2, 60));
            if (EnableColors)
            {
                Console.WriteLine($"\n{BOLD}{FG_CYAN}{title}{RESET}\n{FG_BLACK}{divider}{RESET}");
            }
            else
            {
                Console.WriteLine($"\n{title}\n{divider}");
            }
        }

        /// <summary>二级标题（粗体白色）</summary>
        public static void Subtitle(string subtitle)
        {
            if (EnableColors) Console.WriteLine($"\n{BOLD}{FG_WHITE}{subtitle}{RESET}");
            else              Console.WriteLine($"\n{subtitle}");
        }

        /// <summary>键值列表项（• key: value，value 为绿色）</summary>
        public static void ListItem(string label, string value)
        {
            if (EnableColors)
                Console.WriteLine($"  {FG_WHITE}•{RESET} {label,-12}: {FG_YELLOW}{value}{RESET}");
            else
                Console.WriteLine($"  • {label,-12}: {value}");
        }

        /// <summary>进度条（原地刷新，current==total 时换行）</summary>
        public static void Progress(int current, int total, string message = "")
        {
            var pct     = (double)current / total * 100;
            var barLen  = 30;
            var filled  = (int)(barLen * current / total);
            var bar     = new string('█', filled) + new string('░', barLen - filled);
            if (EnableColors)
                Console.Write($"\r  [{FG_CYAN}{bar}{RESET}] {pct:F0}% {message}");
            else
                Console.Write($"\r  [{bar}] {pct:F0}% {message}");
            if (current == total) Console.WriteLine();
        }

        /// <summary>水平分隔线</summary>
        public static void Divider(char ch = '─', int length = 50)
        {
            if (EnableColors) Console.WriteLine($"{DIM}{new string(ch, length)}{RESET}");
            else              Console.WriteLine(new string(ch, length));
        }

        /// <summary>带颜色的内联文本（不换行）</summary>
        public static void Colored(string text, string colorCode)
        {
            if (EnableColors) Console.Write($"{colorCode}{text}{RESET}");
            else              Console.Write(text);
        }

        /// <summary>直接输出一行（已含格式化字符串）</summary>
        public static void Print(string message) => Console.WriteLine(message);

        /// <summary>下一步建议提示</summary>
        public static void NextStep(string hint)
        {
            if (EnableColors)
                Console.WriteLine($"  {DIM}提示：{hint}{RESET}");
            else
                Console.WriteLine($"  提示：{hint}");
        }
    }
}
