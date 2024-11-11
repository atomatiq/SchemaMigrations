using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using JetBrains.Annotations;
using SchemaMigrations.Abstractions;
using SchemaMigrations.Database.Core;

namespace SchemaMigrations.Database.Schemas;

[UsedImplicitly]
internal class Schema<T> where T : class
{
    internal static Schema Create(Element element)
    {
        var clientAssembly = typeof(T).Assembly;
        
        var schemaName = GetSchemaName(clientAssembly);
        var migrationTypes = GetMigrationTypes(clientAssembly);

        var migrationBuilder = new MigrationBuilder();
        var lastGuidDictionary = new Dictionary<string, Guid>();
        var lastExistedGuidDictionary = new Dictionary<string, Guid>();

        foreach (var migrationType in migrationTypes)
        {
            var migrationInstance = (Migration?)Activator.CreateInstance(migrationType);

            if (migrationInstance is null) continue;

            migrationInstance.Up(migrationBuilder);
            foreach (var pair in migrationInstance.GuidDictionary)
            {
                if (!migrationInstance.GuidDictionary.TryGetValue(pair.Key, out _))
                {
                    lastGuidDictionary.Add(pair.Key, pair.Value);
                }
                else
                {
                    lastGuidDictionary[pair.Key] = pair.Value;
                }

                if (Schema.Lookup(pair.Value) is not null)
                {
                    lastExistedGuidDictionary[pair.Key] = pair.Value;
                }
            }
        }

        var schema = Schema.Lookup(lastGuidDictionary[schemaName]);
        if (schema is not null) return schema;

        if (!lastExistedGuidDictionary.TryGetValue(schemaName, out _))
        {
            return SchemaMigrationUtils.Create(schemaName, migrationBuilder);
        }

        var schemas = SchemaMigrationUtils.MigrateSchemas(lastExistedGuidDictionary, migrationBuilder, element.Document); //it will migrate all the schemas
        return schemas.Find(migratedSchema => migratedSchema.SchemaName == schemaName)!;
    }

    private static string GetSchemaName(Assembly clientAssembly)
    {
        var schemaContextType = clientAssembly.GetTypes().Where(type => type.IsSubclassOf(typeof(SchemaContext)));
        
        var type = schemaContextType
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(SchemaSet<>))
            .First(info => info.PropertyType.GetGenericArguments()[0] == typeof(T));

        var schemaName = type.Name;
        return schemaName;
    }

    private static Type[] GetMigrationTypes(Assembly clientAssembly)
    {
        var migrationTypes = clientAssembly.GetTypes()
            .Where(assemblyType => assemblyType.IsClass && !assemblyType.IsAbstract && assemblyType.IsSubclassOf(typeof(Migration)))
            .OrderBy(migrationType =>
            {
                var className = migrationType.Name;
                return className.Remove(0, className.IndexOf('_'));
            })
            .ToArray();
        return migrationTypes;
    }
}