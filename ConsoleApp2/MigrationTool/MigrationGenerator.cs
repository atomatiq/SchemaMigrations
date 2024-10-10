using System.Text;

namespace ConsoleApp2.MigrationTool;

public class MigrationGenerator(Type modelType)
{
    private readonly StringBuilder _upBuilder = new();
    private readonly StringBuilder _guidsBuilder = new();
    private readonly string _projectName = modelType.Namespace!.Remove(modelType.Namespace.LastIndexOf('.'));

    public bool Finish(string migrationName)
    {
        var migrationCode = $$"""
                              using SchemaMigrations.Abstractions;
                              using SchemaMigrator.Database.Schemas;
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

    public void AddInitialMigration(string schemaName, Type schemaSetType)
    {
        var properties = schemaSetType.GetProperties()
            .ToDictionary(prop => prop.Name, prop => prop.PropertyType);

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

    public void AddMigration(string schemaName, List<string> changes)
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
            var solutionDirectory = PathUtils.GetSolutionDirectory(modelType.Assembly);
            var projectDirectory = FindProjectDirectory(solutionDirectory, modelType.Name);
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

    private static string FindProjectDirectory(string solutionDirectory, string className)
    {
        if (string.IsNullOrEmpty(solutionDirectory)) return string.Empty;

        var directories = Directory.GetDirectories(solutionDirectory, "*", SearchOption.AllDirectories);

        foreach (var directory in directories)
        {
            if (Directory.GetFiles(directory, "*.csproj").Any())
            {
                var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                if (files.Any(file => File.ReadAllText(file).Contains($"public class {className}")))
                {
                    return directory;
                }
            }
        }

        return string.Empty;
    }
}