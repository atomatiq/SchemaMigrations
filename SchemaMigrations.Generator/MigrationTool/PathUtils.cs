using System.Reflection;

namespace SchemaMigrations.Generator.MigrationTool;

public static class PathUtils
{
    public static string? GetSolutionDirectory(Assembly assembly)
    {
        var currentDir = new DirectoryInfo(Path.GetDirectoryName(assembly.Location)!);

        while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
        {
            currentDir = currentDir.Parent;
        }

        return currentDir?.FullName;
    }
}