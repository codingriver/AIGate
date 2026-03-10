using System;
using System.IO;
using Gate.Models;

namespace Gate.Configurators;

/// <summary>
/// 额外的工具配置器 - 包含尚未完全实现的工具配置器占位符
/// </summary>

// CI/CD 配置器
public class JenkinsConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "jenkins";
    public override string Category => "CI/CD";
}

public class GitHubActionsConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "gh";
    public override string Category => "CI/CD";
    
    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        return Path.Combine(home, ".config", "gh", "config.yml");
    }
}

public class GitLabCIConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "gitlab-runner";
    public override string Category => "CI/CD";
}

public class ArgoCDConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "argocd";
    public override string Category => "CI/CD";
}

public class FluxConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "flux";
    public override string Category => "CI/CD";
}

public class TektonConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "tkn";
    public override string Category => "CI/CD";
}

public class CircleCIConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "circleci";
    public override string Category => "CI/CD";
}

public class TravisCIConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "travis";
    public override string Category => "CI/CD";
}

public class DroneCIConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "drone";
    public override string Category => "CI/CD";
}

// 基础设施即代码
public class AnsibleConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "ansible";
    public override string Category => "基础设施即代码";
    
    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        return Path.Combine(home, ".ansible.cfg");
    }
}

public class VaultConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "vault";
    public override string Category => "基础设施即代码";
}

public class PackerConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "packer";
    public override string Category => "基础设施即代码";
}

public class VagrantConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "vagrant";
    public override string Category => "基础设施即代码";
    
    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        return Path.Combine(home, ".vagrant.d", "Vagrantfile");
    }
}

// 服务网格/云原生
public class ConsulConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "consul";
    public override string Category => "服务网格/云原生";
}

public class NomadConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "nomad";
    public override string Category => "服务网格/云原生";
}

public class IstioConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "istioctl";
    public override string Category => "服务网格/云原生";
}

public class CrossplaneConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "crossplane";
    public override string Category => "服务网格/云原生";
}

// 网络工具
public class TailscaleConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "tailscale";
    public override string Category => "网络工具";
    
    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        return Path.Combine(home, ".config", "tailscale", "configs");
    }
}

public class CloudflaredConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "cloudflared";
    public override string Category => "网络工具";
}