using ProxyTool.Managers;
using Xunit;

namespace ProxyTool.Tests.ParameterCombination;

/// <summary>
/// 环境变量管理器参数组合测试（仅测试 ParseProxyUrl，不修改环境变量）
/// </summary>
public class EnvVarManagerParameterTests
{
    [Theory]
    [InlineData("http://proxy.example.com:8080", "proxy.example.com", 8080)]
    [InlineData("https://proxy.example.com:443", "proxy.example.com", 443)]
    [InlineData("socks5://127.0.0.1:1080", "127.0.0.1", 1080)]
    [InlineData("socks5://localhost:1080", "localhost", 1080)]
    [InlineData("proxy.example.com:8080", "proxy.example.com", 8080)]
    [InlineData("192.168.1.1:3128", "192.168.1.1", 3128)]
    [InlineData("http://host.with.dots:8080", "host.with.dots", 8080)]
    public void ParseProxyUrl_ValidUrls_ReturnsHostAndPort(string url, string expectedHost, int expectedPort)
    {
        var result = EnvVarManager.ParseProxyUrl(url);
        Assert.NotNull(result);
        Assert.Equal(expectedHost, result.Value.host);
        Assert.Equal(expectedPort, result.Value.port);
    }

    /// <summary>
    /// 无端口时 Uri 会使用协议默认端口：http=80
    /// </summary>
    [Fact]
    public void ParseProxyUrl_NoPort_Http_UsesUriDefaultPort80()
    {
        var result = EnvVarManager.ParseProxyUrl("http://proxy.example.com");
        Assert.NotNull(result);
        Assert.Equal(80, result.Value.port);
    }

    [Fact]
    public void ParseProxyUrl_NoPort_DefaultsTo1080ForSocks()
    {
        var result = EnvVarManager.ParseProxyUrl("socks5://127.0.0.1");
        Assert.NotNull(result);
        Assert.Equal(1080, result.Value.port);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseProxyUrl_NullOrEmpty_ReturnsNull(string? url)
    {
        var result = EnvVarManager.ParseProxyUrl(url);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("no-dot-host:8080")]
    [InlineData("://missing-host:8080")]
    public void ParseProxyUrl_InvalidUrls_ReturnsNull(string url)
    {
        var result = EnvVarManager.ParseProxyUrl(url);
        Assert.Null(result);
    }
}
