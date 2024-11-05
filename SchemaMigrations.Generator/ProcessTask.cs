using System.Diagnostics;
using System.Text;

namespace SchemaMigrations.Generator;

internal static class ProcessTasks
{
    public static Process StartProcess(string solutionDir)
    {
        var processInfo = new ProcessStartInfo("dotnet", $"build \"{solutionDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        return Process.Start(processInfo)!;
    }
}