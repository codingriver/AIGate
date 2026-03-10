using Gate.Managers;
using Gate.Models;
using Xunit;

namespace Gate.Tests.ParameterCombination;

/// <summary>
/// 配置验证器参数组合测试（纯逻辑，不修改环境变量或代理配置）
/// </summary>
public class ConfigValidatorParameterTests
{
    [Theory]
    [InlineData("http://proxy.example.com:8080")]
    [InlineData("https://proxy.example.com:443")]
    [InlineData("https://proxy.example.com:8443")]
    [InlineData("socks5://127.0.0.1:1080")]
    [InlineData("socks4://127.0.0.1:1080")]
    [InlineData("socks://127.0.0.1:1080")]
    [InlineData("http://localhost:3128")]
    [InlineData("http://192.168.1.1:8080")]
    [InlineData("http://10.0.0.1:80")]
    [InlineData("proxy.example.com:8080")] // 无协议时自动加 http://
    public void ValidateProxyUrl_ValidCombinations_ReturnsSuccess(string url)
    {
        var result = ConfigValidator.ValidateProxyUrl(url);
        Assert.True(result.IsValid, $"Expected valid for: {url}, Error: {result.ErrorMessage}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateProxyUrl_NullOrWhitespace_ReturnsSuccess(string? url)
    {
        var result = ConfigValidator.ValidateProxyUrl(url);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("ftp://proxy.example.com:8080")]
    [InlineData("http://proxy.example.com:99999")]
    [InlineData("http://proxy.example.com:0")]
    [InlineData("http://no-dot-host:8080")] // 无点且非 localhost/IP
    [InlineData("http://:8080")] // 无主机
    public void ValidateProxyUrl_InvalidCombinations_ReturnsFailure(string url)
    {
        var result = ConfigValidator.ValidateProxyUrl(url);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(80, true)]
    [InlineData(443, true)]
    [InlineData(8080, true)]
    [InlineData(65535, true)]
    public void ValidatePort_ValidPorts_ReturnsSuccess(int port, bool _)
    {
        var result = ConfigValidator.ValidatePort(port);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(99999)]
    public void ValidatePort_InvalidPorts_ReturnsFailure(int port)
    {
        var result = ConfigValidator.ValidatePort(port);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    public void ValidateIpAddress_ValidOrEmpty_ReturnsSuccess(string? ip)
    {
        var result = ConfigValidator.ValidateIpAddress(ip);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("256.1.1.1")]
    [InlineData("1.2.3.4.5")]
    public void ValidateIpAddress_InvalidIp_ReturnsFailure(string ip)
    {
        var result = ConfigValidator.ValidateIpAddress(ip);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_AllEmpty_ReturnsSuccess()
    {
        var config = new ProxyConfig();
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_OnlyHttp_ReturnsSuccess()
    {
        var config = new ProxyConfig { HttpProxy = "http://proxy.example.com:8080" };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_OnlyHttps_ReturnsSuccess()
    {
        var config = new ProxyConfig { HttpsProxy = "https://proxy.example.com:443" };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_OnlyFtp_ReturnsSuccess()
    {
        var config = new ProxyConfig { FtpProxy = "http://proxy.example.com:2121" };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_OnlySocks_ReturnsSuccess()
    {
        var config = new ProxyConfig { SocksProxy = "socks5://127.0.0.1:1080" };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProxyConfig_FullValid_ReturnsSuccess()
    {
        var config = new ProxyConfig
        {
            HttpProxy = "http://proxy.example.com:8080",
            HttpsProxy = "https://proxy.example.com:8443",
            FtpProxy = "http://proxy.example.com:2121",
            SocksProxy = "socks5://127.0.0.1:1080",
            NoProxy = "localhost,127.0.0.1"
        };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("http://bad:8080", "https://proxy.example.com:443", "ftp", "socks5://127.0.0.1:1080")]
    [InlineData("http://proxy.example.com:8080", "invalid-https", "http://proxy.example.com:21", "socks5://127.0.0.1:1080")]
    [InlineData("http://proxy.example.com:8080", "https://proxy.example.com:443", "http://proxy.example.com:21", "socks5://bad-host:1080")]
    public void ValidateProxyConfig_OneInvalid_ReturnsFailure(string? http, string? https, string? ftp, string? socks)
    {
        var config = new ProxyConfig
        {
            HttpProxy = http,
            HttpsProxy = https,
            FtpProxy = ftp,
            SocksProxy = socks
        };
        var result = ConfigValidator.ValidateProxyConfig(config);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    /// <summary>
    /// 无端口 URL 使用 Uri 默认端口（http=80, https=443）时验证通过
    /// </summary>
    [Theory]
    [InlineData("http://proxy.example.com", true)]   // Uri 默认 80
    [InlineData("https://proxy.example.com", true)]  // Uri 默认 443
    public void ValidateProxyUrl_NoPort_UsesDefaultPort_ReturnsSuccess(string url, bool expectedValid)
    {
        var result = ConfigValidator.ValidateProxyUrl(url);
        Assert.Equal(expectedValid, result.IsValid);
    }
}
