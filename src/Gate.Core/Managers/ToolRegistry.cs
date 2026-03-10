using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Configurators;

namespace Gate.Managers;

/// <summary>
/// 工具注册表 - 管理所有工具配置器
/// </summary>
public static class ToolRegistry
{
    private static readonly List<ToolConfiguratorBase> _tools = new()
    {
        // 版本控制
        new GitConfigurator(),
        new SvnConfigurator(),
        
        // 包管理器
        new NpmConfigurator(),
        new PipConfigurator(),
        new CondaConfigurator(),
        new YarnConfigurator(),
        new GemConfigurator(),
        new ComposerConfigurator(),
        new CargoConfigurator(),
        
        // 构建工具
        new MavenConfigurator(),
        new GradleConfigurator(),
        
        // 容器工具
        new DockerConfigurator(),
        new HelmConfigurator(),
        
        // 下载工具
        new WgetConfigurator(),
        new CurlConfigurator(),
        
        // 编程语言
        new GoConfigurator(),
        
        // AI 工具
        new HuggingFaceConfigurator(),
        new OpenAIConfigurator(),
        new AnthropicConfigurator(),
        new OllamaConfigurator(),
        new ClaudeCLIConfigurator(),
        new AzureAIConfigurator(),
        new GoogleAIConfigurator(),
        new LMStudioConfigurator(),
        
        // AI IDE
        new CursorConfigurator(),
        new WindsurfConfigurator(),
        new OpenCodeConfigurator(),
        new VSCodeConfigurator(),
        new VSCodeInsidersConfigurator(),
        new ClineConfigurator(),
        new GooseConfigurator(),
        new BoltNewConfigurator(),
        
        // 版本控制 CLI
        new GitHubCLIConfigurator(),
        new GitLabCLIConfigurator(),
        new BitbucketCLIConfigurator(),
        
        // 容器/镜像工具
        new DockerComposeConfigurator(),
        new DockerBuildxConfigurator(),
        new KindConfigurator(),
        new MinikubeConfigurator(),
        new K3sConfigurator(),
        new HelmfileConfigurator(),
        
        // 开发工具
        new SkaffoldConfigurator(),
        new TiltConfigurator(),
        new KanikoConfigurator(),
        new BuildKitConfigurator(),
        
        // CI/CD
        new JenkinsConfigurator(),
        new GitHubActionsConfigurator(),
        new GitLabCIConfigurator(),
        new ArgoCDConfigurator(),
        new FluxConfigurator(),
        new TektonConfigurator(),
        new CircleCIConfigurator(),
        new TravisCIConfigurator(),
        new DroneCIConfigurator(),
        
        // 基础设施即代码
        new AnsibleConfigurator(),
        new VaultConfigurator(),
        new PackerConfigurator(),
        new VagrantConfigurator(),
        
        // 服务网格/云原生
        new ConsulConfigurator(),
        new NomadConfigurator(),
        new IstioConfigurator(),
        new CrossplaneConfigurator(),
        
        // 网络工具
        new TailscaleConfigurator(),
        new CloudflaredConfigurator(),
        
        // AI 编程助手
        new ContinueConfigurator(),
        new CodeiumConfigurator(),
        new TabbyConfigurator(),
        new AiderConfigurator(),
        new SourcegraphCodyConfigurator(),
        new AugmentConfigurator(),
        
        // 本地 LLM
        new GPT4AllConfigurator(),
        new JanConfigurator(),
        new LlamaCppConfigurator(),
        new VLLMConfigurator(),
        new TextGenWebUIConfigurator(),
        
        // AI API 服务
        new MistralAIConfigurator(),
        new CohereConfigurator(),
        new PerplexityConfigurator(),
        new AI21Configurator(),
        new GroqConfigurator(),
        new ReplicateConfigurator(),
        new FireworksConfigurator(),
        new AnyscaleConfigurator(),
        new BeamConfigurator(),
        
        // AI 聚合平台
        new PoeConfigurator(),
        new PoeCliConfigurator(),
        new OpenRouterConfigurator(),
        
        // 更多 AI API
        new NovitaAIConfigurator(),
        new TogetherAIConfigurator(),
        new DeepInfraConfigurator(),
        new HyperbolicConfigurator(),
        new LeptonConfigurator(),
        new CerebrasConfigurator(),
        new SambaNovaConfigurator(),
        
        // AI 平台
        new AIStudioConfigurator(),
        new VertexAIConfigurator(),
        new AWSBedrockConfigurator(),
        
        // AI 框架
        new LangChainConfigurator(),
        new LlamaIndexConfigurator(),
        new HaystackConfigurator(),
        
        // AI 部署/平台
        new BentoMLConfigurator(),
        new RayAIConfigurator(),
        
        // ML 监控/平台
        new WandBConfigurator(),
        new MLflowConfigurator(),
        new KubeflowConfigurator(),
        
        // 更多 AI API / 平台
        new TextComConfigurator(),
        new InflectionConfigurator(),
        new AnthropicAPIConfigurator(),
        new XAIConfigurator(),
        new MetaAIConfigurator(),
        
        // AI 图像/视频
        new StabilityAIConfigurator(),
        new MidjourneyConfigurator(),
        new DALLEConfigurator(),
        new RunwayMLConfigurator(),
        
        // AI 语音
        new ElevenLabsConfigurator(),
        new MurfAIConfigurator(),
        new WellSaidConfigurator(),
        
        // AI 音视频/数字人
        new DescriptConfigurator(),
        new HeyGenConfigurator(),
        
        // AI 数据/标注
        new SynthesisAIConfigurator(),
        new ScaleAIConfigurator(),
        new LabelboxConfigurator(),
        new ScaleNucleusConfigurator(),
        new HFInferenceConfigurator(),
        
        // AI 训练/云平台
        new LightningAIConfigurator(),
        new PaperspaceConfigurator(),
        new CoreWeaveConfigurator(),
        new LambdaLabsConfigurator(),
        
        // 更多开发工具
        new NuGetConfigurator(),
        new PnpmConfigurator(),
        new PubConfigurator(),
        
        // 云 CLI
        new AwsCliConfigurator(),
        new GcloudConfigurator(),
        
        // 容器/K8s
        new KubectlConfigurator(),
        new PodmanConfigurator(),
        
        // 基础设施
        new TerraformCliConfigurator(),
        
        // 版本控制
        new MercurialConfigurator(),
        
        // 移动开发
        new CocoaPodsConfigurator(),
        new SwiftPMConfigurator(),
        
        // 其他语言
        new RConfigurator(),
        new JuliaConfigurator(),
        
        // Shell
        new PowerShellConfigurator(),
        
        // 其他
        new HomebrewConfigurator(),
        new TerraformConfigurator(),
        new FtpConfigurator(),
    };

    /// <summary>
    /// 获取所有工具
    /// </summary>
    public static IReadOnlyList<ToolConfiguratorBase> GetAllTools() => _tools;

    /// <summary>
    /// 获取已安装的工具
    /// </summary>
    public static IReadOnlyList<ToolConfiguratorBase> GetInstalledTools()
    {
        return _tools.Where(t => t.IsInstalled()).ToList();
    }

    /// <summary>
    /// 按分类获取工具
    /// </summary>
    public static IReadOnlyList<ToolConfiguratorBase> GetByCategory(string category)
    {
        return _tools.Where(t => t.Category == category).ToList();
    }

    /// <summary>
    /// 按名称查找工具
    /// </summary>
    public static ToolConfiguratorBase? GetByName(string name)
    {
        return _tools.FirstOrDefault(t => 
            t.ToolName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有分类
    /// </summary>
    public static IReadOnlyList<string> GetCategories()
    {
        return _tools.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
    }

    /// <summary>
    /// 注册新工具配置器
    /// </summary>
    public static void Register(ToolConfiguratorBase configurator)
    {
        if (!_tools.Any(t => t.ToolName == configurator.ToolName))
        {
            _tools.Add(configurator);
        }
    }

    /// <summary>
    /// 批量设置代理
    /// </summary>
    public static Dictionary<string, bool> SetProxyAll(string proxyUrl)
    {
        var results = new Dictionary<string, bool>();
        foreach (var tool in _tools)
        {
            if (tool.IsInstalled())
            {
                results[tool.ToolName] = tool.SetProxy(proxyUrl);
            }
        }
        return results;
    }

    /// <summary>
    /// 批量清除代理
    /// </summary>
    public static Dictionary<string, bool> ClearProxyAll()
    {
        var results = new Dictionary<string, bool>();
        foreach (var tool in _tools)
        {
            if (tool.IsInstalled())
            {
                results[tool.ToolName] = tool.ClearProxy();
            }
        }
        return results;
    }
}