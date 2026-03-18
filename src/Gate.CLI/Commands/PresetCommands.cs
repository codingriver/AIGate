using System;
using System.CommandLine;
using System.Threading.Tasks;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.Models;
using Gate.UI;

namespace Gate.CLI.Commands;

public static class PresetCommands
{
    public static Command Build()
    {
        var cmd       = new Command("preset", "管理预设配置集（保存/加载/删除）");
        var subArg    = new Argument<string?>("action", () => null, "save | load | del | rename | export | import | set-default");
        var nameArg   = new Argument<string?>("name",   () => null, "预设名称");
        // legacy hidden options
        var legN   = new Option<string?>(new[]{"--name","-n"}, "[旧]") { IsHidden = true };
        var legSave= new Option<bool>("--save",         "[旧]") { IsHidden = true };
        var legLoad= new Option<bool>("--load",         "[旧]") { IsHidden = true };
        var legDel = new Option<bool>("--delete",       "[旧]") { IsHidden = true };
        var legDef = new Option<bool>("--set-default",  "[旧]") { IsHidden = true };
        var legList= new Option<bool>(new[]{"-l","--list"}, "[旧]") { IsHidden = true };
        cmd.AddArgument(subArg); cmd.AddArgument(nameArg);
        cmd.AddOption(legN); cmd.AddOption(legSave); cmd.AddOption(legLoad);
        cmd.AddOption(legDel); cmd.AddOption(legDef); cmd.AddOption(legList);

        // rename sub-command
        var renameCmd = new Command("rename", "重命名预设");
        var renameOld = new Argument<string>("old"); var renameNew = new Argument<string>("new");
        renameCmd.AddArgument(renameOld); renameCmd.AddArgument(renameNew);
        renameCmd.SetHandler((string o, string n) =>
        {
            var p = ProfileManager.Load(o);
            if (p == null) { ConsoleStyle.Error($"预设 '{o}' 不存在"); return; }
            if (ProfileManager.Load(n) != null) { ConsoleStyle.Error($"预设 '{n}' 已存在"); return; }
            p.Name = n; ProfileManager.Save(p); ProfileManager.Delete(o);
            ConsoleStyle.Success($"预设已重命名: '{o}' → '{n}'");
        }, renameOld, renameNew);
        cmd.AddCommand(renameCmd);

        // export sub-command
        var exportCmd  = new Command("export", "导出预设到文件");
        var exportName = new Argument<string>("name");
        var exportFile = new Argument<string?>("file", () => null);
        exportCmd.AddArgument(exportName); exportCmd.AddArgument(exportFile);
        exportCmd.SetHandler((string n, string? f) =>
        {
            var p = ProfileManager.Load(n);
            if (p == null) { ConsoleStyle.Error($"预设 '{n}' 不存在"); return; }
            var outFile = f ?? $"{n}.preset.json";
            System.IO.File.WriteAllText(outFile,
                System.Text.Json.JsonSerializer.Serialize(p,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
            ConsoleStyle.Success($"预设 '{n}' 已导出到: {System.IO.Path.GetFullPath(outFile)}");
            ConsoleStyle.Info("下一步：使用 `gate preset import <file>` 在另一台机器导入。");
        }, exportName, exportFile);
        cmd.AddCommand(exportCmd);

        // import sub-command
        var importCmd  = new Command("import", "从文件导入预设");
        var importFile = new Argument<string>("file");
        var importAs   = new Option<string?>("--as", "导入后使用的预设名称");
        importCmd.AddArgument(importFile); importCmd.AddOption(importAs);
        importCmd.SetHandler((string f, string? asName) =>
        {
            if (!System.IO.File.Exists(f)) { ConsoleStyle.Error($"文件不存在: {f}"); return; }
            try
            {
                var json = System.IO.File.ReadAllText(f);
                var p = System.Text.Json.JsonSerializer.Deserialize<Profile>(json,
                    new System.Text.Json.JsonSerializerOptions
                    { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                if (p == null) { ConsoleStyle.Error("文件格式无效"); return; }
                if (!string.IsNullOrEmpty(asName)) p.Name = asName;
                if (ProfileManager.Load(p.Name) != null)
                {
                    Console.Write($"预设 '{p.Name}' 已存在，覆盖? [y/N]: ");
                    if ((Console.ReadLine()?.Trim().ToLowerInvariant() ?? "") != "y")
                    { ConsoleStyle.Info("已取消"); return; }
                }
                ProfileManager.Save(p);
                ConsoleStyle.Success($"预设 '{p.Name}' 已导入（{p.ToolConfigs.Count} 个工具配置）");
            }
            catch (Exception ex) { ConsoleStyle.Error($"导入失败: {ex.Message}"); }
        }, importFile, importAs);
        cmd.AddCommand(importCmd);

        cmd.SetHandler((string? action, string? name,
                        string? legacyN, bool legacySave, bool legacyLoad,
                        bool legacyDel, bool legacyDef, bool legacyList) =>
        {
            var resolvedName = name ?? legacyN;
            var resolvedAct  = action;
            if (resolvedAct == null)
            {
                if (legacySave) resolvedAct = "save";
                else if (legacyLoad) resolvedAct = "load";
                else if (legacyDel)  resolvedAct = "del";
                else if (legacyDef)  resolvedAct = "set-default";
                else if (legacyList) resolvedAct = "list";
            }
            HandlePreset(resolvedAct, resolvedName);
        }, subArg, nameArg, legN, legSave, legLoad, legDel, legDef, legList);

        return cmd;
    }

    public static void HandlePreset(string? action, string? name)
    {
        switch (action?.ToLowerInvariant())
        {
            case "save":
                if (string.IsNullOrEmpty(name))
                { ConsoleStyle.Warning("请指定预设名称。用法：gate preset save <name>"); return; }
                var envCfg = EnvVarManager.GetProxyConfig(EnvLevel.User);
                var pSave  = new Profile
                {
                    Name        = name,
                    Description = $"保存于 {DateTime.Now:yyyy-MM-dd HH:mm}",
                    EnvVars     = envCfg
                };
                // 同时保存所有已配置工具的代理
                foreach (var t in ToolRegistry.GetAllTools())
                {
                    if (!t.IsInstalled()) continue;
                    var tcfg = t.GetCurrentConfig();
                    if (tcfg != null && !tcfg.IsEmpty) pSave.ToolConfigs[t.ToolName] = tcfg;
                }
                ProfileManager.Save(pSave);
                ConsoleStyle.Success($"预设 '{name}' 已保存（{pSave.ToolConfigs.Count} 个工具配置）");
                break;

            case "load":
                if (string.IsNullOrEmpty(name))
                { ConsoleStyle.Warning("请指定预设名称。用法：gate preset load <name>"); return; }
                ApplyPreset(name);
                break;

            case "del": case "delete": case "remove":
                if (string.IsNullOrEmpty(name))
                { ConsoleStyle.Warning("请指定预设名称。"); return; }
                if (ProfileManager.Delete(name)) ConsoleStyle.Success($"预设 '{name}' 已删除");
                else ConsoleStyle.Error($"预设 '{name}' 不存在或删除失败");
                break;

            case "set-default": case "default":
                if (string.IsNullOrEmpty(name))
                { ConsoleStyle.Warning("请指定预设名称。"); return; }
                if (ProfileManager.Load(name) == null)
                { ConsoleStyle.Error($"预设 '{name}' 不存在"); return; }
                ProfileManager.SetDefaultProfile(name);
                ConsoleStyle.Success($"默认预设已设为 '{name}'");
                break;

            default:
                StatusPrinter.PrintPresetList();
                break;
        }
    }

    /// <summary>
    /// 加载预设：同时恢复全局代理 AND 所有工具代理配置文件（修复原有只恢复环境变量的问题）
    /// </summary>
    public static void ApplyPreset(string name)
    {
        var profile = ProfileManager.Load(name);
        if (profile == null) { ConsoleStyle.Error($"预设不存在: {name}"); return; }

        // 1. 恢复全局环境变量
        EnvVarManager.SetProxyForCurrentProcess(profile.EnvVars);
        ConsoleStyle.Success($"预设 '{name}' 全局代理已恢复");
        StatusPrinter.PrintProxyTable(profile.EnvVars);

        // 2. 同时写回所有工具配置文件（修复旧版只恢复环境变量的缺陷）
        var restored = 0; var skipped = 0;
        foreach (var kv in profile.ToolConfigs)
        {
            var tool = ToolRegistry.GetByName(kv.Key);
            if (tool == null || !tool.IsInstalled()) { skipped++; continue; }
            var proxy = kv.Value.HttpProxy ?? kv.Value.HttpsProxy;
            if (!string.IsNullOrEmpty(proxy) && tool.SetProxy(proxy)) restored++;
            else skipped++;
        }
        if (profile.ToolConfigs.Count > 0)
            ConsoleStyle.Info($"  工具代理已恢复 {restored} 个，跳过 {skipped} 个（未安装）");
        ConsoleStyle.Info("运行 `gate` 查看完整状态。");
    }
}
