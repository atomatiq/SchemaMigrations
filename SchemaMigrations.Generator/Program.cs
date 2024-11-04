using System.Reflection;
using Cocona;
using JetBrains.Annotations;

namespace SchemaMigrations.Generator;

[UsedImplicitly]
public class Program
{
    public static void Main(string[] args)
    {
        CoconaLiteApp.Run(Run);
    }

    private static void Run(string migration, bool noBuild = false)
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try
        {
            var currentDirectory = Environment.CurrentDirectory;
            if (!noBuild)
            {
                MigrationTool.MigrationTool.BuildSolution(currentDirectory);
            }
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
        
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }
}