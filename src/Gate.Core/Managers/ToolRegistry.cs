using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gate.Configurators;
using Gate.Models;

namespace Gate.Managers;

/// <summary>工具自定义路径条目</summary>
public class CustomPathEntry
{
    public string? Exec   { get; set; }
    public string? Config { get; set; }
    public CustomPathEntry() { }
    public CustomPathEntry(string? exec, string? config) { Exec = exec; Config = config; }
}

/// <summary>
/// 工具注册表。加载顺序（后者覆盖同名前者）：
///   1. 内置 JSON 声明式工具 (EmbeddedResource tools/**/*.json)
///   2. 需要特殊逻辑的内置 C# Configurator
///   3. 用户插件目录 (~/.local/share/gate/plugins/**/tool.json)
/// </summary>
public static class ToolRegistry
{
    private static readonly List<ToolConfiguratorBase> _tools;

    static ToolRegistry()
    {
        GatePaths.EnsureAllDirs();
        _tools = new List<ToolConfiguratorBase>();

        // 1. 内置 JSON 声明式工具
        foreach (var desc in EmbeddedToolDescriptors.LoadAll())
            if (!_tools.Any(t => t.ToolName.Equals(desc.ToolName, StringComparison.OrdinalIgnoreCase)))
                _tools.Add(new DeclarativeToolConfigurator(desc));

        // 2. 特殊 C# Configurator（覆盖同名 JSON 版本）
        RegisterSpecial(new GitConfigurator());
        RegisterSpecial(new DockerConfigurator());
        RegisterSpecial(new DockerComposeConfigurator());
        RegisterSpecial(new DockerBuildxConfigurator());
        RegisterSpecial(new GradleConfigurator());
        RegisterSpecial(new MavenConfigurator());

        // 3. 其余旧版 C# Configurator（JSON 未覆盖的工具）
        RegisterLegacyConfigurators();

        // 4. 用户插件目录
        foreach (var desc in EmbeddedToolDescriptors.LoadFromPluginsDir())
            RegisterSpecial(new DeclarativeToolConfigurator(desc));
    }

    private static void RegisterSpecial(ToolConfiguratorBase cfg)
    {
        var idx = _tools.FindIndex(t =>
            t.ToolName.Equals(cfg.ToolName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _tools[idx] = cfg;
        else          _tools.Add(cfg);
    }

    private static void RegisterLegacyConfigurators()
    {
        var legacy = BuildLegacyList();
        foreach (var cfg in legacy)
            if (!_tools.Any(t => t.ToolName.Equals(cfg.ToolName, StringComparison.OrdinalIgnoreCase)))
                _tools.Add(cfg);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static IReadOnlyList<ToolConfiguratorBase> GetAllTools()       => _tools;
    public static IReadOnlyList<ToolConfiguratorBase> GetInstalledTools() =>
        _tools.Where(t => t.IsInstalled()).ToList();
    public static IReadOnlyList<ToolConfiguratorBase> GetByCategory(string cat) =>
        _tools.Where(t => t.Category == cat).ToList();
    public static ToolConfiguratorBase? GetByName(string name) =>
        _tools.FirstOrDefault(t => t.ToolName.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IReadOnlyList<string> GetCategories() =>
        _tools.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();

    /// <summary>动态注册工具（插件/运行时）</summary>
    public static void Register(ToolConfiguratorBase cfg) => RegisterSpecial(cfg);

    // ── Batch ops ─────────────────────────────────────────────────────────────

    public static Dictionary<string, bool> SetProxyAll(string proxyUrl)
    {
        var r = new Dictionary<string, bool>();
        foreach (var t in _tools.Where(x => x.IsInstalled()))
            r[t.ToolName] = t.SetProxy(proxyUrl);
        return r;
    }

    public static Dictionary<string, bool> ClearProxyAll()
    {
        var r = new Dictionary<string, bool>();
        foreach (var t in _tools.Where(x => x.IsInstalled()))
            r[t.ToolName] = t.ClearProxy();
        return r;
    }

    // ── Legacy C# configurator list ──────────────────────────────────────────
    private static ToolConfiguratorBase[] BuildLegacyList() => new ToolConfiguratorBase[]
    {
        new TortoiseGitConfigurator(),
        new NpmConfigurator(), new PipConfigurator(), new CondaConfigurator(),
        new YarnConfigurator(), new GemConfigurator(), new ComposerConfigurator(),
        new CargoConfigurator(), new HelmConfigurator(),
        new WgetConfigurator(), new CurlConfigurator(),
        new HuggingFaceConfigurator(), new OpenAIConfigurator(), new AnthropicConfigurator(),
        new OllamaConfigurator(), new ClaudeCLIConfigurator(), new AzureAIConfigurator(),
        new GoogleAIConfigurator(), new LMStudioConfigurator(),
        new CursorConfigurator(), new WindsurfConfigurator(), new OpenCodeConfigurator(),
        new VSCodeConfigurator(), new VSCodeInsidersConfigurator(),
        new ClineConfigurator(), new GooseConfigurator(), new BoltNewConfigurator(),
        new GitHubCLIConfigurator(), new GitLabCLIConfigurator(), new BitbucketCLIConfigurator(),
        new KindConfigurator(), new MinikubeConfigurator(), new K3sConfigurator(),
        new HelmfileConfigurator(), new SkaffoldConfigurator(), new TiltConfigurator(),
        new KanikoConfigurator(), new BuildKitConfigurator(),
        new JenkinsConfigurator(), new GitHubActionsConfigurator(), new GitLabCIConfigurator(),
        new ArgoCDConfigurator(), new FluxConfigurator(), new TektonConfigurator(),
        new CircleCIConfigurator(), new TravisCIConfigurator(), new DroneCIConfigurator(),
        new AnsibleConfigurator(), new VaultConfigurator(), new PackerConfigurator(),
        new VagrantConfigurator(), new ConsulConfigurator(), new NomadConfigurator(),
        new IstioConfigurator(), new CrossplaneConfigurator(),
        new TailscaleConfigurator(), new CloudflaredConfigurator(),
        new ContinueConfigurator(), new CodeiumConfigurator(), new TabbyConfigurator(),
        new AiderConfigurator(), new SourcegraphCodyConfigurator(), new AugmentConfigurator(),
        new GPT4AllConfigurator(), new JanConfigurator(), new LlamaCppConfigurator(),
        new VLLMConfigurator(), new TextGenWebUIConfigurator(),
        new MistralAIConfigurator(), new CohereConfigurator(), new PerplexityConfigurator(),
        new AI21Configurator(), new GroqConfigurator(), new ReplicateConfigurator(),
        new FireworksConfigurator(), new AnyscaleConfigurator(), new BeamConfigurator(),
        new PoeConfigurator(), new PoeCliConfigurator(), new OpenRouterConfigurator(),
        new NovitaAIConfigurator(), new TogetherAIConfigurator(), new DeepInfraConfigurator(),
        new HyperbolicConfigurator(), new LeptonConfigurator(), new CerebrasConfigurator(),
        new SambaNovaConfigurator(), new AIStudioConfigurator(), new VertexAIConfigurator(),
        new AWSBedrockConfigurator(), new LangChainConfigurator(), new LlamaIndexConfigurator(),
        new HaystackConfigurator(), new BentoMLConfigurator(), new RayAIConfigurator(),
        new WandBConfigurator(), new MLflowConfigurator(), new KubeflowConfigurator(),
        new TextComConfigurator(), new InflectionConfigurator(), new AnthropicAPIConfigurator(),
        new XAIConfigurator(), new MetaAIConfigurator(),
        new StabilityAIConfigurator(), new MidjourneyConfigurator(), new DALLEConfigurator(),
        new RunwayMLConfigurator(), new ElevenLabsConfigurator(), new MurfAIConfigurator(),
        new WellSaidConfigurator(), new DescriptConfigurator(), new HeyGenConfigurator(),
        new SynthesisAIConfigurator(), new ScaleAIConfigurator(), new LabelboxConfigurator(),
        new ScaleNucleusConfigurator(), new HFInferenceConfigurator(),
        new LightningAIConfigurator(), new PaperspaceConfigurator(), new CoreWeaveConfigurator(),
        new LambdaLabsConfigurator(), new NuGetConfigurator(), new PnpmConfigurator(),
        new PubConfigurator(), new AwsCliConfigurator(), new GcloudConfigurator(),
        new KubectlConfigurator(), new PodmanConfigurator(), new TerraformCliConfigurator(),
        new MercurialConfigurator(), new CocoaPodsConfigurator(), new SwiftPMConfigurator(),
        new RConfigurator(), new JuliaConfigurator(), new PowerShellConfigurator(),
        new HomebrewConfigurator(), new TerraformConfigurator(), new FtpConfigurator(),
    };

    // ── Custom path storage ───────────────────────────────────────────────────

    private static Dictionary<string, CustomPathEntry>? _customPaths;

    private static Dictionary<string, CustomPathEntry> LoadCustomPaths()
    {
        if (_customPaths != null) return _customPaths;
        var f = GatePaths.ToolPathsFile;
        if (!File.Exists(f)) { _customPaths = new(); return _customPaths; }
        try
        {
            var json = File.ReadAllText(f);
            _customPaths = JsonSerializer.Deserialize<Dictionary<string, CustomPathEntry>>(json) ?? new();
        }
        catch { _customPaths = new(); }
        return _customPaths;
    }

    private static void SaveCustomPaths()
    {
        GatePaths.EnsureDir(GatePaths.DataDir);
        File.WriteAllText(GatePaths.ToolPathsFile,
            JsonSerializer.Serialize(_customPaths,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Dictionary<string, CustomPathEntry> GetCustomPaths()    => LoadCustomPaths();
    public static CustomPathEntry? GetCustomPath(string name)
        => LoadCustomPaths().TryGetValue(name, out var e) ? e : null;
    public static void SetCustomPath(string name, string? exec, string? config)
    {
        LoadCustomPaths()[name] = new CustomPathEntry(exec, config);
        SaveCustomPaths();
    }
    public static void ClearCustomPath(string name)
    {
        if (LoadCustomPaths().Remove(name)) SaveCustomPaths();
    }
}
