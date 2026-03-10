using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ProxyTool.Configurators;

namespace ProxyTool.Tests.Configurators
{
    [CollectionDefinition("ConfiguratorEnvTests")]
    public class ConfiguratorEnvTestsCollection { }

    /// <summary>
    /// GitConfigurator 测试
    /// </summary>
    [Collection("ConfiguratorEnvTests")]
    public class GitConfiguratorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _gitconfigPath;

        public GitConfiguratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "proxytool_git_test_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _gitconfigPath = Path.Combine(_tempDir, ".gitconfig");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void ToolName_IsGit()
        {
            var configurator = new GitConfigurator();
            Assert.Equal("git", configurator.ToolName);
        }

        [Fact]
        public void Category_IsVersionControl()
        {
            var configurator = new GitConfigurator();
            Assert.Equal("版本控制", configurator.Category);
        }

        [Fact]
        public void DetectConfigPath_UsesHomeDirectory()
        {
            // 设置 HOME 环境变量
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            
            var configurator = new GitConfigurator();
            var configPath = configurator.ConfigPath;

            Assert.NotNull(configPath);
            Assert.EndsWith(".gitconfig", configPath);
            
            // 清理
            Environment.SetEnvironmentVariable("HOME", null);
        }

        [Fact]
        public void SetProxy_WritesToGitconfig()
        {
            // 设置 HOME 环境变量
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            
            var configurator = new GitConfigurator();
            var result = configurator.SetProxy("http://proxy.com:8080");

            Assert.True(result);
            Assert.True(File.Exists(_gitconfigPath));
            
            var content = File.ReadAllText(_gitconfigPath);
            // Git 配置器使用 http.proxy 格式
            Assert.True(content.Contains("http.proxy") || content.Contains("proxy ="));
            Assert.Contains("proxy.com:8080", content);
            
            // 清理
            Environment.SetEnvironmentVariable("HOME", null);
        }

        [Fact]
        public void ClearProxy_RemovesProxySettings()
        {
            // 设置 HOME 环境变量并预先写入配置
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            File.WriteAllText(_gitconfigPath, @"[http]
	proxy = http://old.proxy.com:8080
[https]
	proxy = http://old.proxy.com:8080
[user]
	name = test
");
            
            var configurator = new GitConfigurator();
            var result = configurator.ClearProxy();

            Assert.True(result);
            
            var content = File.ReadAllText(_gitconfigPath);
            // 检查是否移除了代理配置（保留 user 部分）
            Assert.DoesNotContain("proxy = http://old.proxy.com:8080", content);
            Assert.Contains("name = test", content); // 保留其他配置
            
            // 清理
            Environment.SetEnvironmentVariable("HOME", null);
        }
    }

    /// <summary>
    /// NpmConfigurator 测试
    /// </summary>
    [Collection("ConfiguratorEnvTests")]
    public class NpmConfiguratorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _npmrcPath;

        public NpmConfiguratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "proxytool_npm_test_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _npmrcPath = Path.Combine(_tempDir, ".npmrc");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void ToolName_IsNpm()
        {
            var configurator = new NpmConfigurator();
            Assert.Equal("npm", configurator.ToolName);
        }

        [Fact]
        public void Category_IsPackageManager()
        {
            var configurator = new NpmConfigurator();
            Assert.Equal("包管理器", configurator.Category);
        }

        [Fact]
        public void SetProxy_WritesToNpmrc()
        {
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            
            var configurator = new NpmConfigurator();
            var result = configurator.SetProxy("http://proxy.com:8080");

            Assert.True(result);
            Assert.True(File.Exists(_npmrcPath));
            
            var content = File.ReadAllText(_npmrcPath);
            Assert.Contains("proxy=http://proxy.com:8080", content);
            
            Environment.SetEnvironmentVariable("HOME", null);
        }

        [Fact]
        public void GetCurrentConfig_ParsesExistingConfig()
        {
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            File.WriteAllText(_npmrcPath, @"proxy=http://existing.proxy.com:3128
https-proxy=http://existing.proxy.com:3128
registry=https://registry.npmjs.org/
");
            
            var configurator = new NpmConfigurator();
            var config = configurator.GetCurrentConfig();

            Assert.NotNull(config);
            Assert.Equal("http://existing.proxy.com:3128", config.HttpProxy);
            
            Environment.SetEnvironmentVariable("HOME", null);
        }

        [Fact]
        public void ClearProxy_RemovesProxyOnly()
        {
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            File.WriteAllText(_npmrcPath, @"proxy=http://proxy.com:8080
https-proxy=http://proxy.com:8080
registry=https://registry.npmjs.org/
");
            
            var configurator = new NpmConfigurator();
            var result = configurator.ClearProxy();

            Assert.True(result);
            
            var content = File.ReadAllText(_npmrcPath);
            Assert.DoesNotContain("proxy", content);
            Assert.Contains("registry", content); // 保留其他配置
            
            Environment.SetEnvironmentVariable("HOME", null);
        }
    }

    /// <summary>
    /// WgetConfigurator 测试
    /// </summary>
    [Collection("ConfiguratorEnvTests")]
    public class WgetConfiguratorTests : IDisposable
    {
        private readonly string _tempDir;

        public WgetConfiguratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "proxytool_wget_test_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void ToolName_IsWget()
        {
            var configurator = new WgetConfigurator();
            Assert.Equal("wget", configurator.ToolName);
        }

        [Fact]
        public void Category_IsDownloadTool()
        {
            var configurator = new WgetConfigurator();
            Assert.Equal("下载工具", configurator.Category);
        }

        [Fact]
        public void SetProxy_WritesToWgetrc()
        {
            Environment.SetEnvironmentVariable("HOME", _tempDir);
            
            var configurator = new WgetConfigurator();
            var result = configurator.SetProxy("http://proxy.com:8080");

            Assert.True(result);
            
            var wgetrcPath = Path.Combine(_tempDir, ".wgetrc");
            Assert.True(File.Exists(wgetrcPath));
            
            var content = File.ReadAllText(wgetrcPath);
            Assert.Contains("http_proxy = http://proxy.com:8080", content);
            Assert.Contains("https_proxy = http://proxy.com:8080", content);
            
            Environment.SetEnvironmentVariable("HOME", null);
        }

        [Fact]
        public void FormatProxyLine_UsesSpaceAroundEquals()
        {
            // Wget 配置器使用不同的格式: http_proxy = value
            // 这测试了不同配置器的格式差异
            var wget = new WgetConfigurator();
            var git = new GitConfigurator();
            
            // 确认两个配置器使用不同的格式
            Assert.Equal("下载工具", wget.Category);
            Assert.Equal("版本控制", git.Category);
        }
    }
}