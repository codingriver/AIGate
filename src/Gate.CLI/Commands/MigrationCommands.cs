using System;
using System.CommandLine;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.UI;

namespace Gate.CLI.Commands;

public static class MigrationCommands
{
    public static Command BuildExportAll()
    {
        var cmd     = new Command("export-all",
            "导出所有配置（全局代理+预设+工具路径）到单个 JSON 文件");
        var fileArg = new Argument<string?>("file", () => null,
            "输出文件路径（默认 gate-backup.json）");
        cmd.AddArgument(fileArg);
        cmd.SetHandler((string? f) =>
        {
            var outFile = f ?? "gate-backup.json";
            try
            {
                ConfigMigration.ExportAll(outFile);
                ConsoleStyle.Success(
                    $"配置已导出到: {System.IO.Path.GetFullPath(outFile)}");
                ConsoleStyle.Info(
                    "在新机器上运行 `gate import-all <file>` 恢复配置。");
            }
            catch (Exception ex)
            {
                ConsoleStyle.Error($"导出失败: {ex.Message}");
            }
        }, fileArg);
        return cmd;
    }

    public static Command BuildImportAll()
    {
        var cmd        = new Command("import-all",
            "从备份文件导入所有配置（全局代理+预设+工具路径）");
        var fileArg    = new Argument<string>("file", "备份文件路径（由 gate export-all 生成）");
        var overwriteO = new Option<bool>(
            new[]{"--overwrite","-f"}, "同名预设存在时直接覆盖（默认跳过）");
        cmd.AddArgument(fileArg);
        cmd.AddOption(overwriteO);
        cmd.SetHandler((string f, bool overwrite) =>
        {
            try
            {
                var (profiles, paths, hasGlobal) =
                    ConfigMigration.ImportAll(f, overwrite);
                if (hasGlobal)  ConsoleStyle.Success("全局代理已恢复");
                ConsoleStyle.Success($"预设已导入: {profiles} 个");
                ConsoleStyle.Success($"工具路径已导入: {paths} 个");
                ConsoleStyle.Info("运行 `gate` 查看完整状态。");
            }
            catch (Exception ex)
            {
                ConsoleStyle.Error($"导入失败: {ex.Message}");
            }
        }, fileArg, overwriteO);
        return cmd;
    }
}
