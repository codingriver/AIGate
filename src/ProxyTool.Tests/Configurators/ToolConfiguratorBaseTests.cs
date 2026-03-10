using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using ProxyTool.Configurators;
using ProxyTool.Models;

namespace ProxyTool.Tests.Configurators
{
    /// <summary>
    /// ToolConfiguratorBase 基类测试
    /// </summary>
    public class ToolConfiguratorBaseTests
    {
        /// <summary>
        /// 测试配置器基类的配置解析功能
        /// </summary>
        [Fact]
        public void ParseConfig_StandardFormat_ParsesCorrectly()
        {
            // Arrange
            var configurator = new TestConfigurator();
            var content = @"proxy=""http://proxy.com:8080""
https-proxy=""http://proxy.com:8080""";

            // Act
            var config = configurator.PublicParseConfig(content);

            // Assert
            Assert.Equal("http://proxy.com:8080", config.HttpProxy);
            Assert.Equal("http://proxy.com:8080", config.HttpsProxy);
        }

        /// <summary>
        /// 测试代理行检测 - HTTP
        /// </summary>
        [Theory]
        [InlineData("proxy=http://proxy.com:8080", true)]
        [InlineData("proxy = http://proxy.com:8080", true)]
        [InlineData("http.proxy = http://proxy.com:8080", true)]
        [InlineData("HTTP_PROXY=http://proxy.com:8080", false)] // 大写下划线格式不匹配
        [InlineData("notaproxy=http://proxy.com:8080", false)]
        public void IsHttpProxyLine_DetectsHttpProxy(string line, bool expected)
        {
            // Arrange
            var configurator = new TestConfigurator();

            // Act
            var result = configurator.PublicIsHttpProxyLine(line);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试代理行检测 - HTTPS
        /// </summary>
        [Theory]
        [InlineData("https-proxy=http://proxy.com:8080", true)]
        [InlineData("https-proxy = http://proxy.com:8080", true)]
        [InlineData("https.proxy = http://proxy.com:8080", true)]
        [InlineData("notaproxy=http://proxy.com:8080", false)]
        public void IsHttpsProxyLine_DetectsHttpsProxy(string line, bool expected)
        {
            // Arrange
            var configurator = new TestConfigurator();

            // Act
            var result = configurator.PublicIsHttpsProxyLine(line);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试格式化代理行
        /// </summary>
        [Fact]
        public void FormatProxyLine_FormatsCorrectly()
        {
            // Arrange
            var configurator = new TestConfigurator();

            // Act
            var result = configurator.PublicFormatProxyLine("proxy", "http://proxy.com:8080");

            // Assert
            Assert.Equal("proxy=\"http://proxy.com:8080\"", result);
        }

        /// <summary>
        /// 测试值提取
        /// </summary>
        [Theory]
        [InlineData("proxy=http://host:port", "http://host:port")]
        [InlineData("proxy = http://host:port", "http://host:port")]
        [InlineData("proxy: http://host:port", "http://host:port")]
        [InlineData("proxy=\"http://host:port\"", "http://host:port")]
        public void ExtractProxyValue_ExtractsCorrectly(string line, string expected)
        {
            // Arrange
            var configurator = new TestConfigurator();

            // Act
            var result = configurator.PublicExtractProxyValue(line);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试清除代理行
        /// </summary>
        [Fact]
        public void ClearProxyLines_RemovesProxyLines()
        {
            // Arrange
            var configurator = new TestConfigurator();
            var lines = new List<string>
            {
                "# This is a comment",
                "proxy=http://proxy.com:8080",
                "https-proxy=http://proxy.com:8080",
                "other config",
                ""
            };

            // Act
            var result = configurator.PublicClearProxyLines(lines);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("# This is a comment", result);
            Assert.Contains("other config", result);
            Assert.DoesNotContain(result, l => l.Contains("proxy"));
        }

        /// <summary>
        /// 测试格式化代理行（批量）
        /// </summary>
        [Fact]
        public void FormatProxyLines_FormatsCorrectly()
        {
            // Arrange
            var configurator = new TestConfigurator();
            var proxyUrl = "http://proxy.com:8080";

            // Act
            var result = configurator.PublicFormatProxyLines(proxyUrl);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("proxy", result[0]);
            Assert.Contains("https-proxy", result[1]);
        }

        /// <summary>
        /// 测试目录创建
        /// </summary>
        [Fact]
        public void EnsureDirectoryExists_CreatesDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "proxytool_test_" + Guid.NewGuid());
            var testFile = Path.Combine(tempDir, "subdir", "test.txt");

            try
            {
                // Act
                var configurator = new TestConfigurator();
                configurator.PublicEnsureDirectoryExists(testFile);

                // Assert
                Assert.True(Directory.Exists(Path.Combine(tempDir, "subdir")));
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// 测试用配置器 - 暴露受保护成员
    /// </summary>
    internal class TestConfigurator : ToolConfiguratorBase
    {
        public override string ToolName => "test";
        public override string Category => "测试";

        // 暴露受保护方法用于测试
        public ProxyConfig PublicParseConfig(string content) => ParseConfig(content);
        public bool PublicIsHttpProxyLine(string line) => IsHttpProxyLine(line);
        public bool PublicIsHttpsProxyLine(string line) => IsHttpsProxyLine(line);
        public string PublicFormatProxyLine(string key, string value) => FormatProxyLine(key, value);
        public string PublicExtractProxyValue(string line) => ExtractProxyValue(line);
        public List<string> PublicClearProxyLines(List<string> lines) => ClearProxyLines(lines);
        public List<string> PublicFormatProxyLines(string proxyUrl) => FormatProxyLines(proxyUrl);
        public void PublicEnsureDirectoryExists(string filePath) => EnsureDirectoryExists(filePath);
    }
}