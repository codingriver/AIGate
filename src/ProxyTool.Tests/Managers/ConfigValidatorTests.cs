using ProxyTool.Managers;
using ProxyTool.Models;
using Xunit;

namespace ProxyTool.Tests.Managers
{
    /// <summary>
    /// 配置验证器测试
    /// </summary>
    public class ConfigValidatorTests
    {
        [Theory]
        [InlineData("http://proxy.example.com:8080", true)]
        [InlineData("https://proxy.example.com:8080", true)]
        [InlineData("socks5://127.0.0.1:1080", true)]
        [InlineData("proxy.example.com:8080", true)]  // 自动添加 http://
        [InlineData("http://192.168.1.1:3128", true)]
        [InlineData("invalid", false)]
        [InlineData("ftp://proxy.example.com:8080", false)]
        [InlineData("http://proxy.example.com:99999", false)]  // 端口超出范围
        public void ValidateProxyUrl_ShouldReturnCorrectResult(string url, bool expectedValid)
        {
            // Act
            var result = ConfigValidator.ValidateProxyUrl(url);
            
            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }
        
        [Theory]
        [InlineData(1, true)]
        [InlineData(8080, true)]
        [InlineData(65535, true)]
        [InlineData(0, false)]
        [InlineData(65536, false)]
        [InlineData(-1, false)]
        public void ValidatePort_ShouldReturnCorrectResult(int port, bool expectedValid)
        {
            // Act
            var result = ConfigValidator.ValidatePort(port);
            
            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }
        
        [Fact]
        public void ValidateProxyConfig_WithValidConfig_ShouldReturnSuccess()
        {
            // Arrange
            var config = new ProxyConfig
            {
                HttpProxy = "http://proxy.example.com:8080",
                HttpsProxy = "http://proxy.example.com:8080"
            };
            
            // Act
            var result = ConfigValidator.ValidateProxyConfig(config);
            
            // Assert
            Assert.True(result.IsValid);
        }
        
        [Fact]
        public void ValidateProxyConfig_WithInvalidConfig_ShouldReturnFailure()
        {
            // Arrange
            var config = new ProxyConfig
            {
                HttpProxy = "invalid-url",
                HttpsProxy = "http://proxy.example.com:8080"
            };
            
            // Act
            var result = ConfigValidator.ValidateProxyConfig(config);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
