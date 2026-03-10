using ProxyTool.Models;
using Xunit;

namespace ProxyTool.Tests.ParameterCombination;

/// <summary>
/// ProxyConfig 模型参数组合测试（纯内存，无副作用）
/// </summary>
public class ProxyConfigModelTests
{
    [Fact]
    public void IsEmpty_DefaultConfig_ReturnsTrue()
    {
        var config = new ProxyConfig();
        Assert.True(config.IsEmpty);
    }

    [Fact]
    public void IsEmpty_OnlyNoProxy_ReturnsTrue()
    {
        var config = new ProxyConfig { NoProxy = "localhost,127.0.0.1" };
        Assert.True(config.IsEmpty);
    }

    [Theory]
    [InlineData("http://p:8080", null, null, null)]
    [InlineData(null, "https://p:443", null, null)]
    [InlineData(null, null, "http://p:21", null)]
    [InlineData(null, null, null, "socks5://127.0.0.1:1080")]
    public void IsEmpty_AnyProxySet_ReturnsFalse(string? http, string? https, string? ftp, string? socks)
    {
        var config = new ProxyConfig
        {
            HttpProxy = http,
            HttpsProxy = https,
            FtpProxy = ftp,
            SocksProxy = socks
        };
        Assert.False(config.IsEmpty);
    }

    [Fact]
    public void ToString_Empty_ReturnsEmptyString()
    {
        var config = new ProxyConfig();
        var s = config.ToString();
        Assert.Equal("", s);
    }

    [Fact]
    public void ToString_OnlyHttp_ReturnsOnePart()
    {
        var config = new ProxyConfig { HttpProxy = "http://proxy:8080" };
        var s = config.ToString();
        Assert.Contains("HTTP=http://proxy:8080", s);
    }

    [Fact]
    public void ToString_FullConfig_ContainsAllParts()
    {
        var config = new ProxyConfig
        {
            HttpProxy = "http://proxy:8080",
            HttpsProxy = "https://proxy:8443",
            FtpProxy = "http://proxy:21",
            SocksProxy = "socks5://127.0.0.1:1080",
            NoProxy = "localhost"
        };
        var s = config.ToString();
        Assert.Contains("HTTP=", s);
        Assert.Contains("HTTPS=", s);
        Assert.Contains("FTP=", s);
        Assert.Contains("SOCKS=", s);
        Assert.Contains("NO_PROXY=", s);
    }

    [Fact]
    public void ToString_NoProxyOnly_ContainsNoProxy()
    {
        var config = new ProxyConfig { NoProxy = "localhost,.local" };
        var s = config.ToString();
        Assert.Contains("NO_PROXY=localhost,.local", s);
    }
}
