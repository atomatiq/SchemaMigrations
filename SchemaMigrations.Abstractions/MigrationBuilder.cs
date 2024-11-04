using JetBrains.Annotations;
using SchemaMigrations.Abstractions.Models;

namespace SchemaMigrations.Abstractions;

[UsedImplicitly]
public class MigrationBuilder
{
    private readonly List<SchemaDescriptor> _schemas = [];
    private List<SchemaBuilderData> BuildersData { get; } = [];

    [UsedImplicitly]
    public void AddSchemaData(SchemaBuilderData data, SchemaDescriptor descriptor)
    {
        BuildersData.Add(data);
        _schemas.Add(descriptor);
    }

    [UsedImplicitly]
    public void UpdateGuid(string schemaName, Guid newGuid)
    {
        BuildersData.First(x => x.Name == schemaName).Guid = newGuid;
    }

    [UsedImplicitly]
    public void AddColumn(string tableName, string name, Type fieldType)
    {
        _schemas.First(schema => schema.SchemaName == tableName).AddField(new FieldDescriptor(name, fieldType));
    }

    [UsedImplicitly]
    public void DropColumn(string tableName, string name)
    {
        _schemas.First(schema => schema.SchemaName == tableName).RemoveField(name);
    }

    [UsedImplicitly]
    public List<FieldDescriptor> GetColumns(string tableName)
    {
        var schema = _schemas.FirstOrDefault(schema => schema.SchemaName == tableName);
        return schema is null ? new List<FieldDescriptor>() : schema.Fields;
    }
}