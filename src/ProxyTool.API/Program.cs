var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("===========================================");
Console.WriteLine("  ProxyTool API Server");
Console.WriteLine("  Version: 1.0.0");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET  /api/v1/proxy/config     - 获取代理配置");
Console.WriteLine("  POST /api/v1/proxy/config     - 设置代理配置");
Console.WriteLine("  POST /api/v1/proxy/test       - 测试代理");
Console.WriteLine("  GET  /api/v1/tools            - 获取所有工具");
Console.WriteLine("  GET  /api/v1/profiles         - 获取配置集");
Console.WriteLine();
Console.WriteLine("Starting server on http://localhost:5000");
Console.WriteLine();

app.Run("http://localhost:5000");