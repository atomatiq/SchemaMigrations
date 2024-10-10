using System.Reflection;
using SchemaMigrations.Abstractions;
using SchemaMigrations.Abstractions.Models;

namespace ConsoleApp2.MigrationTool;

public class MigrationTool
{
    public static void AddMigration(string migrationName, string projectPath)
    {
        var projectDir = projectPath.Split('\\').Last();
        Console.WriteLine(projectDir);
        var dllPath = string.Empty;
        foreach (var dir in Directory.GetDirectories(projectPath, $"*", searchOption: SearchOption.AllDirectories))
        {
            var dlls = Directory.GetFiles(dir, $"{projectDir}.dll", SearchOption.AllDirectories);
            if (dlls.Length == 0) continue;
            
            dllPath = dlls.First();
            break;
        }
        //Console.WriteLine(dllPath);
        
        
        
        var assembly = Assembly.LoadFile(dllPath);
        var types = FindModelTypes(projectDir, assembly);
        if (types.Count == 0) return;
        var snapshots = GetLastMigrationsSnapshot(types.Keys.ToArray(), assembly);
        var generator = new MigrationGenerator(types.Values.ElementAt(0));
        for (var i = 0; i < types.Count; i++)
        {
            var pair = types.ElementAt(i);
            var schemaName = pair.Key;
            var type = pair.Value;
            var snapshot = snapshots.FirstOrDefault(snapshot => snapshot.SchemaName == schemaName);
            if (snapshot is null || snapshot.Fields.Count == 0)
            {
                generator.AddInitialMigration(schemaName, type);
            }
            else
            {
                var changes = ChangeDetector.DetectChanges(type, schemaName, snapshot.Fields.ToDictionary(field => field.Name, field => field.Type));
                if (changes.Any())
                {
                    generator.AddMigration(schemaName, changes);
                }
            }
        }

        generator.Finish(migrationName);
    }

    private static List<SchemaDescriptor> GetLastMigrationsSnapshot(string[] schemaNames, Assembly assembly)
    {
        var schemas = new List<SchemaDescriptor>();
        var migrationTypes = GetMigrationTypes(assembly);
        if (migrationTypes.Length == 0) return schemas;
        
        var migrationBuilder = new MigrationBuilder();
        foreach (var migrationType in migrationTypes)
        {
            var migrationInstance = Activator.CreateInstance(migrationType);
            var method = migrationType.GetMethod("Up");
            method!.Invoke(migrationInstance, [migrationBuilder]);
            foreach (var name in schemaNames)
            {
                var schemaDescriptor = new SchemaDescriptor(name)
                {
                    Fields = migrationBuilder.GetColumns(name)
                };
                schemas.Add(schemaDescriptor);
            }
        }

        return schemas;
    }

    private static Type[] GetMigrationTypes(Assembly assembly)
    {
        var migrationTypes = assembly.ExportedTypes
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Migration)))
            .OrderBy(migrationType =>
            {
                var className = migrationType.Name;
                return className.Remove(0, className.IndexOf('_'));
            })
            .ToArray();

        return migrationTypes;
    }

    private static Dictionary<string, Type> FindModelTypes(string projectName, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(SchemaContext))).ToArray();
        if (types.Length != 1)
        {
            throw new ArgumentException("Project must contain exactly one inheritor of SchemaContext.");
        }

        var typesDictionary = new Dictionary<string, Type>();
        var contextType = types[0];
        var propertyInfos = contextType
            .GetProperties()
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(SchemaSet<>));

        foreach (var propertyInfo in propertyInfos)
        {
            typesDictionary.Add(propertyInfo.Name, propertyInfo.PropertyType.GetGenericArguments()[0]);
        }

        return typesDictionary;
    }
}