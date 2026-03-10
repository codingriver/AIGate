using System;
using System.Linq;
using Xunit;
using Gate.Configurators;
using Gate.Managers;

namespace Gate.Tests.Managers
{
    /// <summary>
    /// ToolRegistry 测试
    /// </summary>
    public class ToolRegistryTests
    {
        [Fact]
        public void GetAllTools_ReturnsAllTools()
        {
            var tools = ToolRegistry.GetAllTools();
            
            Assert.NotNull(tools);
            Assert.True(tools.Count >= 15); // 至少 15 个工具
        }

        [Fact]
        public void GetByName_FindsTool()
        {
            var git = ToolRegistry.GetByName("git");
            
            Assert.NotNull(git);
            Assert.Equal("git", git.ToolName);
        }

        [Fact]
        public void GetByName_CaseInsensitive()
        {
            var npm = ToolRegistry.GetByName("NPM");
            var docker = ToolRegistry.GetByName("Docker");
            
            Assert.NotNull(npm);
            Assert.NotNull(docker);
        }

        [Fact]
        public void GetByName_ReturnsNullForUnknown()
        {
            var unknown = ToolRegistry.GetByName("nonexistent-tool-xyz");
            
            Assert.Null(unknown);
        }

        [Fact]
        public void GetCategories_ReturnsDistinctCategories()
        {
            var categories = ToolRegistry.GetCategories();
            
            Assert.NotNull(categories);
            Assert.Contains("版本控制", categories);
            Assert.Contains("包管理器", categories);
            Assert.Contains("容器工具", categories);
            Assert.Contains("下载工具", categories);
        }

        [Fact]
        public void GetByCategory_FiltersTools()
        {
            var packageManagers = ToolRegistry.GetByCategory("包管理器");
            
            Assert.NotNull(packageManagers);
            Assert.True(packageManagers.Count >= 5);
            Assert.All(packageManagers, tool => Assert.Equal("包管理器", tool.Category));
        }

        [Fact]
        public void Register_AddsNewTool()
        {
            var initialCount = ToolRegistry.GetAllTools().Count;
            
            // 创建一个唯一的测试工具
            var testTool = new TestConfigurator();
            ToolRegistry.Register(testTool);
            
            var newCount = ToolRegistry.GetAllTools().Count;
            Assert.Equal(initialCount + 1, newCount);
        }

        [Fact]
        public void Register_DoesNotDuplicate()
        {
            var initialCount = ToolRegistry.GetAllTools().Count;
            
            // 尝试注册已存在的工具
            ToolRegistry.Register(new GitConfigurator());
            ToolRegistry.Register(new GitConfigurator());
            
            var newCount = ToolRegistry.GetAllTools().Count;
            Assert.Equal(initialCount, newCount);
        }

        [Fact]
        public void GetInstalledTools_FiltersByInstallation()
        {
            // 这个测试取决于系统上安装的工具
            var installed = ToolRegistry.GetInstalledTools();
            
            Assert.NotNull(installed);
            // 至少应该有一些工具已安装（至少 git 应该在大多数系统上）
        }

        /// <summary>
        /// 默认仅做模拟校验不写入真实配置；设置 PROXYTOOL_RUN_INTEGRATION=1 时执行真实 SetProxyAll。
        /// </summary>
        [Fact]
        public void SetProxyAll_SetsProxyForInstalledTools()
        {
            if (Environment.GetEnvironmentVariable("PROXYTOOL_RUN_INTEGRATION") != "1")
            {
                Assert.NotNull(ToolRegistry.GetAllTools());
                return;
            }
            var proxyUrl = "http://test.proxy.com:8080";
            var results = ToolRegistry.SetProxyAll(proxyUrl);
            Assert.NotNull(results);
        }

        /// <summary>
        /// 默认仅做模拟校验不写入真实配置；设置 PROXYTOOL_RUN_INTEGRATION=1 时执行真实 ClearProxyAll。
        /// </summary>
        [Fact]
        public void ClearProxyAll_ClearsProxyForInstalledTools()
        {
            if (Environment.GetEnvironmentVariable("PROXYTOOL_RUN_INTEGRATION") != "1")
            {
                Assert.NotNull(ToolRegistry.GetAllTools());
                return;
            }
            var results = ToolRegistry.ClearProxyAll();
            Assert.NotNull(results);
        }

        /// <summary>
        /// 测试用配置器
        /// </summary>
        private class TestConfigurator : ToolConfiguratorBase
        {
            public override string ToolName => "test-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            public override string Category => "测试";
        }
    }
}