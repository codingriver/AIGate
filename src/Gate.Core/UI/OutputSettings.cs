using System;

namespace Gate.UI;

/// <summary>全局输出模式枚举</summary>
public enum OutputMode
{
    Normal, Json, Quiet, NoColor, Plain
}

/// <summary>全局输出设置</summary>
public static class OutputSettings
{
    public static OutputMode Mode { get; set; } = OutputMode.Normal;

    public static bool IsJson  => Mode == OutputMode.Json;
    public static bool IsQuiet => Mode == OutputMode.Quiet;
    public static bool IsPlain => Mode == OutputMode.Plain;
    public static bool NoColor => Mode == OutputMode.NoColor
                               || Mode == OutputMode.Plain
                               || !SupportsAnsi();

    private static bool SupportsAnsi()
    {
        if (Console.IsOutputRedirected) return false;
        if (Environment.GetEnvironmentVariable("NO_COLOR") != null) return false;
        if (Environment.GetEnvironmentVariable("TERM") == "dumb") return false;
        return true;
    }
}
