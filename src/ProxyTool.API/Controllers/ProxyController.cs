using Microsoft.AspNetCore.Mvc;
using ProxyTool.Managers;
using ProxyTool.Models;
using ProxyTool.Configurators;

namespace ProxyTool.API.Controllers;

/// <summary>
/// 代理配置 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProxyController : ControllerBase
{
    /// <summary>
    /// 获取当前代理配置
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig([FromQuery] string level = "User")
    {
        var envLevel = level == "System" ? EnvLevel.System : EnvLevel.User;
        var config = EnvVarManager.GetProxyConfig(envLevel);
        return Ok(config);
    }
    
    /// <summary>
    /// 设置代理
    /// </summary>
    [HttpPost("config")]
    public async Task<IActionResult> SetConfig([FromBody] ProxyConfigRequest request)
    {
        var config = new ProxyConfig
        {
            HttpProxy = request.HttpProxy,
            HttpsProxy = request.HttpsProxy ?? request.HttpProxy,
            FtpProxy = request.FtpProxy,
            SocksProxy = request.SocksProxy,
            NoProxy = request.NoProxy
        };
        
        // 验证配置
        var validation = ConfigValidator.ValidateProxyConfig(config);
        if (!validation.IsValid)
        {
            return BadRequest(new { error = validation.ErrorMessage });
        }
        
        // 可选：测试代理
        if (request.Verify && !string.IsNullOrEmpty(request.HttpProxy))
        {
            var testResult = await ProxyTester.TestProxyAsync(request.HttpProxy);
            if (!testResult.Success)
            {
                return BadRequest(new { 
                    error = "代理测试失败", 
                    message = testResult.ErrorMessage 
                });
            }
            return Ok(new { 
                success = true, 
                testResult = testResult 
            });
        }
        
        EnvVarManager.SetProxyForCurrentProcess(config);
        return Ok(new { success = true, config });
    }
    
    /// <summary>
    /// 清除代理
    /// </summary>
    [HttpDelete("config")]
    public IActionResult ClearConfig()
    {
        var config = new ProxyConfig();
        EnvVarManager.SetProxyForCurrentProcess(config);
        return Ok(new { success = true, message = "代理已清除" });
    }
    
    /// <summary>
    /// 测试代理连通性
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestProxy([FromBody] TestProxyRequest request)
    {
        var proxyUrl = request.Proxy ?? 
            EnvVarManager.GetProxyConfig(EnvLevel.User).HttpProxy;
        
        if (string.IsNullOrEmpty(proxyUrl))
        {
            return BadRequest(new { error = "未指定代理地址" });
        }
        
        var result = await ProxyTester.TestProxyAsync(proxyUrl, request.TestUrl, request.Timeout);
        return Ok(result);
    }
}

public class ProxyConfigRequest
{
    public string? HttpProxy { get; set; }
    public string? HttpsProxy { get; set; }
    public string? FtpProxy { get; set; }
    public string? SocksProxy { get; set; }
    public string? NoProxy { get; set; }
    public bool Verify { get; set; }
}

public class TestProxyRequest
{
    public string? Proxy { get; set; }
    public string? TestUrl { get; set; }
    public int Timeout { get; set; } = 10;
}