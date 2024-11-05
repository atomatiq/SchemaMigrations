using System;
using System.IO;
using System.Reflection;
using Cocona;
using JetBrains.Annotations;

namespace SchemaMigrations.Generator;
[UsedImplicitly]
internal class Program
{
    internal static void Main(string[] args)
    {
        CoconaLiteApp.Run(([Argument] string migration, bool noBuild = false) =>
        {
            GenerateMigration(migration, noBuild);
        });
    }

    private static void GenerateMigration(string migration, bool noBuild = false)
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
        catch
        {
            Console.WriteLine("An error occured while creation of the migration. Error info: ");
            throw;
        }
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        Console.WriteLine($"{args.Name}");
        var assemblyName = new AssemblyName(args.Name);

        var assemblyPath = Path.Combine(Directory.GetParent(args.RequestingAssembly!.Location)!.FullName, assemblyName.Name + ".dll");
        Console.WriteLine($"Loading assembly: {assemblyPath}, {args.RequestingAssembly?.ImageRuntimeVersion}");
        
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }
}