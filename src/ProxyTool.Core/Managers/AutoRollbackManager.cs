using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyTool.Managers
{
    /// <summary>
    /// 自动回滚管理器
    /// </summary>
    public static class AutoRollbackManager
    {
        private const int DefaultTimeoutSeconds = 30;
        
        /// <summary>
        /// 执行带自动回滚的操作
        /// </summary>
        public static async Task<bool> ExecuteWithRollbackAsync(
            Func<Task<bool>> operation,
            Func<Task> rollback,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            // 执行操作
            var success = await operation();
            if (!success)
            {
                return false;
            }
            
            // 等待用户确认
            Console.WriteLine($"\n代理已设置，请验证网络连接...");
            Console.WriteLine($"[{timeoutSeconds}] 秒后自动回滚...");
            Console.WriteLine($"输入 'y' 确认保留，'n' 立即回滚，'q' 退出: ");
            
            var confirmed = await WaitForUserConfirmationAsync(timeoutSeconds);
            
            if (!confirmed)
            {
                Console.WriteLine("\n正在回滚配置...");
                await rollback();
                Console.WriteLine("✅ 已回滚到原始配置");
                return false;
            }
            
            Console.WriteLine("✅ 配置已确认保留");
            return true;
        }
        
        /// <summary>
        /// 等待用户确认
        /// </summary>
        private static async Task<bool> WaitForUserConfirmationAsync(int timeoutSeconds)
        {
            using var cts = new CancellationTokenSource();
            
            // 启动倒计时任务
            var countdownTask = Task.Run(async () =>
            {
                for (int i = timeoutSeconds; i > 0; i--)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                    
                    Console.Write($"\r[{i,2}] 秒后自动回滚... ");
                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, cts.Token);
            
            // 等待用户输入
            var inputTask = Task.Run(() =>
            {
                try
                {
                    var key = Console.ReadKey(intercept: true);
                    return key.KeyChar.ToString().ToLower();
                }
                catch
                {
                    return string.Empty;
                }
            });
            
            // 等待任一任务完成
            var completedTask = await Task.WhenAny(inputTask, countdownTask);
            
            if (completedTask == inputTask)
            {
                cts.Cancel();
                var input = await inputTask;
                
                return input switch
                {
                    "y" => true,
                    "n" => false,
                    "q" => false,
                    _ => false
                };
            }
            
            // 超时
            Console.WriteLine("\n超时，自动回滚...");
            return false;
        }
    }
}
