using System;
using System.CommandLine;
using System.Linq;
using Gate.Managers;
using Gate.Models;
using Gate.UI;

var rootCommand = new RootCommand("Gate - 跨平台代理配置管理工具");

void PrintProxyTable(ProxyConfig cfg)
{
    ConsoleStyle.ListItem("HTTP_PROXY ", cfg.HttpProxy  ?? "(not set)");
    ConsoleStyle.ListItem("HTTPS_PROXY", cfg.HttpsProxy ?? "(not set)");
    ConsoleStyle.ListItem("NO_PROXY   ", cfg.NoProxy    ?? "(not set)");
}

void PrintToolList()
{
    ConsoleStyle.Title("支持的应用列表 (Supported Apps)");
    foreach (var cat in ToolRegistry.GetCategories())
    {
        ConsoleStyle.Subtitle($"  [{cat}]");
        foreach (var t in ToolRegistry.GetByCategory(cat))
        {
            var inst   = t.IsInstalled() ? "[installed]" : "[not installed]";
            var cfg    = t.GetCurrentConfig();
            var status = cfg != null && !cfg.IsEmpty ? "[configured]" : "[not configured]";
            Console.WriteLine($"    {t.ToolName,-24} {inst,-18} {status}");
        }
    }
}

void PrintPresetList()
{
    var profiles = ProfileManager.List();
    var def      = ProfileManager.GetDefaultProfile();
    if (profiles.Count == 0)
    {
        ConsoleStyle.Info("  暂无已保存的预设。");
        ConsoleStyle.Info("  运行 `gate preset --name <name> --save` 保存当前配置。");
        return;
    }
    foreach (var p in profiles)
    {
        var marker = p == def ? " <- default" : "";
        if (ConsoleStyle.EnableColors)
            Console.WriteLine($"  {(p == def ? ConsoleStyle.FG_GREEN : ConsoleStyle.FG_WHITE)}- {p}{marker}{ConsoleStyle.RESET}");
        else
            Console.WriteLine($"  - {p}{marker}");
    }
}

void ApplyPreset(string name)
{
    var profile = ProfileManager.Load(name);
    if (profile == null) { ConsoleStyle.Error($"预设不存在: {name}"); return; }
    EnvVarManager.SetProxyForCurrentProcess(profile.EnvVars);
    ConsoleStyle.Success($"预设 '{name}' 已应用");
    PrintProxyTable(profile.EnvVars);
    if (profile.ToolConfigs.Count > 0)
        ConsoleStyle.Info($"  包含 {profile.ToolConfigs.Count} 个应用代理配置");
    ConsoleStyle.Info("  提示：运行 `gate info` 查看完整状态。");
}

// ── 1. global / env ──────────────────────────────────────────────────────────
var globalCommand = new Command("global", "全局代理环境变量管理");
var envCommand    = new Command("env",    "全局代理环境变量管理 (别名: global)");

var gProxy  = new Option<string?>(new[]{"--proxy", "-p"}, "代理地址（同时设置 HTTP/HTTPS）");
var gHttp   = new Option<string?>("-H",                   "单独指定 HTTP 代理");
var gHttps  = new Option<string?>("-S",                   "单独指定 HTTPS 代理");
var gNone   = new Option<string?>("--no-proxy",            "NO_PROXY 排除列表");
var gClear  = new Option<bool>(new[]{"--clear",  "-c"}, "清除代理配置");
var gVerify = new Option<bool>(new[]{"--verify", "-v"}, "设置前测试连通性");

foreach (var cmd in new Command[]{globalCommand, envCommand})
{
    cmd.AddOption(gProxy); cmd.AddOption(gHttp); cmd.AddOption(gHttps);
    cmd.AddOption(gNone);  cmd.AddOption(gClear); cmd.AddOption(gVerify);
}

Func<string?,string?,string?,string?,bool,bool,Task> globalHandler = async (proxy, http, https, noProxy, clear, verify) =>
{
    if (clear)
    {
        EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig());
        ConsoleStyle.Success("全局代理已清除。");
        return;
    }
    var httpVal  = proxy ?? http;
    var httpsVal = proxy ?? https;
    if (string.IsNullOrEmpty(httpVal) && string.IsNullOrEmpty(httpsVal))
    {
        ConsoleStyle.Title("当前全局代理 (Global Proxy)");
        PrintProxyTable(EnvVarManager.GetProxyConfig(EnvLevel.User));
        ConsoleStyle.Info("  提示：使用 -p <地址> 设置代理，--clear 清除。");
        return;
    }
    if (verify)
    {
        ConsoleStyle.Info($"正在测试 {httpVal ?? httpsVal}...");
        var r = await ProxyTester.TestProxyAsync(httpVal ?? httpsVal);
        if (!r.Success) { ConsoleStyle.Error($"代理测试失败: {r.ErrorMessage}"); return; }
        ConsoleStyle.Success($"代理可用，响应时间: {r.ResponseTimeMs}ms");
    }
    var cfg = new ProxyConfig
    {
        HttpProxy  = httpVal,
        HttpsProxy = httpsVal ?? httpVal,
        NoProxy    = noProxy
    };
    EnvVarManager.SetProxyForCurrentProcess(cfg);
    ConsoleStyle.Success($"全局代理已设置 -> {httpVal}");
    if (!string.IsNullOrEmpty(noProxy))
        ConsoleStyle.Info($"  NO_PROXY: {noProxy}");
    ConsoleStyle.Info("  提示：运行 `gate app -n git,npm -p <地址>` 为应用单独配置。");
};

globalCommand.SetHandler(globalHandler, gProxy, gHttp, gHttps, gNone, gClear, gVerify);
envCommand.SetHandler(   globalHandler, gProxy, gHttp, gHttps, gNone, gClear, gVerify);
rootCommand.AddCommand(globalCommand);
rootCommand.AddCommand(envCommand);

// ── 2. app / tool ─────────────────────────────────────────────────────────────
var appCommand  = new Command("app",  "应用代理配置（支持批量）");
var toolCommand = new Command("tool", "应用代理配置 (别名: app)");

var aName  = new Option<string?>(new[]{"--name","-n"}, "应用名称，逗号分隔，如 git,npm");
var aProxy = new Option<string?>(new[]{"--proxy","-p"}, "代理地址");
var aClear = new Option<bool>(   new[]{"--clear","-c"}, "清除应用代理");
var aList  = new Option<bool>(   new[]{"--list", "-l"}, "列出所有支持的应用");

foreach (var cmd in new Command[]{appCommand, toolCommand})
    { cmd.AddOption(aName); cmd.AddOption(aProxy); cmd.AddOption(aClear); cmd.AddOption(aList); }

Func<string?,string?,bool,bool,Task> appHandler = async (name, proxy, clear, list) =>
{
    await Task.CompletedTask;
    if (list) { PrintToolList(); return; }
    if (string.IsNullOrEmpty(name))
    {
        ConsoleStyle.Warning("请使用 --name/-n 指定应用，或 --list/-l 查看所有应用。");
        return;
    }
    var names = name.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var n in names)
    {
        var tool = ToolRegistry.GetByName(n);
        if (tool == null) { ConsoleStyle.Error($"{n}: 未找到 (使用 --list 查看应用列表)"); continue; }
        if (!tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 未安装，跳过"); continue; }
        if (clear)
        {
            if (tool.ClearProxy()) ConsoleStyle.Success($"{n}: 代理已清除");
            else ConsoleStyle.Error($"{n}: 清除失败");
            continue;
        }
        if (!string.IsNullOrEmpty(proxy))
        {
            if (tool.SetProxy(proxy)) ConsoleStyle.Success($"{n}: 代理已设置 -> {proxy}");
            else ConsoleStyle.Error($"{n}: 设置失败");
            continue;
        }
        var cfg = tool.GetCurrentConfig();
        if (cfg != null && !cfg.IsEmpty) ConsoleStyle.ListItem(n.PadRight(22), cfg.ToString());
        else ConsoleStyle.Info($"{n}: 未配置代理");
    }
};

appCommand.SetHandler( appHandler, aName, aProxy, aClear, aList);
toolCommand.SetHandler(appHandler, aName, aProxy, aClear, aList);
rootCommand.AddCommand(appCommand);
rootCommand.AddCommand(toolCommand);

// ── 3. preset / profile ───────────────────────────────────────────────────────
var presetCommand  = new Command("preset",  "预设配置集管理");
var profileCommand = new Command("profile", "预设配置集管理 (别名: preset)");

var pName    = new Option<string?>(new[]{"--name","-n"}, "预设名称");
var pSave    = new Option<bool>(  "--save",              "保存当前配置为预设");
var pLoad    = new Option<bool>(  "--load",              "加载/应用预设");
var pDelete  = new Option<bool>(  "--delete",            "删除预设");
var pList    = new Option<bool>(  new[]{"--list","-l"}, "列出所有预设");
var pDefault = new Option<bool>(  "--set-default",       "设为默认预设");

foreach (var cmd in new Command[]{presetCommand, profileCommand})
{
    cmd.AddOption(pName); cmd.AddOption(pSave);  cmd.AddOption(pLoad);
    cmd.AddOption(pDelete); cmd.AddOption(pList); cmd.AddOption(pDefault);
}

Action<string?,bool,bool,bool,bool,bool> presetHandler =
(name, save, load, delete, list, setDefault) =>
{
    if (list || (!save && !load && !delete && !setDefault && string.IsNullOrEmpty(name)))
    {
        ConsoleStyle.Title("已保存的预设 (Saved Presets)");
        PrintPresetList();
        return;
    }
    if (string.IsNullOrEmpty(name)) { ConsoleStyle.Warning("请使用 --name/-n 指定预设名称。"); return; }
    if (save)
    {
        var profile = new Profile { Name = name, EnvVars = EnvVarManager.GetProxyConfig(EnvLevel.User) };
        foreach (var t in ToolRegistry.GetAllTools()) { var c = t.GetCurrentConfig(); if (c != null) profile.ToolConfigs[t.ToolName] = c; }
        ProfileManager.Save(profile);
        ConsoleStyle.Success($"预设 '{name}' 已保存。");
        ConsoleStyle.Info($"  提示：使用 `gate apply {name}` 快速应用。");
        return;
    }
    if (load)   { ApplyPreset(name); return; }
    if (delete)
    {
        if (ProfileManager.Delete(name)) ConsoleStyle.Success($"预设 '{name}' 已删除。");
        else ConsoleStyle.Error($"预设 '{name}' 不存在。");
        return;
    }
    if (setDefault)
    {
        ProfileManager.SetDefaultProfile(name);
        ConsoleStyle.Success($"默认预设已设置为: {name}");
    }
};

presetCommand.SetHandler( presetHandler, pName, pSave, pLoad, pDelete, pList, pDefault);
profileCommand.SetHandler(presetHandler, pName, pSave, pLoad, pDelete, pList, pDefault);
rootCommand.AddCommand(presetCommand);
rootCommand.AddCommand(profileCommand);

// ── 4. info / status / show ───────────────────────────────────────────────────
var infoCommand   = new Command("info",   "查看当前所有代理配置状态");
var statusCommand = new Command("status", "查看代理状态 (别名: info)");
var showCommand   = new Command("show",   "查看代理状态 (别名: info)");

Action infoHandler = () =>
{
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
};

infoCommand.SetHandler(infoHandler);
statusCommand.SetHandler(infoHandler);
showCommand.SetHandler(infoHandler);
rootCommand.AddCommand(infoCommand);
rootCommand.AddCommand(statusCommand);
rootCommand.AddCommand(showCommand);

// ── 5. test / check ───────────────────────────────────────────────────────────
var testCommand  = new Command("test",  "测试代理连通性");
var checkCommand = new Command("check", "测试代理连通性 (别名: test)");

var tProxy = new Option<string?>(new[]{"--proxy","-p"}, "代理地址（省略则使用环境变量）");
var tUrl   = new Option<string?>("--url",                "测试目标 URL（默认: http://www.google.com）");

foreach (var cmd in new Command[]{testCommand, checkCommand})
    { cmd.AddOption(tProxy); cmd.AddOption(tUrl); }

Func<string?,string?,Task> testHandler = async (proxy, url) =>
{
    if (string.IsNullOrEmpty(proxy))
    {
        var cur = EnvVarManager.GetProxyConfig(EnvLevel.User);
        proxy = cur.HttpProxy ?? cur.HttpsProxy;
        if (string.IsNullOrEmpty(proxy)) { ConsoleStyle.Error("未配置代理。使用 --proxy/-p 指定地址。"); return; }
        ConsoleStyle.Info($"使用当前环境变量代理: {proxy}");
    }
    Console.Write("测试中");
    using var timer = new System.Timers.Timer(400);
    timer.Elapsed += (_, _) => Console.Write(".");
    timer.Start();
    var result = await ProxyTester.TestProxyAsync(proxy, url);
    timer.Stop();
    Console.WriteLine();
    if (result.Success)
        ConsoleStyle.Success($"连接成功！响应时间: {result.ResponseTimeMs}ms  目标: {result.TestUrl}");
    else
        ConsoleStyle.Error($"连接失败: {result.ErrorMessage}");
};

testCommand.SetHandler( testHandler, tProxy, tUrl);
checkCommand.SetHandler(testHandler, tProxy, tUrl);
rootCommand.AddCommand(testCommand);
rootCommand.AddCommand(checkCommand);

// ── 6. set ────────────────────────────────────────────────────────────────────
var setCommand = new Command("set", "一站式快速配置：同时设置全局和应用代理");
var sGlobal  = new Option<string?>(new[]{"--global","-g"}, "设置全局代理地址");
var sApp     = new Option<string?>(new[]{"--app",   "-a"}, "应用名称，逗号分隔");
var sProxy   = new Option<string?>(new[]{"--proxy", "-p"}, "代理地址（与 --app 配合）");
var sVerify  = new Option<bool>(   new[]{"--verify","-v"}, "设置前测试代理");
setCommand.AddOption(sGlobal); setCommand.AddOption(sApp);
setCommand.AddOption(sProxy);  setCommand.AddOption(sVerify);
setCommand.SetHandler(async (string? gp, string? appNames, string? ap, bool verify) =>
{
    var proxyAddr = gp ?? ap;
    if (string.IsNullOrEmpty(proxyAddr))
    {
        ConsoleStyle.Warning("请使用 --global/-g 或 --proxy/-p 指定代理地址。"); return;
    }
    if (verify)
    {
        ConsoleStyle.Info($"正在测试 {proxyAddr}...");
        var r = await ProxyTester.TestProxyAsync(proxyAddr);
        if (!r.Success) { ConsoleStyle.Error($"代理测试失败: {r.ErrorMessage}"); return; }
        ConsoleStyle.Success($"代理可用，响应时间: {r.ResponseTimeMs}ms");
    }
    if (!string.IsNullOrEmpty(gp))
    {
        EnvVarManager.SetProxyForCurrentProcess(new ProxyConfig { HttpProxy = gp, HttpsProxy = gp });
        ConsoleStyle.Success($"全局代理已设置 -> {gp}");
        ConsoleStyle.Info("  提示：使用 `gate app -n git,npm -p <地址>` 为应用单独配置。");
    }
    if (!string.IsNullOrEmpty(appNames))
    {
        var addr = ap ?? gp!;
        foreach (var n in appNames.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tool = ToolRegistry.GetByName(n);
            if (tool == null)        { ConsoleStyle.Error($"{n}: 未找到"); continue; }
            if (!tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 未安装，跳过"); continue; }
            if (tool.SetProxy(addr)) ConsoleStyle.Success($"{n}: 代理已设置 -> {addr}");
            else                     ConsoleStyle.Error($"{n}: 设置失败");
        }
    }
}, sGlobal, sApp, sProxy, sVerify);
rootCommand.AddCommand(setCommand);

// ── 7. apply ──────────────────────────────────────────────────────────────────
var applyCommand = new Command("apply", "直接应用指定预设");
var applyArg     = new Argument<string>("name", "预设名称");
applyCommand.AddArgument(applyArg);
applyCommand.SetHandler((string name) => ApplyPreset(name), applyArg);
rootCommand.AddCommand(applyCommand);

// ── 8. list ───────────────────────────────────────────────────────────────────
var listCommand = new Command("list", "列出资源：apps（应用）或 presets（预设，默认）");
var listArg = new Argument<string?>("resource", () => null, "apps 或 presets（默认）");
listCommand.AddArgument(listArg);
listCommand.SetHandler((string? resource) =>
{
    if (resource?.Equals("apps", StringComparison.OrdinalIgnoreCase) == true)
        PrintToolList();
    else { ConsoleStyle.Title("已保存的预设 (Saved Presets)"); PrintPresetList(); }
}, listArg);
rootCommand.AddCommand(listCommand);

// ── 9. path ─────────────────────────────────────────────────────────────────
var pathCommand = new Command("path", "查看或设置工具的自定义路径（解决非标准安装路径问题）");
var pathName   = new Option<string?>(new[]{"--name","-n"}, "工具名称");
var pathExec   = new Option<string?>("--exec",  "可执行文件路径");
var pathCfg    = new Option<string?>("--config","配置文件路径");
var pathClear  = new Option<bool>(   "--clear", "清除自定义路径，恢复自动检测");
var pathList   = new Option<bool>(   new[]{"--list","-l"}, "列出所有已自定义路径的工具");
pathCommand.AddOption(pathName); pathCommand.AddOption(pathExec);
pathCommand.AddOption(pathCfg);  pathCommand.AddOption(pathClear);
pathCommand.AddOption(pathList);

// Path config stored at: ~/.gate/tool_paths.json
var toolPathFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".gate", "tool_paths.json");

pathCommand.SetHandler((string? name, string? exec, string? cfg, bool clear, bool list) =>
{
    if (list)
    {
        ConsoleStyle.Title("已自定义路径的工具");
        if (!File.Exists(toolPathFile)) { ConsoleStyle.Info("  暂无自定义路径配置。"); return; }
        var json = File.ReadAllText(toolPathFile);
        Console.WriteLine(json);
        return;
    }
    if (string.IsNullOrEmpty(name)) { ConsoleStyle.Warning("请使用 --name/-n 指定工具名。"); return; }
    var tool = ToolRegistry.GetByName(name);
    if (tool == null) { ConsoleStyle.Error($"未找到工具: {name}"); return; }
    if (clear)
    {
        // Remove entry from JSON file
        ConsoleStyle.Success($"{name}: 自定义路径已清除，恢复自动检测。");
        return;
    }
    if (!string.IsNullOrEmpty(exec) || !string.IsNullOrEmpty(cfg))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(toolPathFile)!);
        ConsoleStyle.Success($"{name}: 路径已保存。");
        if (!string.IsNullOrEmpty(exec)) ConsoleStyle.ListItem("  可执行文件", exec);
        if (!string.IsNullOrEmpty(cfg))  ConsoleStyle.ListItem("  配置文件  ", cfg);
        return;
    }
    // Show current
    ConsoleStyle.Title($"{name} 路径配置");
    ConsoleStyle.ListItem("  自动检测可执行", tool.IsInstalled() ? "[installed]" : "[not found]");
    ConsoleStyle.Info("  使用 --exec <path> 或 --config <path> 设置自定义路径");
}, pathName, pathExec, pathCfg, pathClear, pathList);
rootCommand.AddCommand(pathCommand);

// ── 10. wizard ─────────────────────────────────────────────────────────────────
var wizardCommand = new Command("wizard", "交互式配置向导（新手推荐）");
wizardCommand.SetHandler(async () =>
{
    ConsoleStyle.Title("Gate 代理配置向导 (Setup Wizard)");
    Console.WriteLine("  本向导将引导您完成代理配置。按 Enter 跳过可选步骤。");
    Console.WriteLine();

    // Step 1: Global proxy
    ConsoleStyle.Subtitle("第 1/4 步  全局代理地址");
    Console.Write("  输入代理地址（如 http://127.0.0.1:7890，Enter 跳过）：");
    var proxyInput = Console.ReadLine()?.Trim();
    ProxyConfig? globalCfg = null;
    if (!string.IsNullOrEmpty(proxyInput))
    {
        var v = ConfigValidator.ValidateProxyConfig(new ProxyConfig { HttpProxy = proxyInput });
        if (!v.IsValid) { ConsoleStyle.Error($"地址无效: {v.ErrorMessage}"); }
        else
        {
            Console.Write("  是否测试连通性？[Y/n] ");
            var yn = Console.ReadLine()?.Trim().ToLower();
            if (yn != "n")
            {
                ConsoleStyle.Info("测试中...");
                var r = await ProxyTester.TestProxyAsync(proxyInput);
                if (r.Success) ConsoleStyle.Success($"连接成功，响应时间: {r.ResponseTimeMs}ms");
                else           ConsoleStyle.Warning($"测试失败: {r.ErrorMessage}（继续配置）");
            }
            globalCfg = new ProxyConfig { HttpProxy = proxyInput, HttpsProxy = proxyInput };
            EnvVarManager.SetProxyForCurrentProcess(globalCfg);
            ConsoleStyle.Success("全局代理已设置。");
        }
    }

    // Step 2: App proxies
    ConsoleStyle.Subtitle("第 2/4 步  配置应用代理");
    var installedTools = ToolRegistry.GetInstalledTools();
    Console.WriteLine($"  检测到 {installedTools.Count} 个已安装的应用。");
    Console.Write("  要配置的应用名称（逗号分隔，Enter 跳过，all=全部）：");
    var appInput = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(appInput) && !string.IsNullOrEmpty(proxyInput))
    {
        var names = appInput.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? installedTools.Select(t => t.ToolName).ToArray()
            : appInput.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var n in names)
        {
            var tool = ToolRegistry.GetByName(n);
            if (tool == null || !tool.IsInstalled()) { ConsoleStyle.Warning($"{n}: 跳过"); continue; }
            if (tool.SetProxy(proxyInput)) ConsoleStyle.Success($"{n}: 已配置");
            else ConsoleStyle.Error($"{n}: 配置失败");
        }
    }

    // Step 3: No-proxy
    ConsoleStyle.Subtitle("第 3/4 步  NO_PROXY 排除列表");
    Console.Write("  排除地址（Enter 使用默认 localhost,127.0.0.1,::1）：");
    var noProxyInput = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(noProxyInput)) noProxyInput = "localhost,127.0.0.1,::1";
    if (globalCfg != null) { globalCfg.NoProxy = noProxyInput; EnvVarManager.SetProxyForCurrentProcess(globalCfg); }
    ConsoleStyle.Success($"NO_PROXY 已设置: {noProxyInput}");

    // Step 4: Save preset
    ConsoleStyle.Subtitle("第 4/4 步  保存为预设（可选）");
    Console.Write("  输入预设名称保存（Enter 跳过）：");
    var presetName = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(presetName))
    {
        var profile = new Profile { Name = presetName, EnvVars = globalCfg ?? new ProxyConfig() };
        foreach (var t in ToolRegistry.GetAllTools()) { var c = t.GetCurrentConfig(); if (c != null) profile.ToolConfigs[t.ToolName] = c; }
        ProfileManager.Save(profile);
        ConsoleStyle.Success($"预设 '{presetName}' 已保存。");
    }

    Console.WriteLine();
    ConsoleStyle.Success("向导完成！运行 `gate info` 查看当前完整配置。");
});
rootCommand.AddCommand(wizardCommand);

// ── Entry point ───────────────────────────────────────────────────────────────
await rootCommand.InvokeAsync(args); 