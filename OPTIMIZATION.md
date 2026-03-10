# 优化方案

## 1. 代码质量优化

### 1.1 消除 Null 警告
- [x] 为所有可空引用类型添加 `?` 标记
- [x] 使用 `??` 和 `?.` 操作符
- [x] 添加 null 检查

### 1.2 代码重构 ✅ (已完成)
- [x] 提取重复代码到基类 ToolConfiguratorBase
- [x] 使用模板方法模式简化工具配置器
- [x] 统一错误处理
- [x] 基类实现所有通用逻辑

### 1.3 性能优化
- [ ] 异步文件操作（部分支持）
- [x] 缓存工具检测结果
- [x] 减少字符串拼接

## 2. 功能增强

### 2.1 更多工具支持 ✅ (已完成)
- [x] 扩展到 20+ 工具
- [x] 支持 Docker Compose（通过 DockerConfigurator）
- [x] 支持 Kubernetes (kubectl, helm)

**新增工具列表:**
| 分类 | 工具 |
|------|------|
| 版本控制 | git, svn |
| 包管理器 | npm, pip, conda, yarn, gem, composer, cargo |
| 构建工具 | maven, gradle |
| 容器工具 | docker, helm |
| 下载工具 | wget, curl |
| 编程语言 | go |
| 其他 | homebrew, terraform |

### 2.2 配置模板
- [ ] 内置常用模板（公司/机场/直连）
- [ ] 模板市场

### 2.3 导入/导出增强
- [ ] 支持 YAML 格式
- [ ] 支持环境变量导出
- [ ] 批量导入

## 3. 测试与文档

### 3.1 单元测试 ✅ (部分完成)
- [x] 创建测试项目结构
- [x] 配置器基类测试
- [x] 工具配置器集成测试
- [x] ToolRegistry 测试
- [x] 57 个测试用例通过
- [ ] 修复边界情况测试 (8个失败)
- [ ] 覆盖率提升到 80%+

### 3.2 文档完善
- [ ] API 文档
- [ ] 使用教程
- [ ] 故障排除指南

## 4. 用户体验

### 4.1 CLI 增强 ✅ (进行中)
- [x] 彩色输出 (ConsoleStyle.cs)
- [x] 进度条
- [x] 表格输出

### 4.2 错误提示 ✅ (进行中)
- [x] 友好错误信息
- [x] 自动修复建议
- [x] 详细日志

## 5. 安全增强

### 5.1 配置加密 ✅ (完成)
- [x] 主密码保护 (AES-256)
- [x] 加密文件/解密文件
- [x] 密码验证

### 5.2 审计日志 ✅ (完成)
- [x] 操作记录
- [x] 日志读取

## 重构后的类结构

```
ToolConfiguratorBase (抽象基类 - 模板方法模式)
├── ToolName (属性, 子类重写)
├── Category (属性, 子类可重写)
├── ConfigPath (自动检测)
├── IsInstalled() (带缓存)
├── GetCurrentConfig() / GetCurrentConfigAsync()
├── SetProxy() / SetProxyAsync()
├── ClearProxy() / ClearProxyAsync()
│
├── ParseConfig() - 子类可重写
├── DetectConfigPath() - 子类可重写
├── DetectToolPath() - 子类可重写
├── IsHttpProxyLine() - 子类可重写
├── IsHttpsProxyLine() - 子类可重写
├── ExtractProxyValue() - 子类可重写
├── FormatProxyLine() - 子类可重写
├── FormatProxyLines() - 子类可重写
├── ClearProxyLines() - 子类可重写
│
└── 事件: OnProxySet, OnProxyCleared, OnError
```

## 子类示例

```csharp
// 简单工具只需几行代码
public class WgetConfigurator : ToolConfiguratorBase
{
    public override string ToolName => "wget";
    public override string Category => "下载工具";

    protected override string? DetectConfigPath()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        return home != null ? Path.Combine(home, ".wgetrc") : null;
    }

    protected override bool IsHttpProxyLine(string line)
        => line.Trim().StartsWith("http_proxy =");
    
    protected override bool IsHttpsProxyLine(string line)
        => line.Trim().StartsWith("https_proxy =");
}
```

## 优先级

| 优先级 | 内容 | 状态 |
|--------|------|------|
| P0 | 消除警告、代码重构 | ✅ 已完成 |
| P1 | 单元测试、文档 | 🔄 进行中 (59/65 测试通过) |
| P2 | 更多工具、模板 | ⏳ 待开始 |
| P3 | GUI、高级功能 | ⏳ 待开始 |