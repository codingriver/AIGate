using ProxyTool.Managers;
using Xunit;

namespace ProxyTool.Tests.Managers
{
    /// <summary>
    /// 环境变量管理器测试
    /// </summary>
    public class EnvVarManagerTests
    {
        [Theory]
        [InlineData("http://proxy.example.com:8080", "proxy.example.com", 8080)]
        [InlineData("https://proxy.example.com:8080", "proxy.example.com", 8080)]
        [InlineData("socks5://127.0.0.1:1080", "127.0.0.1", 1080)]
        [InlineData("proxy.example.com:8080", "proxy.example.com", 8080)]
        public void ParseProxyUrl_WithValidUrl_ShouldReturnHostAndPort(string url, string expectedHost, int expectedPort)
        {
            // Act
            var result = EnvVarManager.ParseProxyUrl(url);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedHost, result.Value.host);
            Assert.Equal(expectedPort, result.Value.port);
        }
        
        [Fact]
        public void ParseProxyUrl_WithNullUrl_ShouldReturnNull()
        {
            // Act
            var result = EnvVarManager.ParseProxyUrl(null);
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void ParseProxyUrl_WithInvalidUrl_ShouldReturnNull()
        {
            // Act
            var result = EnvVarManager.ParseProxyUrl("not-a-valid-url");
            
            // Assert
            Assert.Null(result);
        }
    }
}
