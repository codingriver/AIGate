using System;
using System.CommandLine;
using System.Runtime.InteropServices;
using System.Text;
using Gate.UI;

namespace Gate.CLI.Commands;

/// <summary>gate completion <bash|zsh|fish|pwsh> — 输出 Shell Tab 补全脚本</summary>
public static class CompletionCommand
{
    public static Command Build()
    {
        var cmd      = new Command("completion", "输出 Shell Tab 补全脚本");
        var shellArg = new Argument<string?>("shell", () => null,
            "bash | zsh | fish | pwsh（默认自动检测）");
        cmd.AddArgument(shellArg);
        cmd.SetHandler((string? shell) =>
        {
            var s = (shell ?? DetectShell()).ToLowerInvariant();
            var script = s switch
            {
                "zsh"  => ZshScript(),
                "fish" => FishScript(),
                "pwsh" => PwshScript(),
                _      => BashScript()
            };
            Console.Write(script);
        }, shellArg);
        return cmd;
    }

    private static string DetectShell()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "pwsh";
        var s = Environment.GetEnvironmentVariable("SHELL") ?? "";
        if (s.Contains("zsh"))  return "zsh";
        if (s.Contains("fish")) return "fish";
        return "bash";
    }

    private static string BashScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# gate bash completions");
        sb.AppendLine("_gate_completions() {");
        sb.AppendLine("  local cur prev");
        sb.AppendLine("  cur=\"${COMP_WORDS[COMP_CWORD]}\"");
        sb.AppendLine("  prev=\"${COMP_WORDS[COMP_CWORD-1]}\"");
        sb.AppendLine("");
        sb.AppendLine("  local cmds=\"set clear app apps preset plugin history doctor test wizard export-all import-all completion install-shell-hook path env\"");
        sb.AppendLine("");
        sb.AppendLine("  case \"$prev\" in");
        sb.AppendLine("    preset)");
        sb.AppendLine("      COMPREPLY=( $(compgen -W \"save load del rename export import set-default\" -- \"$cur\") )");
        sb.AppendLine("      return ;;");
        sb.AppendLine("    plugin)");
        sb.AppendLine("      COMPREPLY=( $(compgen -W \"list install remove validate\" -- \"$cur\") )");
        sb.AppendLine("      return ;;");
        sb.AppendLine("    load|del|delete|set-default)");
        sb.AppendLine("      local presets=$(gate preset 2>/dev/null | grep '^ *-' | sed 's/.*- //')");
        sb.AppendLine("      COMPREPLY=( $(compgen -W \"$presets\" -- \"$cur\") )");
        sb.AppendLine("      return ;;");
        sb.AppendLine("  esac");
        sb.AppendLine("");
        sb.AppendLine("  COMPREPLY=( $(compgen -W \"$cmds\" -- \"$cur\") )");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("complete -F _gate_completions gate");
        return sb.ToString();
    }

    private static string ZshScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#compdef gate");
        sb.AppendLine("");
        sb.AppendLine("_gate() {");
        sb.AppendLine("  local -a cmds");
        sb.AppendLine("  cmds=(");
        sb.AppendLine("    'set:设置代理'");
        sb.AppendLine("    'clear:清除代理'");
        sb.AppendLine("    'app:单个应用代理管理'");
        sb.AppendLine("    'apps:列出所有应用'");
        sb.AppendLine("    'preset:管理预设'");
        sb.AppendLine("    'plugin:管理插件'");
        sb.AppendLine("    'history:代理历史记录'");
        sb.AppendLine("    'doctor:诊断配置'");
        sb.AppendLine("    'test:测试代理连通性'");
        sb.AppendLine("    'wizard:配置向导'");
        sb.AppendLine("    'export-all:导出所有配置'");
        sb.AppendLine("    'import-all:导入所有配置'");
        sb.AppendLine("    'completion:Shell Tab 补全'");
        sb.AppendLine("    'install-shell-hook:安装 Shell 启动钩子'");
        sb.AppendLine("    'path:管理工具路径'");
        sb.AppendLine("    'env:查看全局环境变量'");
        sb.AppendLine("  )");
        sb.AppendLine("  _describe 'gate commands' cmds");
        sb.AppendLine("}");
        sb.AppendLine("");
        sb.AppendLine("_gate");
        return sb.ToString();
    }

    private static string FishScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# gate fish completions");
        sb.AppendLine("set -l gate_cmds set clear app apps preset plugin history doctor test wizard export-all import-all completion install-shell-hook path env");
        sb.AppendLine("");
        sb.AppendLine("complete -c gate -f -n '__fish_use_subcommand' -a \"$gate_cmds\"");
        sb.AppendLine("complete -c gate -f -n '__fish_seen_subcommand_from preset' -a 'save load del rename export import set-default'");
        sb.AppendLine("complete -c gate -f -n '__fish_seen_subcommand_from plugin'  -a 'list install remove validate'");
        return sb.ToString();
    }

    private static string PwshScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# gate PowerShell completions");
        sb.AppendLine("Register-ArgumentCompleter -Native -CommandName gate -ScriptBlock {");
        sb.AppendLine("  param($wordToComplete, $commandAst, $cursorPosition)");
        sb.AppendLine("  $cmds = @('set','clear','app','apps','preset','plugin','history','doctor',");
        sb.AppendLine("            'test','wizard','export-all','import-all','completion',");
        sb.AppendLine("            'install-shell-hook','path','env')");
        sb.AppendLine("  $cmds | Where-Object { $_ -like \"$wordToComplete*\" } |");
        sb.AppendLine("    ForEach-Object {");
        sb.AppendLine("      [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
