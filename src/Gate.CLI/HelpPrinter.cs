using System;
using Gate.UI;

/// <summary>
/// Rich help output — matches the new verb+noun command structure.
/// </summary>
internal static class HelpPrinter
{
    private static bool C => ConsoleStyle.EnableColors;
    private static string B(string s)  => C ? $"{ConsoleStyle.BOLD}{s}{ConsoleStyle.RESET}" : s;
    private static string Cy(string s) => C ? $"{ConsoleStyle.FG_CYAN}{s}{ConsoleStyle.RESET}" : s;
    private static string Ye(string s) => C ? $"{ConsoleStyle.FG_YELLOW}{s}{ConsoleStyle.RESET}" : s;
    private static string Gr(string s) => C ? $"{ConsoleStyle.FG_GREEN}{s}{ConsoleStyle.RESET}" : s;
    private static string Dm(string s) => C ? $"{ConsoleStyle.DIM}{s}{ConsoleStyle.RESET}" : s;

    private static void Ln()  => Console.WriteLine();
    private static void H1(string t)  => Console.WriteLine($"\n{B(Cy(t))}");
    private static void Sub(string t) => Console.WriteLine($"\n  {B(t)}");
    private static void Row(string a, string b, int pad = 30)
        => Console.WriteLine($"    {Ye(a.PadRight(pad))} {b}");
    private static void Eg(string s)
        => Console.WriteLine($"    {Dm("$")} {Gr(s)}");
    private static void Note(string s)
        => Console.WriteLine($"    {Dm(s)}");
    private static void Sep(int n = 58)
        => Console.WriteLine(C
            ? $"  {ConsoleStyle.DIM}{new string('\u2500', n)}{ConsoleStyle.RESET}"
            : $"  {new string('-', n)}");

    public static void Print(string? topic = null)
    {
        switch (topic?.ToLowerInvariant())
        {
            case null: case "": case "help": PrintRoot();   break;
            case "set":                      PrintSet();    break;
            case "clear":                    PrintClear();  break;
            case "app": case "tool":         PrintApp();    break;
            case "apps":                     PrintApps();   break;
            case "env": case "global":       PrintEnv();    break;
            case "preset": case "profile":   PrintPreset(); break;
            case "test": case "check":       PrintTest();   break;
            case "list":                     PrintList();   break;
            case "path":                     PrintPath();   break;
            case "wizard":                   PrintWizard(); break;
            case "info": case "status": case "show": PrintInfo(); break;
            case "apply":                    PrintApply();  break;
            case "reset":                    PrintReset();  break;
            case "doctor":                   PrintDoctor(); break;
            default:
                ConsoleStyle.Warning($"未知命令: {topic}。运行 `gate -h` 查看可用命令。");
                break;
        }
    }

    // ── root ──────────────────────────────────────────────────────────────────
    private static void PrintRoot()
    {
        H1("Gate — 跨平台命令行代理配置管理工具");
        Ln();
        Console.WriteLine($"  {B("用法")}");
        Eg("gate                              # 无参数 → 显示当前状态总览");
        Eg("gate <命令> [参数] [选项]");
        Eg("gate <命令> -h             # 查看该命令的详细帮助");
        Sub("命令列表");
        Sep();
        Row("  set <proxy> [tools]",          "设置全局代理，可同时配置工具代理", 30);
        Row("  clear [tools]",                "清除全局代理或工具代理", 30);
        Row("  app <name> [<proxy>]",         "查看或设置单个/多个工具的代理", 30);
        Row("  apps [--installed]",           "列出所有支持的工具（含安装和代理状态）", 30);
        Row("  env",                          "查看环境变量代理（Machine/User/Process 三层）", 30);
        Row("  preset <save|load|del> <name>", "管理预设配置集", 30);
        Row("  test [<proxy>]",              "测试代理连通性", 30);
        Row("  list [apps|presets]",          "列出工具或预设", 30);
        Row("  info",                         "查看当前所有代理配置状态总览", 30);
        Row("  path [-n <tool>]",            "查看或设置工具自定义路径", 30);
        Row("  wizard",                       "交互式配置向导（新手推荐）", 30);
        Row("  reset",                        "完全重置所有配置、预设和自定义路径", 30);
        Row("  doctor",                       "自动诊断工具路径、配置文件、预设完整性", 30);
        Sep();
        Sub("典型场景");
        Eg("gate set http://127.0.0.1:7890             # 设置全局代理");
        Eg("gate set http://127.0.0.1:7890 git,npm     # 全局 + 工具代理一条命令");
        Eg("gate app git                              # 查看 git 代理配置");
        Eg("gate app git http://127.0.0.1:7890        # 为 git 设置代理");
        Eg("gate app git --clear                      # 清除 git 代理");
        Eg("gate clear                               # 清除全局代理");
        Eg("gate clear git,npm                       # 清除工具代理");
        Eg("gate preset save office                  # 保存预设");
        Eg("gate preset load office                  # 加载预设");
        Eg("gate test http://127.0.0.1:7890          # 测试代理连通性");
        Eg("gate wizard                              # 交互式向导（新手推荐）");
        Ln();
        Note("运行 `gate <命令> -h` 查看各命令的详细说明和示例。");
        Ln();
    }

    // ── gate set ─────────────────────────────────────────────────────────────
    private static void PrintSet()
    {
        H1("gate set — 设置全局代理");
        Note("可同时为多个工具配置代理，一条命令完成全部设置");
        Sub("用法");
        Eg("gate set <proxy> [<tools>] [选项]");
        Sub("参数");
        Row("<proxy>",  "代理地址，如 http://127.0.0.1:7890", 16);
        Row("[tools]", "工具名称，逗号分隔，如 git,npm（可选）", 16);
        Sub("选项");
        Row("--verify, -v",   "设置前先测试代理连通性，失败则中止");
        Row("--no-proxy",     "NO_PROXY 排除列表，逗号分隔");
        Sub("示例");
        Eg("gate set http://127.0.0.1:7890");
        Note("    → 设置全局代理（HTTP_PROXY 和 HTTPS_PROXY）");
        Ln();
        Eg("gate set http://127.0.0.1:7890 git,npm");
        Note("    → 全局代理 + 同时配置 git 和 npm");
        Ln();
        Eg("gate set http://proxy:8080 git,npm,pip,cargo --verify");
        Note("    → 测试通过后一次性配置全局 + 4 个工具");
        Ln();
        Eg("gate set http://proxy:8080 --no-proxy \"localhost,127.0.0.1\"");
        Note("    → 设置代理并排除本地地址");
        Sub("说明");
        Note("• 若工具未安装则跳过并显示警告，不中止整体流程。");
        Note("• 配置完成后建议运行 gate preset save <name> 保存当前状态。");
        Ln();
    }

    // ── gate clear ────────────────────────────────────────────────────────────
    private static void PrintClear()
    {
        H1("gate clear — 清除代理");
        Sub("用法");
        Eg("gate clear [tools] [--global] [--all]");
        Sub("参数");
        Row("[tools]", "工具名称，逗号分隔（省略则清除全局代理）", 16);
        Sub("选项");
        Row("--global", "同时清除全局代理（指定工具时使用）");
        Row("--all",    "清除全局代理 + 所有已安装工具的代理（一键清除）");
        Sub("示例");
        Eg("gate clear");
        Note("    → 清除全局代理（HTTP_PROXY / HTTPS_PROXY）");
        Ln();
        Eg("gate clear git,npm");
        Note("    → 清除 git 和 npm 的代理配置");
        Ln();
        Eg("gate clear git,npm --global");
        Note("    → 同时清除工具代理和全局代理");
        Ln();
        Eg("gate clear --all");
        Note("    → 一键清除全局代理 + 所有已安装工具的代理");
        Ln();
    }

    // ── gate app ──────────────────────────────────────────────────────────────
    private static void PrintApp()
    {
        H1("gate app — 查看或设置工具代理");
        Note("支持逗号分隔批量操作，支持 --all 操作所有已安装工具");
        Sub("用法");
        Eg("gate app <name> [<proxy>] [选项]");
        Eg("gate app --all [<proxy>] [--except <names>] [--clear]");
        Sub("参数");
        Row("<name>",  "工具名称，逗号分隔，如 git 或 git,npm", 16);
        Row("[<proxy>]", "代理地址（省略则查看当前配置）", 16);
        Sub("选项");
        Row("--clear, -c",       "清除工具代理配置");
        Row("--all",             "操作所有已安装工具");
        Row("--except <names>", "排除指定工具（配合 --all 使用，逗号分隔）");
        Row("--list, -l",        "列出所有支持的工具（同 gate apps）");
        Sub("示例");
        Eg("gate app git");
        Note("    → 查看 git 当前代理配置");
        Ln();
        Eg("gate app git http://127.0.0.1:7890");
        Note("    → 为 git 设置代理");
        Ln();
        Eg("gate app git --clear");
        Note("    → 清除 git 代理");
        Ln();
        Eg("gate app git,npm,pip http://proxy:8080");
        Note("    → 批量为 3 个工具设置相同代理");
        Ln();
        Eg("gate app --all http://127.0.0.1:7890");
        Note("    → 为所有已安装工具设置代理");
        Ln();
        Eg("gate app --all http://127.0.0.1:7890 --except git,npm");
        Note("    → 为所有已安装工具设置代理，排除 git 和 npm");
        Ln();
        Eg("gate app --all --clear");
        Note("    → 清除所有已安装工具的代理");
        Ln();
        Note("提示：运行 `gate apps` 查看所有支持工具的完整列表。");
        Ln();
    }

    // ── gate apps ─────────────────────────────────────────────────────────────
    private static void PrintApps()
    {
        H1("gate apps — 列出所有支持的工具");
        Sub("用法");
        Eg("gate apps [--installed]");
        Sub("选项");
        Row("--installed, -i", "只显示已安装的工具");
        Sub("示例");
        Eg("gate apps");
        Note("    → 列出全部支持工具（按分类，显示安装状态和代理配置）");
        Ln();
        Eg("gate apps --installed");
        Note("    → 只显示已安装的工具");
        Ln();
        Sub("支持工具分类（部分）");
        Note("  VCS       git, mercurial, svn");
        Note("  Node.js   npm, yarn, pnpm, bun");
        Note("  Python    pip, poetry, conda, uv");
        Note("  Go/Rust   go, cargo");
        Note("  Java      maven, gradle");
        Note("  .NET      nuget, dotnet");
        Note("  Container docker, podman, helm, kubectl");
        Note("  Cloud     aws, gcloud, az");
        Note("  AI/IDE    cursor, vscode, ollama");
        Note("  ... 共 130+ 工具");
        Ln();
    }

    // ── gate env ──────────────────────────────────────────────────────────────
    private static void PrintEnv()
    {
        H1("gate env — 查看环境变量代理");
        Note("显示 Machine / User / Process 三层代理及生效值");
        Sub("用法");
        Eg("gate env");
        Eg("gate env --write-registry   # Windows: 写入系统代理（注册表）");
        Sub("选项");
        Row("--write-registry", "将当前用户级代理写入 Windows 注册表系统代理（Internet 选项）[仅 Windows]");
        Sub("说明");
        Note("• 不带选项时显示三层代理详情及最终生效值（优先级: 进程>用户>系统）。");
        Note("• 使用 gate set <proxy> 设置全局代理。");
        Note("• 使用 gate clear 清除全局代理。");
        Note("• --write-registry 需先用 gate set 设置用户级代理。");
        Ln();
    }

    // ── gate preset ───────────────────────────────────────────────────────────
    private static void PrintPreset()
    {
        H1("gate preset — 管理预设配置集");
        Sub("用法");
        Eg("gate preset                          # 列出所有预设");
        Eg("gate preset save <name>             # 保存当前配置为预设");
        Eg("gate preset load <name>             # 加载并应用预设");
        Eg("gate preset del <name>              # 删除预设");
        Eg("gate preset set-default <name>      # 设置默认预设");
        Eg("gate preset rename <old> <new>       # 重命名预设");
        Eg("gate preset export <name> [<file>]  # 导出预设到文件");
        Eg("gate preset import <file> [--as <name>] # 从文件导入预设");
        Sub("示例");
        Eg("gate preset save office");
        Note("    → 将当前全局代理 + 工具配置保存为 office 预设");
        Ln();
        Eg("gate preset load office");
        Note("    → 加载并应用 office 预设");
        Ln();
        Eg("gate preset rename office work");
        Note("    → 将 office 预设重命名为 work");
        Ln();
        Eg("gate preset export office office.preset.json");
        Note("    → 导出预设到文件（用于迁移到另一台机器）");
        Ln();
        Eg("gate preset import office.preset.json --as myoffice");
        Note("    → 从文件导入预设，命名为 myoffice");
        Ln();
        Sub("说明");
        Note("• 预设保存位置：%APPDATA%/ProxyTool/profiles/");
        Note("• 预设内容：全局代理（HTTP/HTTPS/NO_PROXY）+ 所有已配置工具的代理。");
        Note("• del 别名：delete、remove；set-default 别名：default。");
        Ln();
    }

    // ── gate test ─────────────────────────────────────────────────────────────
    private static void PrintTest()
    {
        H1("gate test — 测试代理连通性");
        Note("别名：check");
        Sub("用法");
        Eg("gate test [<proxy>] [--url <url>] [--compare <proxy1> <proxy2> ...]");
        Sub("参数");
        Row("[<proxy>]", "代理地址（省略则使用当前环境变量中的代理）", 16);
        Sub("选项");
        Row("--url <url>",    "自定义测试目标 URL（默认: http://www.google.com）");
        Row("--compare",      "对比测试多个代理，输出延迟排名（可多次指定或逗号分隔）");
        Sub("示例");
        Eg("gate test");
        Note("    → 测试当前环境变量中配置的代理");
        Ln();
        Eg("gate test http://127.0.0.1:7890");
        Note("    → 测试指定代理地址");
        Ln();
        Eg("gate test http://proxy:8080 --url https://github.com");
        Note("    → 测试代理能否访问 GitHub");
        Ln();
        Eg("gate test --compare http://proxy1:7890 http://proxy2:8080");
        Note("    → 对比两个代理的连通性和延迟，输出排名");
        Ln();
        Sub("说明");
        Note("• 显示 HTTP 状态码和响应时间（ms）。");
        Note("• 连接超时默认 10 秒。");
        Note("• --compare 模式输出按延迟升序排列，并标出最快可用代理。");
        Ln();
    }

    // ── gate list ─────────────────────────────────────────────────────────────
    private static void PrintList()
    {
        H1("gate list — 列出工具或预设");
        Sub("用法");
        Eg("gate list [resource] [--installed]");
        Sub("参数");
        Row("apps",    "列出所有支持的工具（含安装状态和代理配置）", 12);
        Row("presets", "列出所有已保存的预设", 12);
        Note("    （省略时显示工具分类概览 + 预设列表）");
        Sub("选项");
        Row("--installed, -i", "仅显示已安装的工具（配合 apps 使用）");
        Sub("示例");
        Eg("gate list");
        Note("    → 工具分类概览 + 预设列表");
        Ln();
        Eg("gate list apps");
        Note("    → 完整工具列表");
        Ln();
        Eg("gate list apps --installed");
        Note("    → 只显示已安装的工具");
        Ln();
        Eg("gate list presets");
        Note("    → 列出所有预设");
        Ln();
    }

    // ── gate info ─────────────────────────────────────────────────────────────
    private static void PrintInfo()
    {
        H1("gate info — 代理状态总览");
        Sub("用法");
        Eg("gate info");
        Sub("输出内容");
        Note("  1. 全局代理（HTTP_PROXY / HTTPS_PROXY / NO_PROXY）");
        Note("  2. 应用代理配置（按分类列出所有已设代理的工具）");
        Note("  3. 预设列表（所有已保存的预设）");
        Ln();
        Note("提示：无参数直接运行 `gate` 也会显示此总览。");
        Ln();
    }

    // ── gate path ─────────────────────────────────────────────────────────────
    private static void PrintPath()
    {
        H1("gate path — 工具自定义路径");
        Note("为非标准安装位置的工具手动指定可执行文件或配置文件路径");
        Sub("用法");
        Eg("gate path [选项]");
        Sub("选项");
        Row("-n, --name <工具名>", "要配置的工具名称");
        Row("--exec <路径>",       "工具可执行文件的绝对路径");
        Row("--config <路径>",     "工具配置文件的绝对路径");
        Row("--clear",             "清除自定义路径，恢复自动检测");
        Row("-l, --list",          "列出所有已自定义路径的工具");
        Sub("示例");
        Eg("gate path -l");
        Note("    → 列出所有已手动配置路径的工具");
        Ln();
        Eg("gate path -n git --exec /usr/local/bin/git");
        Note("    → 指定 git 可执行文件路径");
        Ln();
        Eg("gate path -n git --clear");
        Note("    → 清除自定义路径，恢复 PATH 自动检测");
        Ln();
    }

    // ── gate wizard ───────────────────────────────────────────────────────────
    private static void PrintWizard()
    {
        H1("gate wizard — 交互式配置向导");
        Note("逐步引导完成代理配置，适合首次使用");
        Sub("用法");
        Eg("gate wizard");
        Sub("向导步骤");
        Note("  第 1/4 步  输入全局代理地址（可选测试连通性）");
        Note("  第 2/4 步  选择要配置的工具（支持 all 全选已安装工具）");
        Note("  第 3/4 步  设置 NO_PROXY 排除列表");
        Note("  第 4/4 步  保存为预设（可选）");
        Sub("说明");
        Note("• 每步均可按 Enter 跳过。");
        Note("• 输入 all 可一次性为所有已安装工具配置代理。");
        Note("• 完成后运行 `gate` 查看配置结果。");
        Ln();
    }

    // ── gate apply (hidden legacy) ────────────────────────────────────────────
    private static void PrintApply()
    {
        H1("gate apply — 应用预设（旧命令）");
        Note("等价于 gate preset load <name>");
        Sub("用法");
        Eg("gate apply <预设名称>");
        Eg("gate preset load <预设名称>   # 推荐新写法");
        Ln();
    }

    // ── gate reset ────────────────────────────────────────────────────────────
    private static void PrintReset()
    {
        H1("gate reset — 完全重置所有配置");
        Note("清除全局代理、所有工具代理、预设和自定义路径");
        Sub("用法");
        Eg("gate reset [--force]");
        Sub("选项");
        Row("--force, -f", "跳过确认提示，直接执行");
        Sub("示例");
        Eg("gate reset");
        Note("    → 交互式确认后重置所有配置");
        Ln();
        Eg("gate reset --force");
        Note("    → 跳过确认直接重置");
        Ln();
        Sub("说明");
        Note("• 此操作不可撤销，建议先运行 gate preset save <name> 备份配置。");
        Note("• 重置后运行 gate wizard 重新配置。");
        Ln();
    }

    // ── gate doctor ───────────────────────────────────────────────────────────
    private static void PrintDoctor()
    {
        H1("gate doctor — 自动诊断");
        Note("检测工具路径、配置文件权限、预设完整性");
        Sub("用法");
        Eg("gate doctor");
        Sub("检测内容");
        Note("  1. 全局代理设置状态");
        Note("  2. 工具自定义路径有效性（exec 文件是否存在）");
        Note("  3. 预设文件完整性（JSON 是否可解析）");
        Note("  4. 已安装工具统计");
        Sub("示例");
        Eg("gate doctor");
        Note("    → 输出诊断报告，并在发现问题时给出修复建议");
        Ln();
    }
}
