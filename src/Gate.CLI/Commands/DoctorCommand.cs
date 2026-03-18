using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.UI;

namespace Gate.CLI.Commands;

/// <summary>
/// gate doctor — 增强版诊断（配置文件读写权限 + 持久化检测 + 代理连通性）
/// </summary>
public static class DoctorCommand
{
    public static Command Build()
    {
        var cmd = new Command("doctor",
            "诊断 Gate 配置、工具路径、代理连通性和存储权限");
        var verboseOpt = new Option<bool>(
            new[]{"--verbose","-v"}, "显示所有检查项（包括通过项）");
        cmd.AddOption(verboseOpt);
        cmd.SetHandler((bool verbose) => RunDoctor(verbose), verboseOpt);
        return cmd;
    }

    private static void RunDoctor(bool verbose)
    {
        ConsoleStyle.Title("Gate 诊断报告");
        var issues = new List<string>();

        // ── 1. 数据目录权限 ───────────────────────────────────────────────────
        ConsoleStyle.Subtitle("[存储目录]");
        CheckDir(GatePaths.DataDir,    "数据目录",    issues, verbose);
        CheckDir(GatePaths.ProfilesDir,"预设目录",    issues, verbose);
        CheckDir(GatePaths.PluginsDir, "插件目录",    issues, verbose);
        CheckDir(GatePaths.AuditDir,   "审计日志目录", issues, verbose);

        // ── 2. 工具配置文件读写权限 ────────────────────────────────────────────
        ConsoleStyle.Subtitle("[工具配置文件]");
        foreach (var tool in ToolRegistry.GetAllTools().Where(t => t.IsInstalled()))
        {
            var configPath = tool.ConfigPath;
            if (string.IsNullOrEmpty(configPath))
            {
                if (verbose) ConsoleStyle.Info($"  {tool.ToolName,-22} 配置路径未知（跳过）");
                continue;
            }
            if (File.Exists(configPath))
            {
                try
                {
                    var tmp = configPath + ".gate_write_test";
                    File.WriteAllText(tmp, "");
                    File.Delete(tmp);
                    if (verbose)
                        ConsoleStyle.Success($"  {tool.ToolName,-22} {ShortenPath(configPath)}");
                }
                catch
                {
                    issues.Add($"{tool.ToolName} 配置文件不可写: {configPath}");
                    ConsoleStyle.Error($"  {tool.ToolName,-22} [只读] {ShortenPath(configPath)}");
                }
            }
            else if (verbose)
            {
                Console.WriteLine($"  {tool.ToolName,-22} [不存在] {ShortenPath(configPath)}");
            }
        }

        // ── 3. 环境变量持久化检测 ────────────────────────────────────────────
        ConsoleStyle.Subtitle("[环境变量持久化]");
        var (_, user, _) = EnvVarManager.GetProxyConfigAllLevels();
        if (!string.IsNullOrEmpty(user.HttpProxy))
        {
            if (verbose)
                ConsoleStyle.Success($"  用户级 HTTP_PROXY 已设置: {user.HttpProxy}");
        }
        else
        {
            ConsoleStyle.Warning("  用户级 HTTP_PROXY 未设置（重新打开终端后代理会消失）");
            ConsoleStyle.Info("    提示：运行 `gate install-shell-hook` 实现持久化。");
        }

        // ── 4. Shell hook 检测 ───────────────────────────────────────────────
        ConsoleStyle.Subtitle("[Shell 启动钩子]");
        CheckShellHook(verbose);

        // ── 5. 预设完整性 ────────────────────────────────────────────────────
        ConsoleStyle.Subtitle("[预设完整性]");
        var presets = ProfileManager.List();
        if (presets.Count == 0 && verbose)
            ConsoleStyle.Info("  暂无已保存的预设。");
        foreach (var name in presets)
        {
            var p = ProfileManager.Load(name);
            if (p == null)
            {
                issues.Add($"预设文件损坏: {name}");
                ConsoleStyle.Error($"  {name}: 文件损坏，无法读取");
            }
            else if (verbose)
                ConsoleStyle.Success($"  {name}: OK ({p.ToolConfigs.Count} 个工具配置)");
        }

        // ── 6. 自定义路径有效性 ───────────────────────────────────────────────
        ConsoleStyle.Subtitle("[自定义工具路径]");
        var customPaths = ToolRegistry.GetCustomPaths();
        if (customPaths.Count == 0 && verbose)
            ConsoleStyle.Info("  无自定义路径。");
        foreach (var kv in customPaths)
        {
            if (!string.IsNullOrEmpty(kv.Value.Exec) && !File.Exists(kv.Value.Exec))
            {
                issues.Add($"自定义 exec 路径不存在: {kv.Key} → {kv.Value.Exec}");
                ConsoleStyle.Error($"  {kv.Key}: exec 不存在: {kv.Value.Exec}");
            }
            else if (verbose)
                ConsoleStyle.Success($"  {kv.Key}: OK");
        }

        // ── 汇总 ──────────────────────────────────────────────────────────────
        Console.WriteLine();
        if (issues.Count == 0)
            ConsoleStyle.Success($"诊断通过（{ToolRegistry.GetInstalledTools().Count} 个工具已安装）");
        else
        {
            ConsoleStyle.Warning($"发现 {issues.Count} 个问题：");
            foreach (var iss in issues)
                ConsoleStyle.Error($"  • {iss}");
        }
        Console.WriteLine();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static void CheckDir(
        string dir, string label, List<string> issues, bool verbose)
    {
        GatePaths.EnsureDir(dir);
        try
        {
            var tmp = Path.Combine(dir, ".gate_write_test");
            File.WriteAllText(tmp, "");
            File.Delete(tmp);
            if (verbose) ConsoleStyle.Success($"  {label,-12} {dir}");
        }
        catch
        {
            issues.Add($"{label} 不可写: {dir}");
            ConsoleStyle.Error($"  {label,-12} [不可写] {dir}");
        }
    }

    private static void CheckShellHook(bool verbose)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var psProfile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PowerShell", "Microsoft.PowerShell_profile.ps1");
            var hasHook = File.Exists(psProfile) &&
                          File.ReadAllText(psProfile).Contains("gate preset load");
            if (hasHook)
            { if (verbose) ConsoleStyle.Success("  PowerShell profile: gate hook 已配置"); }
            else
                ConsoleStyle.Warning(
                    "  PowerShell profile: 未配置 gate hook，运行 `gate install-shell-hook` 配置");
            return;
        }
        var home  = Environment.GetEnvironmentVariable("HOME") ?? "";
        var files = new[] { Path.Combine(home, ".bashrc"),
                            Path.Combine(home, ".zshrc"),
                            Path.Combine(home, ".config", "fish", "config.fish") };
        var found = false;
        foreach (var f in files)
            if (File.Exists(f) && File.ReadAllText(f).Contains("gate preset load"))
            {
                found = true;
                if (verbose) ConsoleStyle.Success($"  gate hook 已配置: {f}");
            }
        if (!found)
            ConsoleStyle.Warning(
                "  未检测到 gate shell hook，运行 `gate install-shell-hook` 配置持久化代理。");
    }

    private static string ShortenPath(string path)
    {
        var home = Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
        return !string.IsNullOrEmpty(home) && path.StartsWith(home)
            ? "~" + path.Substring(home.Length)
            : path;
    }
}
