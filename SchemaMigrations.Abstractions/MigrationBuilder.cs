using System;
using System.Collections.Generic;
using System.Linq;
using SchemaMigrations.Abstractions.Models;

namespace SchemaMigrations.Abstractions;

public class MigrationBuilder
{
    public List<SchemaDescriptor> Schemas { get; } = [];
    public List<SchemaBuilderData> BuildersData { get; } = [];
    
    public void AddSchemaData(SchemaBuilderData data, SchemaDescriptor descriptor)
    {
        BuildersData.Add(data);
        Schemas.Add(descriptor);
    }

    public void UpdateGuid(string schemaName, Guid newGuid)
    {
        BuildersData.First(x => x.Name == schemaName).Guid = newGuid;
    }

    public void AddColumn(string tableName, string name, Type fieldType)
    {
        Schemas.First(schema => schema.SchemaName == tableName).AddField(new FieldDescriptor(name, fieldType));
    }
    
    public void DropColumn(string tableName, string name)
    {
        Schemas.First(schema => schema.SchemaName == tableName).RemoveField(name);
    }

    public List<FieldDescriptor> GetColumns(string tableName)
    {
        var schema = Schemas.FirstOrDefault(schema => schema.SchemaName == tableName);
        return schema is null ? new List<FieldDescriptor>() : schema.Fields;
    }
}