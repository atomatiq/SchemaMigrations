using System.Reflection;
using Autodesk.Revit.DB.ExtensibleStorage;
using SchemaMigrations.Abstractions;
using SchemaMigrator.Database.Core;

namespace SchemaMigrator.Database.Schemas;

public class Schema<T> where T : class
{
    public Schema Create()
    {
        var contextType = typeof(SchemaContext);
        var propertyInfos = contextType
            .GetProperties()
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(SchemaSet<>));
        var type = propertyInfos.First(info => info.PropertyType.GetGenericArguments()[0] == typeof(T));
        
        var schemaName = type.Name;
        
        var currentAssembly = Assembly.GetExecutingAssembly();

        var migrationTypes = currentAssembly.GetTypes()
            .Where(assemblyType => assemblyType.IsClass && !assemblyType.IsAbstract && assemblyType.IsSubclassOf(typeof(Migration)))
            .OrderBy(migrationType =>
            {
                var className = migrationType.Name;
                return className.Remove(0, className.IndexOf('_'));
            })
            .ToArray();

        var migrationBuilder = new MigrationBuilder();
        var lastGuidDict = new Dictionary<string, Guid>();
        var lastExistedGuidDict = new Dictionary<string, Guid>();

        foreach (var migrationType in migrationTypes)
        {
            var migrationInstance = (Migration)Activator.CreateInstance(migrationType);

            if (migrationInstance is null) continue;
            
            migrationInstance.Up(migrationBuilder);
            foreach (var pair in migrationInstance.GuidDictionary)
            {
                if (!migrationInstance.GuidDictionary.TryGetValue(pair.Key, out _))
                {
                    lastGuidDict.Add(pair.Key, pair.Value);
                }
                else
                {
                    lastGuidDict[pair.Key] = pair.Value;
                }

                if (Schema.Lookup(pair.Value) is not null)
                {
                    lastExistedGuidDict[pair.Key] = pair.Value;
                }
            }
        }

        var schema = Schema.Lookup(lastGuidDict[schemaName]);
        if (schema is not null) return schema;
        if (!lastExistedGuidDict.TryGetValue(schemaName, out _))
        {
            return SchemaMigrationUtils.Create(schemaName, migrationBuilder);
        }

        var schemas = SchemaMigrationUtils.MigrateSchemas(lastExistedGuidDict, migrationBuilder);  //it will migrate all the schemas
        return schemas.Find(migratedSchema => migratedSchema.SchemaName == schemaName);
    }

    public void Delete()
    {
        // var schema = Schema.Lookup(new Guid(Guid));
        // if (schema is not null)
        // {
        //     Context.ActiveDocument!.EraseSchemaAndAllEntities(schema);
        // }
    }
}