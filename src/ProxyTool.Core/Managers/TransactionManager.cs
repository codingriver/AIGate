using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProxyTool.Managers
{
    /// <summary>
    /// 代理配置事务
    /// </summary>
    public class ProxyTransaction
    {
        private readonly List<TransactionItem> _items = new();
        private readonly List<TransactionItem> _executedItems = new();
        
        /// <summary>
        /// 添加操作
        /// </summary>
        public void Add(string toolName, Func<Task<bool>> operation, Func<Task> rollback)
        {
            _items.Add(new TransactionItem
            {
                ToolName = toolName,
                Operation = operation,
                Rollback = rollback
            });
        }
        
        /// <summary>
        /// 提交事务
        /// </summary>
        public async Task<TransactionResult> CommitAsync()
        {
            _executedItems.Clear();
            
            foreach (var item in _items)
            {
                try
                {
                    var success = await item.Operation();
                    
                    if (success)
                    {
                        item.Executed = true;
                        item.Success = true;
                        _executedItems.Add(item);
                    }
                    else
                    {
                        item.Success = false;
                        return new TransactionResult
                        {
                            Success = false,
                            FailedTool = item.ToolName,
                            ErrorMessage = $"工具 {item.ToolName} 配置失败",
                            Results = _items.Select(i => new ToolResult
                            {
                                ToolName = i.ToolName,
                                Success = i.Success,
                                Executed = i.Executed
                            }).ToList()
                        };
                    }
                }
                catch (Exception ex)
                {
                    item.Success = false;
                    return new TransactionResult
                    {
                        Success = false,
                        FailedTool = item.ToolName,
                        ErrorMessage = ex.Message,
                        Results = _items.Select(i => new ToolResult
                        {
                            ToolName = i.ToolName,
                            Success = i.Success,
                            Executed = i.Executed
                        }).ToList()
                    };
                }
            }
            
            return new TransactionResult
            {
                Success = true,
                Results = _items.Select(i => new ToolResult
                {
                    ToolName = i.ToolName,
                    Success = i.Success,
                    Executed = i.Executed
                }).ToList()
            };
        }
        
        /// <summary>
        /// 回滚事务
        /// </summary>
        public async Task RollbackAsync()
        {
            // 按相反顺序回滚
            foreach (var item in _executedItems.AsEnumerable().Reverse())
            {
                try
                {
                    await item.Rollback();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: 回滚 {item.ToolName} 失败: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// 事务项
    /// </summary>
    public class TransactionItem
    {
        public string ToolName { get; set; } = "";
        public Func<Task<bool>> Operation { get; set; } = () => Task.FromResult(true);
        public Func<Task> Rollback { get; set; } = () => Task.CompletedTask;
        public bool Executed { get; set; }
        public bool Success { get; set; }
    }
    
    /// <summary>
    /// 事务结果
    /// </summary>
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string? FailedTool { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ToolResult> Results { get; set; } = new();
    }
    
    /// <summary>
    /// 工具结果
    /// </summary>
    public class ToolResult
    {
        public string ToolName { get; set; } = "";
        public bool Success { get; set; }
        public bool Executed { get; set; }
    }
}
