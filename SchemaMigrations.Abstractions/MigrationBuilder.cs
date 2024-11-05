using JetBrains.Annotations;
using SchemaMigrations.Abstractions.Models;

namespace SchemaMigrations.Abstractions;

/// <summary>
/// The class to collect all the data about all the generated migrations
/// </summary>
[PublicAPI]
public class MigrationBuilder
{
    /// <summary>
    /// List of all the schema descriptors from the generated migrations
    /// </summary>
    public List<SchemaDescriptor> Schemas { get; } = [];

    /// <summary>
    /// List of base data for schema creation for all the schema descriptors from the generated migrations
    /// </summary>
    public List<SchemaBuilderData> BuildersData { get; } = [];

    /// <summary>
    /// Method created inside the migration to add new schema
    /// </summary>
    public void AddSchemaData(SchemaBuilderData data, SchemaDescriptor descriptor)
    {
        BuildersData.Add(data);
        Schemas.Add(descriptor);
    }

    /// <summary>
    /// Method created inside the migration to update guid of schema
    /// </summary>
    public void UpdateGuid(string schemaName, Guid newGuid)
    {
        BuildersData.First(x => x.Name == schemaName).Guid = newGuid;
    }

    /// <summary>
    /// Method created inside the migration to add new field to the schema
    /// </summary>
    public void AddColumn(string schemaName, string name, Type fieldType)
    {
        Schemas.First(schema => schema.SchemaName == schemaName).AddField(new FieldDescriptor(name, fieldType));
    }

    /// <summary>
    /// Method created inside the migration to remove a field from the schema
    /// </summary>
    public void DropColumn(string schemaName, string name)
    {
        Schemas.First(schema => schema.SchemaName == schemaName).RemoveField(name);
    }

    /// <summary>
    /// Method used by generator to check is the migration actual or not
    /// </summary>
    [UsedImplicitly]
    public List<FieldDescriptor> GetColumns(string schemaName)
    {
        var schema = Schemas.FirstOrDefault(schema => schema.SchemaName == schemaName);
        return schema is null ? [] : schema.Fields;
    }
}