using System.Reflection;
using Cocona;

namespace SchemaMigrations.Generator;

public class Program
{
    public static void Main(string[] args)
    {
        CoconaApp.Run(([Argument] string migration) =>
        {
            Run(migration);
        });
    }

    private static void Run(string migration)
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try
        {
            var currentDirectory = Environment.CurrentDirectory;
            MigrationTool.MigrationTool.BuildSolution(currentDirectory);
            MigrationTool.MigrationTool.AddMigration(migration, currentDirectory);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);

        var assemblyPath = Path.Combine(Directory.GetParent(args.RequestingAssembly!.Location)!.FullName, assemblyName.Name + ".dll");
        Console.WriteLine($"Loading assembly: {assemblyPath}");
        if (File.Exists(assemblyPath))
        {
            var ass = Assembly.LoadFrom(assemblyPath);
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }
}