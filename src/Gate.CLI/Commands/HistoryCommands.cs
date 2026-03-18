using System;
using System.CommandLine;
using System.Linq;
using Gate.CLI.Display;
using Gate.Managers;
using Gate.UI;

namespace Gate.CLI.Commands;

public static class HistoryCommands
{
    public static Command Build()
    {
        var cmd = new Command("history", "查看或清除代理地址历史记录");

        // gate history  (无子命令则列出)
        var clearCmd = new Command("clear", "清除所有历史记录");
        clearCmd.SetHandler(() =>
        {
            ProxyHistory.Clear();
            ConsoleStyle.Success("代理历史记录已清除。");
        });
        cmd.AddCommand(clearCmd);

        cmd.SetHandler(() => StatusPrinter.PrintHistory());
        return cmd;
    }
}
