using Microsoft.AspNetCore.Mvc;
using ProxyTool.Managers;
using ProxyTool.Configurators;

namespace ProxyTool.API.Controllers;

/// <summary>
/// 工具配置 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ToolsController : ControllerBase
{
    /// <summary>
    /// 获取所有工具列表
    /// </summary>
    [HttpGet]
    public IActionResult GetAllTools()
    {
        var tools = ToolRegistry.GetAllTools().Select(t => new
        {
            t.ToolName,
            t.Category,
            IsInstalled = t.IsInstalled(),
            ConfigPath = t.ConfigPath,
            CurrentConfig = t.GetCurrentConfig()
        });
        
        return Ok(new { tools });
    }
    
    /// <summary>
    /// 获取已安装的工具
    /// </summary>
    [HttpGet("installed")]
    public IActionResult GetInstalledTools()
    {
        var tools = ToolRegistry.GetInstalledTools().Select(t => new
        {
            t.ToolName,
            t.Category,
            ConfigPath = t.ConfigPath,
            CurrentConfig = t.GetCurrentConfig()
        });
        
        return Ok(new { tools });
    }
    
    /// <summary>
    /// 获取工具分类
    /// </summary>
    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        return Ok(new { categories = ToolRegistry.GetCategories() });
    }
    
    /// <summary>
    /// 按分类获取工具
    /// </summary>
    [HttpGet("category/{category}")]
    public IActionResult GetByCategory(string category)
    {
        var tools = ToolRegistry.GetByCategory(category).Select(t => new
        {
            t.ToolName,
            t.Category,
            IsInstalled = t.IsInstalled(),
            CurrentConfig = t.GetCurrentConfig()
        });
        
        return Ok(new { tools });
    }
    
    /// <summary>
    /// 获取单个工具信息
    /// </summary>
    [HttpGet("{name}")]
    public IActionResult GetTool(string name)
    {
        var tool = ToolRegistry.GetByName(name);
        if (tool == null)
        {
            return NotFound(new { error = $"未找到工具: {name}" });
        }
        
        return Ok(new
        {
            tool.ToolName,
            tool.Category,
            tool.ConfigPath,
            IsInstalled = tool.IsInstalled(),
            CurrentConfig = tool.GetCurrentConfig()
        });
    }
    
    /// <summary>
    /// 设置工具代理
    /// </summary>
    [HttpPost("{name}/proxy")]
    public IActionResult SetToolProxy(string name, [FromBody] SetToolProxyRequest request)
    {
        var tool = ToolRegistry.GetByName(name);
        if (tool == null)
        {
            return NotFound(new { error = $"未找到工具: {name}" });
        }
        
        if (!tool.IsInstalled())
        {
            return BadRequest(new { error = $"工具 {name} 未安装" });
        }
        
        var success = tool.SetProxy(request.Proxy);
        return Ok(new { success, tool = name });
    }
    
    /// <summary>
    /// 清除工具代理
    /// </summary>
    [HttpDelete("{name}/proxy")]
    public IActionResult ClearToolProxy(string name)
    {
        var tool = ToolRegistry.GetByName(name);
        if (tool == null)
        {
            return NotFound(new { error = $"未找到工具: {name}" });
        }
        
        var success = tool.ClearProxy();
        return Ok(new { success, tool = name });
    }
    
    /// <summary>
    /// 批量设置代理
    /// </summary>
    [HttpPost("batch/proxy")]
    public IActionResult SetBatchProxy([FromBody] SetToolProxyRequest request)
    {
        var results = ToolRegistry.SetProxyAll(request.Proxy);
        return Ok(new { results });
    }
    
    /// <summary>
    /// 批量清除代理
    /// </summary>
    [HttpDelete("batch/proxy")]
    public IActionResult ClearBatchProxy()
    {
        var results = ToolRegistry.ClearProxyAll();
        return Ok(new { results });
    }
}

public class SetToolProxyRequest
{
    public string Proxy { get; set; } = "";
}