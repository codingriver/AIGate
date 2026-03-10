using System.Runtime.CompilerServices;
using System.Text;

namespace ProxyTool.Tests;

/// <summary>
/// Sets UTF-8 encoding for console output to prevent garbled Chinese in test output.
/// </summary>
internal static class TestEncoding
{
    [ModuleInitializer]
    internal static void Init()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
    }
}
