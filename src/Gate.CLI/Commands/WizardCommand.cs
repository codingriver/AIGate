using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.UI;

namespace Gate.CLI.Commands;

/// <summary>
/// gate wizard — 交互式配置向导（步骤 2 改为编号菜单选择已安装工具）
/// </summary>
public static class WizardCommand
{
    public static Command Build()
    {
        var cmd = new Command("wizard", "交互式代理配置向导");
        cmd.SetHandler(RunWizard);
        return cmd;
    }

    private static void RunWizard()
    {
        ConsoleStyle.Title("Gate 代理配置向导");
        Console.WriteLine("  按 Ctrl+C 随时退出。\n");

        // ── 步骤 1: 输入代理地址 ──────────────────────────────────────────────
        Console.Write("  [1/4] 请输入代理地址 (如 http://127.0.0.1:7890): ");
        var proxyUrl = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(proxyUrl))
        {
            ConsoleStyle.Warning("代理地址为空，已退出向导。");
            return;
        }

        // ── 步骤 2: 选择要配置的工具（编号菜单） ─────────────────────────────
        Console.WriteLine();
        Console.WriteLine("  [2/4] 为哪些工具设置代理? (检测到以下已安装工具)");
        Console.WriteLine();

        var installed = ToolRegistry.GetAllTools()
            .Where(t => t.IsInstalled())
            .ToList();

        if (installed.Count == 0)
        {
            ConsoleStyle.Warning("未检测到已安装工具，将只设置全局环境变量代理。");
        }
        else
        {
            for (var i = 0; i < installed.Count; i++)
            {
                var t = installed[i];
                Console.WriteLine($"    [{i + 1,3}] {t.ToolName,-24} [{t.Category}]");
            }
            Console.WriteLine();
            Console.Write("  输入编号选择 (如 1,2,3)，all=全部，回车=跳过: ");
            var sel = Console.ReadLine()?.Trim() ?? "";

            List<int> selectedIndices;
            if (sel.Equals("all", StringComparison.OrdinalIgnoreCase))
                selectedIndices = Enumerable.Range(0, installed.Count).ToList();
            else
                selectedIndices = sel
                    .Split(new[]{',', ' '}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n - 1 : -1)
                    .Where(n => n >= 0 && n < installed.Count)
                    .Distinct().ToList();

            if (selectedIndices.Count > 0)
            {
                Console.WriteLine();
                foreach (var idx in selectedIndices)
                {
                    var t = installed[idx];
                    var ok = t.SetProxy(proxyUrl);
                    if (ok) ConsoleStyle.Success($"    {t.ToolName} 代理已设置");
                    else    ConsoleStyle.Warning($"    {t.ToolName} 设置失败");
                }
            }
        }

        // ── 步骤 3: 设置全局环境变量 ──────────────────────────────────────────
        Console.WriteLine();
        Console.Write("  [3/4] 是否设置全局环境变量代理 (HTTP_PROXY / HTTPS_PROXY)? [Y/n]: ");
        var setEnv = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "y";
        if (setEnv != "n")
        {
            EnvVarManager.SetProxyForCurrentProcess(
                new Gate.Models.ProxyConfig { HttpProxy = proxyUrl, HttpsProxy = proxyUrl });
            ProxyHistory.Push(proxyUrl);
            ConsoleStyle.Success($"    全局代理已设置: {proxyUrl}");
        }

        // ── 步骤 4: 保存为预设 ────────────────────────────────────────────────
        Console.WriteLine();
        Console.Write("  [4/4] 是否保存为预设? 输入预设名称 (回车跳过): ");
        var presetName = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(presetName))
            PresetCommands.HandlePreset("save", presetName);

        Console.WriteLine();
        ConsoleStyle.Success("向导完成！运行 `gate` 查看当前代理状态。");
        ConsoleStyle.Info("提示：运行 `gate install-shell-hook` 可让代理在新终端自动生效。");
    }
}
