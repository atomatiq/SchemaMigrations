using System.Reflection;
using System.Text;

namespace SchemaMigrations.Generator.MigrationTool;

internal class MigrationGenerator(Type modelType)
{
    private readonly StringBuilder _guidsBuilder = new();
    private readonly string _projectName = modelType.Namespace!.Remove(modelType.Namespace.LastIndexOf('.'));
    private readonly StringBuilder _upBuilder = new();

    internal bool Finish(string migrationName)
    {
        if (_upBuilder.Length == 0 && _guidsBuilder.Length == 0)
        {
            Console.WriteLine($"No changes found. Migration {migrationName} not added");
            return false;
        }
        var migrationCode = $$"""
                              using SchemaMigrations.Abstractions;
                              using SchemaMigrations.Database.Schemas;
                              using SchemaMigrations.Abstractions.Models;

                              namespace {{_projectName}}.Migrations;
                              public class {{migrationName}}_{{DateTime.Now:yyyyMMdd_hhmm}} : Migration
                              {
                                  public override Dictionary<string, Guid> GuidDictionary { get; } = new Dictionary<string, Guid>()
                                  {
                              {{_guidsBuilder}}    };
                              
                                  public override void Up(MigrationBuilder migrationBuilder)
                                  {
                              {{_upBuilder}}    }    
                              }
                              """;

        return SaveFile(migrationName, migrationCode);
    }

    internal void AddInitialMigration(string schemaName, Type schemaSetType)
    {
        var properties = schemaSetType.GetProperties()
            .ToDictionary(property => property.Name, property => property.PropertyType);

        _guidsBuilder.Append($$"""
                                       { "{{schemaName}}", new Guid("{{Guid.NewGuid()}}") },

                               """);
        _upBuilder.Append($$"""
                                    migrationBuilder.AddSchemaData(new SchemaBuilderData()
                                    {
                                        Guid = GuidDictionary["{{schemaName}}"],
                                        Documentation = "Initial schema for {{schemaSetType.Name}}",
                                        Name = "{{schemaName}}",
                                        VendorId = "Atomatiq"
                                    },
                                    new SchemaDescriptor("{{schemaName}}")
                                    {
                                        Fields = new List<FieldDescriptor>()
                                        {
                                            {{string.Join(",\n                ", properties.Select(p =>
                                            {
                                                var type = p.Value;
                                                if (type.IsGenericType)
                                                {
                                                    var genericTypeName = type.GetGenericTypeDefinition().Name;
                                                    var genericArguments = string.Join(", ", type.GetGenericArguments().Select(t => t.Name));
                                                    var fullTypeName = $"{genericTypeName.Substring(0, genericTypeName.IndexOf('`'))}<{genericArguments}>";
                                                    return $"new FieldDescriptor( \"{p.Key}\", typeof({fullTypeName}) )";
                                                }

                                                return $"new FieldDescriptor( \"{p.Key}\", typeof({type.Name}) )";
                                            }))}}
                                        }
                                    });


                            """);
    }

    internal void AddMigration(string schemaName, List<string> changes)
    {
        _guidsBuilder.Append($$"""
                                       { "{{schemaName}}", new Guid("{{Guid.NewGuid()}}") },

                               """);
        _upBuilder.Append(
            $"""
                     migrationBuilder.UpdateGuid("{schemaName}", GuidDictionary["{schemaName}"]);

             """);
        _upBuilder.Append(
            $"{string.Join("\n", changes.Select(change => $"        migrationBuilder.{change};"))}");
        _upBuilder.AppendLine();
    }

    private bool SaveFile(string migrationName, string migrationCode)
    {
        try
        {
            var projectDirectory = FindProjectDirectory(modelType.Assembly, modelType.Name);
            var migrationsFolderPath = Path.Combine(projectDirectory, "Migrations");

            if (!Directory.Exists(migrationsFolderPath))
            {
                Directory.CreateDirectory(migrationsFolderPath);
            }

            var filePath = Path.Combine(migrationsFolderPath, $"{DateTime.Now:yyyyMMdd_hhmm}_{migrationName}.cs");
            File.WriteAllText(filePath, migrationCode);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occured while saving the migration file. Exception info:");
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            return false;
        }
    }

    private static string FindProjectDirectory(Assembly assembly, string className)
    {
        var currentDirectory =Path.GetDirectoryName(assembly.Location)!;
        
        var directories = Directory.GetDirectories(currentDirectory, "*", SearchOption.AllDirectories);

        foreach (var directory in directories)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length == 0) continue;
            
            var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
            if (files.Any(file => File.ReadAllText(file).Contains($"public class {className}")))
            {
                return directory;
            }
        }

        return string.Empty;
    }
}