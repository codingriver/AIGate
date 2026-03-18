using System;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using Gate.UI;

namespace Gate.CLI.Commands;

/// <summary>
/// gate install-shell-hook — 自动向 shell profile 写入
///   `gate preset load <default>`  或  `eval "$(gate env)"`
/// </summary>
public static class ShellHookCommand
{
    public static Command Build()
    {
        var cmd       = new Command("install-shell-hook",
            "将代理自动加载脚本写入 shell profile（~/.bashrc / ~/.zshrc 等）");
        var presetOpt = new Option<string>(
            new[]{"--preset","-p"},
            () => "default",
            "启动时自动加载的预设名称");
        var shellOpt  = new Option<string?>(
            new[]{"--shell","-s"},
            () => null,
            "指定 shell 类型：bash / zsh / fish / pwsh（自动检测）");
        var dryRunOpt = new Option<bool>(
            "--dry-run", "只打印要写入的内容，不实际修改文件");
        cmd.AddOption(presetOpt);
        cmd.AddOption(shellOpt);
        cmd.AddOption(dryRunOpt);

        cmd.SetHandler((string preset, string? shell, bool dryRun) =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                InstallWindowsHook(preset, dryRun);
                return;
            }
            var shellType = shell ?? DetectShell();
            InstallUnixHook(shellType, preset, dryRun);
        }, presetOpt, shellOpt, dryRunOpt);

        return cmd;
    }

    // ── Windows: PowerShell profile ───────────────────────────────────────────

    private static void InstallWindowsHook(string preset, bool dryRun)
    {
        var hook = $"\n# gate proxy hook\ngate preset load {preset}\n";
        var profileCandidates = new[]
        {
            Environment.GetEnvironmentVariable("PROFILE") ?? "",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PowerShell", "Microsoft.PowerShell_profile.ps1"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1")
        };
        var profileFile = profileCandidates
            .FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p))
            ?? profileCandidates[1];

        if (dryRun)
        {
            Console.WriteLine($"[DRY-RUN] 将向以下文件追加:\n  {profileFile}\n");
            Console.WriteLine(hook);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(profileFile)!);
        if (File.Exists(profileFile) &&
            File.ReadAllText(profileFile).Contains("gate preset load"))
        {
            ConsoleStyle.Warning("PowerShell profile 中已包含 gate hook，跳过。");
            return;
        }
        File.AppendAllText(profileFile, hook);
        ConsoleStyle.Success($"已写入: {profileFile}");
        ConsoleStyle.Info("重新打开终端后生效。");
    }

    // ── Unix: bash / zsh / fish / pwsh ───────────────────────────────────────

    private static void InstallUnixHook(string shellType, string preset, bool dryRun)
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? "~";
        string profileFile;
        string hook;

        switch (shellType.ToLowerInvariant())
        {
            case "zsh":
                profileFile = Path.Combine(home, ".zshrc");
                hook = $"\n# gate proxy hook\ngate preset load {preset}\n";
                break;
            case "fish":
                profileFile = Path.Combine(home, ".config", "fish", "config.fish");
                hook = $"\n# gate proxy hook\ngate preset load {preset}\n";
                break;
            case "pwsh":
                profileFile = Path.Combine(home, ".config", "powershell",
                    "Microsoft.PowerShell_profile.ps1");
                hook = $"\n# gate proxy hook\ngate preset load {preset}\n";
                break;
            default: // bash
                profileFile = Path.Combine(home, ".bashrc");
                hook = $"\n# gate proxy hook\ngate preset load {preset}\n";
                break;
        }

        if (dryRun)
        {
            Console.WriteLine($"[DRY-RUN] 将向以下文件追加:\n  {profileFile}\n");
            Console.WriteLine(hook);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(profileFile) ?? home);
        if (File.Exists(profileFile) &&
            File.ReadAllText(profileFile).Contains("gate preset load"))
        {
            ConsoleStyle.Warning($"{profileFile} 中已包含 gate hook，跳过。");
            return;
        }
        File.AppendAllText(profileFile, hook);
        ConsoleStyle.Success($"已写入: {profileFile}");
        ConsoleStyle.Info($"执行 `source {profileFile}` 或重新打开终端后生效。");
    }

    private static string DetectShell()
    {
        var shell = Environment.GetEnvironmentVariable("SHELL") ?? "";
        if (shell.Contains("zsh"))  return "zsh";
        if (shell.Contains("fish")) return "fish";
        if (shell.Contains("pwsh") || shell.Contains("powershell")) return "pwsh";
        return "bash";
    }

    // helper for dryrun FirstOrDefault
    private static T? FirstOrDefault<T>(
        this T[] arr, Func<T, bool> pred) =>
        Array.Find(arr, new Predicate<T>(pred));
}
