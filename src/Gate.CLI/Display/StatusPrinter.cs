using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Gate.Managers;
using Gate.Models;
using Gate.UI;

namespace Gate.CLI.Display;

/// <summary>
/// 集中管理所有状态/列表/预设的打印逻辑，支持 Normal / Json / Plain / NoColor 模式。
/// </summary>
public static class StatusPrinter
{
    // ── 状态总览 ──────────────────────────────────────────────────────────────

    public static void PrintStatusOverview()
    {
        if (OutputSettings.IsJson)
        {
            var obj = new
            {
                globalProxy = GetGlobalProxyObj(),
                toolProxies = GetToolProxiesObj(),
                presets     = ProfileManager.List()
            };
            Console.WriteLine(JsonSerializer.Serialize(obj,
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        ConsoleStyle.Title("全局代理 (Global Proxy / Env Vars)");
        PrintProxyTable(EnvVarManager.GetProxyConfig(EnvLevel.User));

        ConsoleStyle.Title("应用代理配置 (App Proxy Config)");
        var anyTool = false;
        foreach (var cat in ToolRegistry.GetCategories())
        {
            var catTools = ToolRegistry.GetByCategory(cat)
                .Where(t => { var c = t.GetCurrentConfig(); return c != null && !c.IsEmpty; })
                .ToList();
            if (catTools.Count == 0) continue;
            anyTool = true;
            ConsoleStyle.Subtitle($"  [{cat}]");
            foreach (var tool in catTools)
            {
                var inst = tool.IsInstalled() ? "[OK] " : "[?]  ";
                Console.WriteLine($"    {inst} {tool.ToolName,-22}  {tool.GetCurrentConfig()}");
            }
        }
        if (!anyTool) ConsoleStyle.Info("  暂无应用代理配置。");

        ConsoleStyle.Title("预设 (Presets)");
        PrintPresetList();
        ConsoleStyle.Info("运行 `gate -h` 查看所有命令。");
    }

    // ── 三层环境变量详情 ──────────────────────────────────────────────────────

    public static void PrintProxyLayers()
    {
        if (OutputSettings.IsJson)
        {
            var (machine, user, process) = EnvVarManager.GetProxyConfigAllLevels();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                machine = new { httpProxy = machine.HttpProxy, httpsProxy = machine.HttpsProxy, noProxy = machine.NoProxy },
                user    = new { httpProxy = user.HttpProxy,    httpsProxy = user.HttpsProxy,    noProxy = user.NoProxy },
                process = new { httpProxy = process.HttpProxy, httpsProxy = process.HttpsProxy, noProxy = process.NoProxy },
                effective = new
                {
                    httpProxy  = process.HttpProxy  ?? user.HttpProxy  ?? machine.HttpProxy,
                    httpsProxy = process.HttpsProxy ?? user.HttpsProxy ?? machine.HttpsProxy,
                    noProxy    = process.NoProxy    ?? user.NoProxy    ?? machine.NoProxy
                }
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        var (m, u, p) = EnvVarManager.GetProxyConfigAllLevels();
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("当前全局代理 (Global Proxy)");
        sb.AppendLine(new string('─', 37));

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
                if (key != null)
                {
                    var enabled = (int)(key.GetValue("ProxyEnable") ?? 0) != 0;
                    var server  = key.GetValue("ProxyServer")   as string;
                    var bypass  = key.GetValue("ProxyOverride") as string;
                    sb.AppendLine();
                    sb.AppendLine("  [Windows 系统代理 (注册表)]");
                    sb.AppendLine($"    状态         : {(enabled ? "已开启" : "已关闭")}");
                    sb.AppendLine($"    ProxyServer  : {server  ?? "(not set)"}");
                    sb.AppendLine($"    ProxyOverride: {bypass  ?? "(not set)"}");
                }
            }
            catch { }
        }

        sb.AppendLine();
        sb.AppendLine("  [系统级 Machine]");
        sb.AppendLine($"    HTTP_PROXY  : {m.HttpProxy  ?? "(not set)"}");
        sb.AppendLine($"    HTTPS_PROXY : {m.HttpsProxy ?? "(not set)"}");
        sb.AppendLine($"    NO_PROXY    : {m.NoProxy    ?? "(not set)"}");
        sb.AppendLine();
        sb.AppendLine("  [用户级 User]");
        sb.AppendLine($"    HTTP_PROXY  : {u.HttpProxy  ?? "(not set)"}");
        sb.AppendLine($"    HTTPS_PROXY : {u.HttpsProxy ?? "(not set)"}");
        sb.AppendLine($"    NO_PROXY    : {u.NoProxy    ?? "(not set)"}");
        sb.AppendLine();
        sb.AppendLine("  [进程级 Process]");
        sb.AppendLine($"    HTTP_PROXY  : {p.HttpProxy  ?? "(not set)"}");
        sb.AppendLine($"    HTTPS_PROXY : {p.HttpsProxy ?? "(not set)"}");
        sb.AppendLine($"    NO_PROXY    : {p.NoProxy    ?? "(not set)"}");
        sb.AppendLine();
        sb.AppendLine("  " + new string('─', 35));
        sb.AppendLine("  [生效值 (进程>用户>系统)]");

        void AppendEffective(string label, string? proc, string? usr, string? sys)
        {
            var val = proc ?? usr ?? sys;
            if (val == null) sb.AppendLine($"    {label} : (not set)");
            else
            {
                var src = proc != null ? "进程级" : usr != null ? "用户级" : "系统级";
                sb.AppendLine($"    {label} : {val}  \u2190 {src}");
            }
        }

        AppendEffective("HTTP_PROXY ", p.HttpProxy,  u.HttpProxy,  m.HttpProxy);
        AppendEffective("HTTPS_PROXY", p.HttpsProxy, u.HttpsProxy, m.HttpsProxy);
        AppendEffective("NO_PROXY   ", p.NoProxy,    u.NoProxy,    m.NoProxy);
        sb.AppendLine();
        sb.AppendLine("[INFO] 使用 `gate set <proxy>` 设置全局代理，`gate clear` 清除。");
        Console.Write(sb);
        Console.Out.Flush();
    }

    // ── 工具列表 ──────────────────────────────────────────────────────────────

    public static void PrintToolList(bool installedOnly = false)
    {
        var allTools  = ToolRegistry.GetAllTools();
        var toolInfos = allTools
            .Select(t => { var inst = t.IsInstalled(); var cfg = inst ? t.GetCurrentConfig() : null;
                return (Tool: t, Installed: inst, HasCfg: cfg != null && !cfg.IsEmpty); })
            .Where(x => !installedOnly || x.Installed).ToList();

        if (OutputSettings.IsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                toolInfos.Select(x => new
                {
                    name      = x.Tool.ToolName,
                    category  = x.Tool.Category,
                    installed = x.Installed,
                    proxySet  = x.HasCfg
                }),
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        var instCount = toolInfos.Count(x => x.Installed);
        var cfgCount  = toolInfos.Count(x => x.HasCfg);
        var sb        = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"支持的应用列表  [{instCount}/{allTools.Count} installed, {cfgCount} configured]");
        sb.AppendLine(new string('─', 60));
        foreach (var cat in ToolRegistry.GetCategories())
        {
            var catInfos = toolInfos.Where(x => x.Tool.Category == cat).ToList();
            if (catInfos.Count == 0) continue;
            var catInst = catInfos.Count(x => x.Installed);
            sb.AppendLine();
            sb.AppendLine($"  [{cat}]  ({catInst}/{catInfos.Count})");
            foreach (var (tool, installed, hasCfg) in catInfos)
                sb.AppendLine($"    {tool.ToolName,-24} {(installed ? "[installed]" : "[not installed]"),-18} {(hasCfg ? "[proxy set]" : "[-]")}");
        }
        sb.AppendLine();
        sb.AppendLine($"[INFO] 共 {allTools.Count} 个工具，{instCount} 个已安装，{cfgCount} 个已配置代理。");
        if (!installedOnly) sb.AppendLine("[INFO] 使用 `gate apps --installed` 只显示已安装的工具。");
        Console.Write(sb);
        Console.Out.Flush();
    }

    // ── 工具概览 ──────────────────────────────────────────────────────────────

    public static void PrintToolSummary()
    {
        var allTools   = ToolRegistry.GetAllTools();
        var installMap = allTools.ToDictionary(t => t, t => t.IsInstalled());
        var instCount  = installMap.Values.Count(v => v);

        if (OutputSettings.IsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                ToolRegistry.GetCategories().Select(cat =>
                {
                    var tools = ToolRegistry.GetByCategory(cat);
                    var inst  = tools.Count(t => installMap.TryGetValue(t, out var v) && v);
                    return new { category = cat, total = tools.Count, installed = inst };
                }),
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        var sb  = new StringBuilder();
        var sep = new string('─', 44);
        sb.AppendLine();
        sb.AppendLine("支持的应用概览 (Apps Overview)");
        sb.AppendLine(new string('─', 32));
        sb.AppendLine($"  {"分类",-22} {"工具数",6}  {"已安装",8}");
        sb.AppendLine(sep);
        foreach (var cat in ToolRegistry.GetCategories())
        {
            var tools = ToolRegistry.GetByCategory(cat);
            var inst  = tools.Count(t => installMap.TryGetValue(t, out var v) && v);
            sb.AppendLine($"  {cat,-22} {tools.Count,6}  {inst,8}");
        }
        sb.AppendLine(sep);
        sb.AppendLine($"  {"合计",-22} {allTools.Count,6}  {instCount,8}");
        Console.Write(sb);
        Console.Out.Flush();
    }

    // ── 预设列表 ──────────────────────────────────────────────────────────────

    public static void PrintPresetList()
    {
        var profiles = ProfileManager.List();
        var def      = ProfileManager.GetDefaultProfile();

        if (OutputSettings.IsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                profiles.Select(p => new { name = p, isDefault = p == def }),
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("已保存的预设 (Saved Presets)");
        sb.AppendLine(new string('─', 24));
        if (profiles.Count == 0)
        {
            sb.AppendLine("[INFO]   暂无已保存的预设。");
            sb.AppendLine("[INFO]   运行 `gate preset save <name>` 保存当前配置。");
        }
        else
        {
            foreach (var p in profiles)
                sb.AppendLine($"  - {p}{(p == def ? " <- default" : "")}");
        }
        Console.Write(sb);
        Console.Out.Flush();
    }

    // ── 代理表 ────────────────────────────────────────────────────────────────

    public static void PrintProxyTable(ProxyConfig cfg)
    {
        ConsoleStyle.ListItem("HTTP_PROXY ", cfg.HttpProxy  ?? "(not set)");
        ConsoleStyle.ListItem("HTTPS_PROXY", cfg.HttpsProxy ?? "(not set)");
        ConsoleStyle.ListItem("NO_PROXY   ", cfg.NoProxy    ?? "(not set)");
    }

    // ── 历史记录 ──────────────────────────────────────────────────────────────

    public static void PrintHistory()
    {
        var list = ProxyHistory.Load();
        if (OutputSettings.IsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(list,
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }
        ConsoleStyle.Title("代理历史记录");
        if (list.Count == 0) { ConsoleStyle.Info("暂无历史记录。"); return; }
        for (int i = 0; i < list.Count; i++)
            Console.WriteLine($"  [{i + 1,2}] {list[i]}");
        ConsoleStyle.Info("使用 `gate set --from-history <N>` 快速应用。");
    }

    // ── JSON helpers ──────────────────────────────────────────────────────────

    private static object GetGlobalProxyObj()
    {
        var cfg = EnvVarManager.GetProxyConfig(EnvLevel.User);
        return new { httpProxy = cfg.HttpProxy, httpsProxy = cfg.HttpsProxy, noProxy = cfg.NoProxy };
    }

    private static object GetToolProxiesObj()
    {
        var dict = new Dictionary<string, object>();
        foreach (var cat in ToolRegistry.GetCategories())
        {
            foreach (var tool in ToolRegistry.GetByCategory(cat))
            {
                var cfg = tool.GetCurrentConfig();
                if (cfg != null && !cfg.IsEmpty)
                    dict[tool.ToolName] = new
                    {
                        category   = tool.Category,
                        installed  = tool.IsInstalled(),
                        httpProxy  = cfg.HttpProxy,
                        httpsProxy = cfg.HttpsProxy
                    };
            }
        }
        return dict;
    }
}
