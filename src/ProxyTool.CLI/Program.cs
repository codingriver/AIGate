using System.CommandLine;
using System.CommandLine.Invocation;
using ProxyTool;
using ProxyTool.Managers;
using ProxyTool.Models;

var rootCommand = new RootCommand("Proxy Tool - 代理配置管理工具");

// 环境变量命令
var envCommand = new Command("env", "环境变量管理");
var httpProxy = new Option<string>("--http", "HTTP 代理地址");
var httpsProxy = new Option<string>("--https", "HTTPS 代理地址");
var noProxy = new Option<string>("--no-proxy", "排除代理的地址列表");
var clearEnv = new Option<bool>("--clear", "清除代理设置");
var verifyOption = new Option<bool>("--verify", "设置前测试代理连通性");

envCommand.AddOption(httpProxy);
envCommand.AddOption(httpsProxy);
envCommand.AddOption(noProxy);
envCommand.AddOption(clearEnv);
envCommand.AddOption(verifyOption);

envCommand.SetHandler(async (string? http, string? https, string? noProxy, bool clear, bool verify) =>
{
    if (clear)
    {
        var config = new ProxyConfig();
        EnvVarManager.SetProxyForCurrentProcess(config);
        Console.WriteLine("✅ 已清除当前进程代理设置");
        return;
    }

    var config2 = new ProxyConfig
    {
        HttpProxy = http,
        HttpsProxy = https ?? http,
        NoProxy = noProxy
    };
    
    // 验证配置
    var validation = ConfigValidator.ValidateProxyConfig(config2);
    if (!validation.IsValid)
    {
        Console.WriteLine($"❌ 配置验证失败: {validation.ErrorMessage}");
        return;
    }
    
    // 测试代理连通性
    if (verify && !string.IsNullOrEmpty(http))
    {
        Console.WriteLine("正在测试代理连通性...");
        var testResult = await ProxyTester.TestProxyAsync(http);
        if (!testResult.Success)
        {
            Console.WriteLine($"❌ 代理测试失败: {testResult.ErrorMessage}");
            Console.WriteLine("使用 --no-verify 跳过测试");
            return;
        }
        Console.WriteLine($"✅ 代理测试成功，响应时间: {testResult.ResponseTimeMs}ms");
    }
    
    if (!string.IsNullOrEmpty(http) || !string.IsNullOrEmpty(https))
    {
        EnvVarManager.SetProxyForCurrentProcess(config2);
        Console.WriteLine($"✅ 已设置代理: {config2}");
    }
    else
    {
        // 显示当前配置
        var current = EnvVarManager.GetProxyConfig(EnvLevel.User);
        Console.WriteLine("当前环境变量代理设置:");
        Console.WriteLine($"  HTTP_PROXY:  {current.HttpProxy ?? "(未设置)"}");
        Console.WriteLine($"  HTTPS_PROXY: {current.HttpsProxy ?? "(未设置)"}");
        Console.WriteLine($"  NO_PROXY:    {current.NoProxy ?? "(未设置)"}");
    }
}, httpProxy, httpsProxy, noProxy, clearEnv, verifyOption);

rootCommand.AddCommand(envCommand);

// 工具命令
var toolCommand = new Command("tool", "工具代理配置");
var toolName = new Option<string>("--name", "工具名称") { IsRequired = false };
var toolProxy = new Option<string>("--proxy", "代理地址");
var toolClear = new Option<bool>("--clear", "清除代理");
var toolList = new Option<bool>("--list", "列出所有工具");

toolCommand.AddOption(toolName);
toolCommand.AddOption(toolProxy);
toolCommand.AddOption(toolClear);
toolCommand.AddOption(toolList);

toolCommand.SetHandler(async (string? name, string? proxy, bool clear, bool list) =>
{
    if (list)
    {
        Console.WriteLine("支持的工具:");
        foreach (var cat in ToolRegistry.GetCategories())
        {
            Console.WriteLine($"\n[{cat}]");
            foreach (var tool in ToolRegistry.GetByCategory(cat))
            {
                var status = tool.IsInstalled() ? "✅ 已安装" : "❌ 未安装";
                var current = tool.GetCurrentConfig();
                var configStatus = current != null ? "已配置" : "未配置";
                Console.WriteLine($"  {tool.ToolName,-15} {status,-15} [{configStatus}]");
            }
        }
        return;
    }

    if (string.IsNullOrEmpty(name))
    {
        Console.WriteLine("请指定工具名称，或使用 --list 查看所有工具");
        return;
    }

    var targetTool = ToolRegistry.GetByName(name);
    if (targetTool == null)
    {
        Console.WriteLine($"❌ 未找到工具: {name}");
        return;
    }

    if (!targetTool.IsInstalled())
    {
        Console.WriteLine($"❌ 工具 {name} 未安装");
        return;
    }

    if (clear)
    {
        if (targetTool.ClearProxy())
            Console.WriteLine($"✅ 已清除 {name} 的代理配置");
        else
            Console.WriteLine($"❌ 清除 {name} 代理失败");
        return;
    }

    if (!string.IsNullOrEmpty(proxy))
    {
        if (targetTool.SetProxy(proxy))
            Console.WriteLine($"✅ 已设置 {name} 代理: {proxy}");
        else
            Console.WriteLine($"❌ 设置 {name} 代理失败");
        return;
    }

    // 显示当前配置
    var config = targetTool.GetCurrentConfig();
    if (config != null)
        Console.WriteLine($"{name} 当前代理: {config}");
    else
        Console.WriteLine($"{name} 未配置代理");
}, toolName, toolProxy, toolClear, toolList);

rootCommand.AddCommand(toolCommand);

// 配置集命令
var profileCommand = new Command("profile", "配置集管理");
var profileName = new Option<string>("--name", "配置集名称") { IsRequired = false };
var profileSave = new Option<bool>("--save", "保存当前配置");
var profileLoad = new Option<bool>("--load", "加载配置");
var profileDelete = new Option<bool>("--delete", "删除配置");
var profileList = new Option<bool>("--list", "列出配置集");
var profileDefault = new Option<bool>("--set-default", "设为默认");

profileCommand.AddOption(profileName);
profileCommand.AddOption(profileSave);
profileCommand.AddOption(profileLoad);
profileCommand.AddOption(profileDelete);
profileCommand.AddOption(profileList);
profileCommand.AddOption(profileDefault);

profileCommand.SetHandler((string? name, bool save, bool load, bool delete, bool list, bool setDefault) =>
{
    if (list || (!save && !load && !delete && !setDefault))
    {
        var profiles = ProfileManager.List();
        Console.WriteLine("保存的配置集:");
        if (profiles.Count == 0)
            Console.WriteLine("  (无)");
        else
            foreach (var p in profiles)
                Console.WriteLine($"  - {p}");
        
        var defaultProfile = ProfileManager.GetDefaultProfile();
        if (!string.IsNullOrEmpty(defaultProfile))
            Console.WriteLine($"\n默认: {defaultProfile}");
        return;
    }

    if (string.IsNullOrEmpty(name))
    {
        Console.WriteLine("请指定配置集名称 (--name)");
        return;
    }

    if (save)
    {
        var profile = new Profile
        {
            Name = name,
            EnvVars = EnvVarManager.GetProxyConfig(EnvLevel.User)
        };
        
        // 收集所有工具配置
        foreach (var tool in ToolRegistry.GetAllTools())
        {
            var config = tool.GetCurrentConfig();
            if (config != null)
                profile.ToolConfigs[tool.ToolName] = config;
        }
        
        ProfileManager.Save(profile);
        Console.WriteLine($"✅ 已保存配置集: {name}");
        return;
    }

    if (load)
    {
        var profile = ProfileManager.Load(name);
        if (profile == null)
        {
            Console.WriteLine($"❌ 未找到配置集: {name}");
            return;
        }
        
        EnvVarManager.SetProxyForCurrentProcess(profile.EnvVars);
        Console.WriteLine($"✅ 已加载配置集: {name}");
        Console.WriteLine($"  代理: {profile.EnvVars}");
        return;
    }

    if (delete)
    {
        if (ProfileManager.Delete(name))
            Console.WriteLine($"✅ 已删除配置集: {name}");
        else
            Console.WriteLine($"❌ 删除配置集失败: {name}");
        return;
    }

    if (setDefault)
    {
        ProfileManager.SetDefaultProfile(name);
        Console.WriteLine($"✅ 已设默认配置集: {name}");
    }
}, profileName, profileSave, profileLoad, profileDelete, profileList, profileDefault);

rootCommand.AddCommand(profileCommand);

// 测试代理命令
var testCommand = new Command("test", "测试代理连通性");
var testProxy = new Option<string>("--proxy", "代理地址");
var testUrl = new Option<string>("--url", "测试URL");

testCommand.AddOption(testProxy);
testCommand.AddOption(testUrl);

testCommand.SetHandler(async (string? proxy, string? url) =>
{
    if (string.IsNullOrEmpty(proxy))
    {
        var current = EnvVarManager.GetProxyConfig(EnvLevel.User);
        proxy = current.HttpProxy ?? current.HttpsProxy;
        if (string.IsNullOrEmpty(proxy))
        {
            Console.WriteLine("❌ 未配置代理，请使用 --proxy 指定");
            return;
        }
        Console.WriteLine($"测试当前代理: {proxy}");
    }

    Console.Write("测试中...");
    var result = await ProxyTester.TestProxyAsync(proxy, url);
    
    if (result.Success)
        Console.WriteLine($"\n✅ 连接成功! 响应时间: {result.ResponseTimeMs}ms");
    else
        Console.WriteLine($"\n❌ 连接失败: {result.ErrorMessage}");
}, testProxy, testUrl);

rootCommand.AddCommand(testCommand);

// 根级选项：显示帮助
rootCommand.AddOption(new Option<bool>("--help", "显示帮助") { IsHidden = true });

await rootCommand.InvokeAsync(args);