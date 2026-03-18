using System;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gate.CLI.Commands;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.Models;
using Gate.UI;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding  = Encoding.UTF8;

// ── Pre-parse: global output flags ───────────────────────────────────────────
{
    var a = args.ToList();
    if (a.Remove("--json"))     { OutputSettings.Mode = OutputMode.Json;    args = a.ToArray(); }
    if (a.Remove("--quiet"))    { OutputSettings.Mode = OutputMode.Quiet;   args = a.ToArray(); }
    if (a.Remove("--no-color")) { OutputSettings.Mode = OutputMode.NoColor; args = a.ToArray(); }
    if (a.Remove("--plain"))    { OutputSettings.Mode = OutputMode.Plain;   args = a.ToArray(); }
    if (args.Length == 0) { StatusPrinter.PrintStatusOverview(); return 0; }
    if (args.Length == 1 && (args[0] is "-h" or "--help" or "help"))
    { HelpPrinter.Print(null); return 0; }
    for (var i = 1; i < Math.Min(args.Length, 3); i++)
        if (args[i] is "-h" or "--help") { HelpPrinter.Print(args[0]); return 0; }
}

var root = new RootCommand("Gate - 跨平台代理配置管理工具");
root.SetHandler(StatusPrinter.PrintStatusOverview);

// ── helpers ───────────────────────────────────────────────────────────────────
static async Task SetToolProxies(string names, string proxy)
{
    foreach (var n in names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var tool = ToolRegistry.GetByName(n);
        if (tool == null)        { ConsoleStyle.Error($"{n}: 未找到"); continue; }
        if (!tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 未安装，跳过"); continue; }
        if (tool.SetProxy(proxy)) ConsoleStyle.Success($"{n}: 代理已设置 -> {proxy}");
        else                      ConsoleStyle.Error($"{n}: 设置失败");
    }
    await Task.CompletedTask;
}
static void ClearToolProxies(string names)
{
    foreach (var n in names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var tool = ToolRegistry.GetByName(n);
        if (tool == null)        { ConsoleStyle.Error($"{n}: 未找到"); continue; }
        if (!tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 未安装，跳过"); continue; }
        if (tool.ClearProxy()) ConsoleStyle.Success($"{n}: 代理已清除");
        else                   ConsoleStyle.Error($"{n}: 清除失败");
    }
}
static void WriteRegistry()
{
    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
    { ConsoleStyle.Error("--write-registry 仅支持 Windows"); return; }
    var up = EnvVarManager.GetProxyConfig(EnvLevel.User);
    var rp = up.HttpProxy ?? up.HttpsProxy;
    if (string.IsNullOrEmpty(rp)) { ConsoleStyle.Warning("未设置用户级代理"); return; }
    try
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings", writable: true);
        if (key == null) { ConsoleStyle.Error("无法打开注册表键"); return; }
        key.SetValue("ProxyEnable", 1, Microsoft.Win32.RegistryValueKind.DWord);
        key.SetValue("ProxyServer", rp,  Microsoft.Win32.RegistryValueKind.String);
        if (!string.IsNullOrEmpty(up.NoProxy))
            key.SetValue("ProxyOverride", up.NoProxy, Microsoft.Win32.RegistryValueKind.String);
        ConsoleStyle.Success($"Windows 系统代理已写入注册表: {rp}");
    }
    catch (Exception ex) { ConsoleStyle.Error($"写入注册表失败: {ex.Message}"); }
}

// ── gate set ─────────────────────────────────────────────────────────────────
var setCmd    = new Command("set", "设置全局代理，可同时配置工具代理");
var sProxy    = new Argument<string?>("proxy", () => null, "代理地址，如 http://127.0.0.1:7890");
var sTools    = new Argument<string?>("tools", () => null, "工具名称，逗号分隔（可选）");
var sVerify   = new Option<bool>(new[]{"--verify","-v"}, "设置前测试连通性");
var sNoProxy  = new Option<string?>("--no-proxy", "NO_PROXY 排除列表");
var sHistIdx  = new Option<int>("--history-index", () => 0, "从历史记录选择（序号从 1 开始）");
var sLegG     = new Option<string?>(new[]{"-g","--global"}, "[旧]") { IsHidden = true };
var sLegA     = new Option<string?>(new[]{"-a","--app"   }, "[旧]") { IsHidden = true };
var sLegP     = new Option<string?>(new[]{"-p","--proxy" }, "[旧]") { IsHidden = true };
setCmd.AddArgument(sProxy); setCmd.AddArgument(sTools);
setCmd.AddOption(sVerify); setCmd.AddOption(sNoProxy); setCmd.AddOption(sHistIdx);
setCmd.AddOption(sLegG); setCmd.AddOption(sLegA); setCmd.AddOption(sLegP);
setCmd.SetHandler(async (string? proxy, string? tools, bool verify, string? noProxy,
                         int histIdx, string? legG, string? legA, string? legP) =>
{
    var rProxy = proxy ?? legG ?? legP;
    var rTools = tools ?? legA;
    if (histIdx > 0)
    {
        var hist = ProxyHistory.Load();
        if (histIdx > hist.Count) { ConsoleStyle.Error($"编号超出范围（共 {hist.Count} 条）"); return; }
        rProxy = hist[histIdx - 1]; ConsoleStyle.Info($"从历史记录选择: {rProxy}");
    }
    if (string.IsNullOrEmpty(rProxy))
    { ConsoleStyle.Warning("请指定代理地址。用法：gate set <proxy> [tools]"); return; }
    if (verify)
    {
        ConsoleStyle.Info($"正在测试 {rProxy}...");
        var r = await ProxyTester.TestProxyAsync(rProxy);
        if (!r.Success) { ConsoleStyle.Error($"代理测试失败: {r.ErrorMessage}"); return; }
        ConsoleStyle.Success($"代理可用，响应时间: {r.ResponseTimeMs}ms");
    }
    EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig
        { HttpProxy = rProxy, HttpsProxy = rProxy, NoProxy = noProxy });
    ProxyHistory.Push(rProxy);
    ConsoleStyle.Success($"全局代理已设置 -> {rProxy}");
    if (!string.IsNullOrEmpty(noProxy)) ConsoleStyle.Info($"  NO_PROXY: {noProxy}");
    if (!string.IsNullOrEmpty(rTools)) await SetToolProxies(rTools, rProxy);
    ConsoleStyle.Info("提示：运行 `gate install-shell-hook` 可让代理在新终端自动生效。");
}, sProxy, sTools, sVerify, sNoProxy, sHistIdx, sLegG, sLegA, sLegP);
root.AddCommand(setCmd);

// ── gate clear ────────────────────────────────────────────────────────────────
var clearCmd   = new Command("clear", "清除全局代理或工具代理");
var clTools    = new Argument<string?>("tools", () => null);
var clGlobal   = new Option<bool>("--global");
var clAll      = new Option<bool>("--all", "清除全局代理 + 所有工具代理");
clearCmd.AddArgument(clTools); clearCmd.AddOption(clGlobal); clearCmd.AddOption(clAll);
clearCmd.SetHandler((string? tools, bool global, bool all) =>
{
    if (all)
    {
        EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig());
        ConsoleStyle.Success("全局代理已清除。");
        var cnt = 0;
        foreach (var t in ToolRegistry.GetAllTools())
            if (t.IsInstalled() && t.ClearProxy()) cnt++;
        ConsoleStyle.Info($"共清除 {cnt} 个工具的代理配置。"); return;
    }
    if (string.IsNullOrEmpty(tools))
    { EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig()); ConsoleStyle.Success("全局代理已清除。"); }
    else
    {
        ClearToolProxies(tools);
        if (global) { EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig()); ConsoleStyle.Success("全局代理已清除。"); }
    }
}, clTools, clGlobal, clAll);
root.AddCommand(clearCmd);

// ── gate app ──────────────────────────────────────────────────────────────────
var appCmd    = new Command("app", "查看或设置工具代理（支持批量）");
var appName   = new Argument<string?>("name",  () => null);
var appProxy  = new Argument<string?>("proxy", () => null);
var appClear  = new Option<bool>(new[]{"--clear","-c"});
var appAll    = new Option<bool>("--all");
var appExcept = new Option<string?>("--except");
var appLegN   = new Option<string?>("--name",  "[旧]") { IsHidden = true };
var appLegP   = new Option<string?>("--proxy", "[旧]") { IsHidden = true };
appCmd.AddArgument(appName); appCmd.AddArgument(appProxy);
appCmd.AddOption(appClear); appCmd.AddOption(appAll); appCmd.AddOption(appExcept);
appCmd.AddOption(appLegN); appCmd.AddOption(appLegP);
appCmd.SetHandler(async (string? name, string? proxy, bool clear, bool all,
                         string? except, string? legN, string? legP) =>
{
    await Task.CompletedTask;
    var rName = name ?? legN; var rProxy = proxy ?? legP;
    if (all)
    {
        var excl = new System.Collections.Generic.HashSet<string>(
            (except ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        var tgts = ToolRegistry.GetAllTools().Where(t => t.IsInstalled() && !excl.Contains(t.ToolName)).ToList();
        if (clear)  { foreach (var t in tgts) if (t.ClearProxy()) ConsoleStyle.Success($"{t.ToolName}: 代理已清除"); return; }
        if (!string.IsNullOrEmpty(rProxy))
        { foreach (var t in tgts) if (t.SetProxy(rProxy)) ConsoleStyle.Success($"{t.ToolName}: 代理已设置"); return; }
        foreach (var t in tgts) { var c = t.GetCurrentConfig(); if (c != null && !c.IsEmpty) ConsoleStyle.ListItem(t.ToolName.PadRight(22), c.ToString()); }
        return;
    }
    if (string.IsNullOrEmpty(rName)) { ConsoleStyle.Warning("请指定工具名。用法：gate app <name> [proxy]"); return; }
    foreach (var n in rName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var tool = ToolRegistry.GetByName(n);
        if (tool == null) { ConsoleStyle.Error($"{n}: 未找到"); continue; }
        if (clear) { if (tool.IsInstalled() && tool.ClearProxy()) ConsoleStyle.Success($"{n}: 代理已清除"); continue; }
        if (!string.IsNullOrEmpty(rProxy))
        {
            if (!rProxy.Contains("://")) { ConsoleStyle.Error($"{n}: 代理地址无效"); continue; }
            if (!tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 未安装，跳过"); continue; }
            if (tool.SetProxy(rProxy)) ConsoleStyle.Success($"{n}: 代理已设置 -> {rProxy}"); continue;
        }
        var cfg = tool.GetCurrentConfig();
        ConsoleStyle.ListItem(n.PadRight(22) + (tool.IsInstalled() ? "" : " [未安装]"),
            cfg != null && !cfg.IsEmpty ? cfg.ToString() : "(未配置)");
    }
}, appName, appProxy, appClear, appAll, appExcept, appLegN, appLegP);
root.AddCommand(appCmd);
root.AddCommand(new Command("tool", "[旧]") { IsHidden = true });

// ── gate apps ─────────────────────────────────────────────────────────────────
var appsCmd  = new Command("apps", "列出所有支持的工具（含安装和代理状态）");
var appsInst = new Option<bool>(new[]{"--installed","-i"}, "只显示已安装的工具");
appsCmd.AddOption(appsInst);
appsCmd.SetHandler((bool i) => StatusPrinter.PrintToolList(i), appsInst);
root.AddCommand(appsCmd);

// ── gate env ──────────────────────────────────────────────────────────────────
var envCmd  = new Command("env", "查看环境变量代理（Machine / User / Process 三层）");
var eP      = new Option<string?>(new[]{"--proxy","-p"}) { IsHidden = true };
var eH      = new Option<string?>("-H") { IsHidden = true };
var eS      = new Option<string?>("-S") { IsHidden = true };
var eN      = new Option<string?>("--no-proxy") { IsHidden = true };
var eC      = new Option<bool>(new[]{"--clear","-c"}) { IsHidden = true };
var eV      = new Option<bool>(new[]{"--verify","-v"}) { IsHidden = true };
var eWR     = new Option<bool>("--write-registry", "将代理写入 Windows 注册表系统代理");
envCmd.AddOption(eP); envCmd.AddOption(eH); envCmd.AddOption(eS);
envCmd.AddOption(eN); envCmd.AddOption(eC); envCmd.AddOption(eV); envCmd.AddOption(eWR);
envCmd.SetHandler(async (string? p, string? h, string? s, string? n, bool c, bool v, bool wr) =>
{
    if (wr) { WriteRegistry(); return; }
    if (c) { EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig()); ConsoleStyle.Success("全局代理已清除。"); return; }
    var hv = p ?? h;
    if (!string.IsNullOrEmpty(hv))
    {
        if (v) { var r = await ProxyTester.TestProxyAsync(hv); if (!r.Success) { ConsoleStyle.Error($"代理测试失败: {r.ErrorMessage}"); return; } }
        EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig { HttpProxy = hv, HttpsProxy = p ?? s ?? hv, NoProxy = n });
        ConsoleStyle.Success($"全局代理已设置 -> {hv}"); return;
    }
    StatusPrinter.PrintProxyLayers();
}, eP, eH, eS, eN, eC, eV, eWR);
root.AddCommand(envCmd);
root.AddCommand(new Command("global", "[旧]") { IsHidden = true });

// ── gate preset ───────────────────────────────────────────────────────────────
root.AddCommand(PresetCommands.Build());
root.AddCommand(new Command("profile", "[旧]") { IsHidden = true });
root.AddCommand(new Command("apply",   "[旧]") { IsHidden = true });

// ── gate path ─────────────────────────────────────────────────────────────────
var pathCmd    = new Command("path", "查看或设置工具的自定义可执行/配置文件路径");
var pathN      = new Option<string?>(new[]{"-n","--name"}, "工具名称");
var pathExec   = new Option<string?>("--exec",   "工具可执行文件路径");
var pathConf   = new Option<string?>("--config", "工具配置文件路径");
var pathClear  = new Option<bool>("--clear",  "清除自定义路径");
var pathList   = new Option<bool>(new[]{"-l","--list"}, "列出所有自定义路径");
pathCmd.AddOption(pathN); pathCmd.AddOption(pathExec); pathCmd.AddOption(pathConf);
pathCmd.AddOption(pathClear); pathCmd.AddOption(pathList);
pathCmd.SetHandler((string? name, string? exec, string? config, bool clear, bool list) =>
{
    if (list)
    {
        var cp = ToolRegistry.GetCustomPaths();
        if (cp.Count == 0) { ConsoleStyle.Info("暂无自定义路径配置。"); return; }
        foreach (var kv in cp)
            Console.WriteLine($"  {kv.Key,-22} exec={kv.Value.Exec ?? "(auto)"}  config={kv.Value.Config ?? "(auto)"}");
        return;
    }
    if (string.IsNullOrEmpty(name)) { ConsoleStyle.Warning("请指定工具名：gate path -n <tool>"); return; }
    if (clear) { ToolRegistry.ClearCustomPath(name); ConsoleStyle.Success($"{name}: 自定义路径已清除"); return; }
    if (!string.IsNullOrEmpty(exec) || !string.IsNullOrEmpty(config))
    {
        ToolRegistry.SetCustomPath(name, exec, config); ConsoleStyle.Success($"{name}: 路径已更新");
        if (!string.IsNullOrEmpty(exec))   ConsoleStyle.Info($"  exec  : {exec}");
        if (!string.IsNullOrEmpty(config)) ConsoleStyle.Info($"  config: {config}");
        return;
    }
    var info = ToolRegistry.GetCustomPath(name);
    Console.WriteLine($"  {name}\n    exec  : {info?.Exec ?? "(auto)"}\n    config: {info?.Config ?? "(auto)"}");
}, pathN, pathExec, pathConf, pathClear, pathList);
root.AddCommand(pathCmd);

// ── gate test ─────────────────────────────────────────────────────────────────
var testCmd   = new Command("test", "测试代理连通性");
var testProxy = new Argument<string?>("proxy", () => null);
var testUrl   = new Option<string?>("--url", "自定义测试目标 URL");
var testLegP  = new Option<string?>(new[]{"-p","--proxy"}, "[旧]") { IsHidden = true };
var testComp  = new Option<string[]>("--compare", "对比多个代理") { AllowMultipleArgumentsPerToken = true };
testCmd.AddArgument(testProxy); testCmd.AddOption(testUrl);
testCmd.AddOption(testLegP); testCmd.AddOption(testComp);
testCmd.SetHandler(async (string? p, string? url, string? legP, string[] compare) =>
{
    var compareList = (compare ?? Array.Empty<string>())
        .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList();
    if (!string.IsNullOrEmpty(p)) compareList.Insert(0, p);
    if (compareList.Count > 1)
    {
        ConsoleStyle.Title($"代理对比测试（{compareList.Count} 个代理）");
        var results = new System.Collections.Generic.List<(string Proxy, ProxyTestResult Result)>();
        foreach (var px in compareList)
        { ConsoleStyle.Info($"  正在测试 {px}..."); results.Add((px, await ProxyTester.TestProxyAsync(px, url))); }
        Console.WriteLine($"\n  {"代理地址",-42} {"状态",-8} {"延迟(ms)",10}\n  {new string('-', 64)}");
        foreach (var (px, r) in results.OrderBy(x => x.Result.Success ? x.Result.ResponseTimeMs : int.MaxValue))
            Console.WriteLine($"  {px,-42} {(r.Success ? "✓ 可用" : "✗ 不可用"),-8} {(r.Success ? r.ResponseTimeMs.ToString() : "-"),10}");
        var best = results.Where(x => x.Result.Success).OrderBy(x => x.Result.ResponseTimeMs).FirstOrDefault();
        Console.WriteLine();
        if (best.Proxy != null) ConsoleStyle.Success($"最快可用代理: {best.Proxy} ({best.Result.ResponseTimeMs}ms)");
        else ConsoleStyle.Error("所有代理均不可用"); return;
    }
    var proxy = p ?? legP
        ?? EnvVarManager.GetProxyConfig(EnvLevel.Process).HttpProxy
        ?? EnvVarManager.GetProxyConfig(EnvLevel.User).HttpProxy;
    if (string.IsNullOrEmpty(proxy)) { ConsoleStyle.Warning("未配置代理，请指定：gate test <proxy>"); return; }
    ConsoleStyle.Info($"正在测试 {proxy}" + (url != null ? $" -> {url}" : "") + "...");
    var res = await ProxyTester.TestProxyAsync(proxy, url);
    if (res.Success) ConsoleStyle.Success($"代理可用，响应时间: {res.ResponseTimeMs}ms  目标: {res.TestUrl}");
    else             ConsoleStyle.Error($"代理不可用: {res.ErrorMessage}  目标: {res.TestUrl}");
}, testProxy, testUrl, testLegP, testComp);
root.AddCommand(testCmd);
root.AddCommand(new Command("check", "[旧]") { IsHidden = true });

// ── gate list ─────────────────────────────────────────────────────────────────
var listCmd  = new Command("list", "列出工具或预设");
var listRes  = new Argument<string?>("resource", () => null, "apps | presets");
var listInst = new Option<bool>(new[]{"-i","--installed"});
listCmd.AddArgument(listRes); listCmd.AddOption(listInst);
listCmd.SetHandler((string? res, bool inst) =>
{
    switch (res?.ToLowerInvariant())
    {
        case "apps": case "app": case "tools": StatusPrinter.PrintToolList(inst); break;
        case "presets": case "preset":         StatusPrinter.PrintPresetList();   break;
        default: StatusPrinter.PrintToolSummary(); StatusPrinter.PrintPresetList(); break;
    }
}, listRes, listInst);
root.AddCommand(listCmd);

// ── gate history ──────────────────────────────────────────────────────────────
root.AddCommand(HistoryCommands.Build());

// ── gate wizard ───────────────────────────────────────────────────────────────
root.AddCommand(WizardCommand.Build());

// ── gate doctor ───────────────────────────────────────────────────────────────
root.AddCommand(DoctorCommand.Build());

// ── gate info / status / show (aliases) ───────────────────────────────────────
foreach (var alias in new[]{"info", "status", "show"})
{
    var c = new Command(alias, alias == "info" ? "查看当前所有代理配置状态总览" : $"[旧]") { IsHidden = alias != "info" };
    c.SetHandler(StatusPrinter.PrintStatusOverview);
    root.AddCommand(c);
}

// ── gate reset ────────────────────────────────────────────────────────────────
var resetCmd   = new Command("reset", "完全重置：清除所有代理配置、预设和自定义路径");
var resetForce = new Option<bool>(new[]{"--force","-f"}, "跳过确认");
resetCmd.AddOption(resetForce);
resetCmd.SetHandler((bool force) =>
{
    if (!force)
    {
        Console.Write("  警告：此操作将清除所有代理配置、预设和自定义路径，无法撤销。确认? [y/N]: ");
        if ((Console.ReadLine()?.Trim().ToLowerInvariant() ?? "") is not ("y" or "yes")) { ConsoleStyle.Info("已取消。"); return; }
    }
    EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig()); ConsoleStyle.Success("全局代理已清除");
    var tc = 0; foreach (var t in ToolRegistry.GetAllTools()) if (t.IsInstalled() && t.ClearProxy()) tc++;
    ConsoleStyle.Success($"{tc} 个工具代理已清除");
    var ps = ProfileManager.List(); foreach (var p in ps) ProfileManager.Delete(p);
    ConsoleStyle.Success($"{ps.Count} 个预设已删除");
    var ck = ToolRegistry.GetCustomPaths().Keys.ToList(); foreach (var k in ck) ToolRegistry.ClearCustomPath(k);
    ConsoleStyle.Success($"{ck.Count} 个自定义路径配置已清除");
    ConsoleStyle.Info("Gate 已完全重置。运行 `gate wizard` 重新配置。");
}, resetForce);
root.AddCommand(resetCmd);

// ── gate plugin ───────────────────────────────────────────────────────────────
root.AddCommand(PluginCommands.Build());

// ── gate export-all / import-all ─────────────────────────────────────────────
root.AddCommand(MigrationCommands.BuildExportAll());
root.AddCommand(MigrationCommands.BuildImportAll());

// ── gate completion ───────────────────────────────────────────────────────────
root.AddCommand(CompletionCommand.Build());

// ── gate install-shell-hook ───────────────────────────────────────────────────
root.AddCommand(ShellHookCommand.Build());

// ─────────────────────────────────────────────────────────────────────────────
return await root.InvokeAsync(args);
