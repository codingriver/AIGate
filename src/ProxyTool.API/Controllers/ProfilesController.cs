using Microsoft.AspNetCore.Mvc;
using ProxyTool.Managers;
using ProxyTool.Models;

namespace ProxyTool.API.Controllers;

/// <summary>
/// 配置集(Profile) API 控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProfilesController : ControllerBase
{
    /// <summary>
    /// 获取所有配置集
    /// </summary>
    [HttpGet]
    public IActionResult GetProfiles()
    {
        var profiles = ProfileManager.List();
        return Ok(new { profiles });
    }
    
    /// <summary>
    /// 获取配置集详情
    /// </summary>
    [HttpGet("{name}")]
    public IActionResult GetProfile(string name)
    {
        var profile = ProfileManager.Load(name);
        if (profile == null)
        {
            return NotFound(new { error = $"未找到配置集: {name}" });
        }
        
        return Ok(profile);
    }
    
    /// <summary>
    /// 保存配置集
    /// </summary>
    [HttpPost]
    public IActionResult SaveProfile([FromBody] Profile profile)
    {
        if (string.IsNullOrEmpty(profile.Name))
        {
            return BadRequest(new { error = "配置集名称不能为空" });
        }
        
        profile.CreatedAt = DateTime.Now;
        profile.UpdatedAt = DateTime.Now;
        
        ProfileManager.Save(profile);
        return Ok(new { success = true, profile = profile.Name });
    }
    
    /// <summary>
    /// 加载配置集
    /// </summary>
    [HttpPost("{name}/load")]
    public IActionResult LoadProfile(string name)
    {
        var profile = ProfileManager.Load(name);
        if (profile == null)
        {
            return NotFound(new { error = $"未找到配置集: {name}" });
        }
        
        // 应用环境变量
        EnvVarManager.SetProxyForCurrentProcess(profile.EnvVars);
        
        return Ok(new { success = true, profile });
    }
    
    /// <summary>
    /// 删除配置集
    /// </summary>
    [HttpDelete("{name}")]
    public IActionResult DeleteProfile(string name)
    {
        var success = ProfileManager.Delete(name);
        return Ok(new { success });
    }
    
    /// <summary>
    /// 设置默认配置集
    /// </summary>
    [HttpPost("{name}/default")]
    public IActionResult SetDefault(string name)
    {
        ProfileManager.SetDefaultProfile(name);
        return Ok(new { success = true, defaultProfile = name });
    }
    
    /// <summary>
    /// 获取默认配置集
    /// </summary>
    [HttpGet("default")]
    public IActionResult GetDefault()
    {
        var defaultProfile = ProfileManager.GetDefaultProfile();
        return Ok(new { defaultProfile });
    }
    
    /// <summary>
    /// 导出配置集
    /// </summary>
    [HttpGet("{name}/export")]
    public IActionResult ExportProfile(string name, [FromQuery] string format = "json")
    {
        var profile = ProfileManager.Load(name);
        if (profile == null)
        {
            return NotFound(new { error = $"未找到配置集: {name}" });
        }
        
        var extension = format.ToLower() switch
        {
            "yaml" => ".yaml",
            "env" => ".env",
            _ => ".json"
        };
        
        var tempPath = Path.Combine(Path.GetTempPath(), $"{name}{extension}");
        
        var success = format.ToLower() switch
        {
            "yaml" => ConfigExporter.ExportToYaml(profile, tempPath),
            "env" => ConfigExporter.ExportToEnvVars(profile, tempPath),
            _ => ConfigExporter.ExportToJson(profile, tempPath)
        };
        
        if (!success)
        {
            return BadRequest(new { error = "导出失败" });
        }
        
        var content = System.IO.File.ReadAllText(tempPath);
        return Ok(new { path = tempPath, content });
    }
    
    /// <summary>
    /// 导入配置集
    /// </summary>
    [HttpPost("import")]
    public IActionResult ImportProfile([FromBody] ImportProfileRequest request)
    {
        Profile? profile = request.Format?.ToLower() switch
        {
            "yaml" => ConfigImporter.ImportFromYaml(request.FilePath),
            "env" => ConfigImporter.ImportFromEnvVars(),
            _ => ConfigImporter.ImportFromJson(request.FilePath)
        };
        
        if (profile == null)
        {
            return BadRequest(new { error = "导入失败" });
        }
        
        if (!string.IsNullOrEmpty(request.Name))
        {
            profile.Name = request.Name;
        }
        
        ProfileManager.Save(profile);
        return Ok(new { success = true, profile });
    }
}

public class ImportProfileRequest
{
    public string FilePath { get; set; } = "";
    public string? Name { get; set; }
    public string? Format { get; set; }
}