using System.IO;
using System.Reflection;
using Cocona;

namespace ConsoleApp2
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CoconaApp.Run(([Argument] string migration, [Argument] string projectPath) =>
            {
                Run(migration, projectPath);
            });
        }

        private static void Run(string migration, string projectPath)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            try
            {
                Console.WriteLine($"Adding migration: {migration}");
                MigrationTool.MigrationTool.AddMigration(migration, projectPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            var assemblyPath = Path.Combine(Directory.GetParent(args.RequestingAssembly.Location)!.FullName, assemblyName.Name + ".dll");
            Console.WriteLine($"Loading assembly: {assemblyPath}");
            if (File.Exists(assemblyPath))
            {
                var ass = Assembly.LoadFrom(assemblyPath);
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
    }
}