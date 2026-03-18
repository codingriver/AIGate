using System;
using System.CommandLine;
using System.IO;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.UI;

namespace Gate.CLI.Commands;

public static class PluginCommands
{
    public static Command Build()
    {
        var cmd = new Command("plugin", "管理 Gate 工具插件（社区贡献的声明式工具配置）");

        // gate plugin list
        var listCmd = new Command("list", "列出已安装插件");
        listCmd.SetHandler(() =>
        {
            var installed = PluginManager.ListInstalled();
            ConsoleStyle.Title($"已安装插件 ({installed.Count} 个)");
            if (installed.Count == 0)
            {
                ConsoleStyle.Info("暂无已安装插件。");
                ConsoleStyle.Info("使用 `gate plugin install <path/to/tool.json>` 安装插件。");
                return;
            }
            foreach (var name in installed)
                Console.WriteLine($"  - {name}");
        });
        cmd.AddCommand(listCmd);

        // gate plugin install <file>
        var installCmd  = new Command("install", "从本地 tool.json 文件安装插件");
        var installFile = new Argument<string>("file", "插件 tool.json 文件路径");
        installCmd.AddArgument(installFile);
        installCmd.SetHandler((string f) =>
        {
            if (!File.Exists(f)) { ConsoleStyle.Error($"文件不存在: {f}"); return; }
            if (PluginManager.InstallFromFile(f, out var err))
                ConsoleStyle.Success($"插件已安装，运行 `gate apps` 查看。");
            else
                ConsoleStyle.Error($"安装失败: {err}");
        }, installFile);
        cmd.AddCommand(installCmd);

        // gate plugin remove <name>
        var removeCmd  = new Command("remove", "卸载插件");
        var removeName = new Argument<string>("name", "插件工具名称");
        removeCmd.AddArgument(removeName);
        removeCmd.SetHandler((string n) =>
        {
            if (PluginManager.Remove(n, out var err))
                ConsoleStyle.Success($"插件 '{n}' 已卸载。");
            else
                ConsoleStyle.Error($"卸载失败: {err}");
        }, removeName);
        cmd.AddCommand(removeCmd);

        // gate plugin validate <file>
        var validateCmd  = new Command("validate", "校验 tool.json 是否符合 Gate 插件规范");
        var validateFile = new Argument<string>("file", "插件 tool.json 文件路径");
        validateCmd.AddArgument(validateFile);
        validateCmd.SetHandler((string f) =>
        {
            if (PluginManager.ValidateFile(f, out var err))
                ConsoleStyle.Success($"{f}: 校验通过 ✓");
            else
                ConsoleStyle.Error($"{f}: 校验失败 — {err}");
        }, validateFile);
        cmd.AddCommand(validateCmd);

        return cmd;
    }
}
