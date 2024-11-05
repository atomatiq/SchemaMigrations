using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions.Models;

/// <summary>
/// This class used by generated migrations to describe  future schema
/// </summary>
/// <param name="schemaName"></param>
[PublicAPI]
public class SchemaDescriptor(string schemaName)
{
    /// <summary>
    /// Name of the schema to generate
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// List of schema fields
    /// </summary>
    public List<FieldDescriptor> Fields { get; set; } = [];

    internal void AddField(FieldDescriptor field)
    {
        if (Fields.All(descriptor => descriptor.Name != field.Name))
        {
            Fields.Add(field);
        }
        else
        {
            throw new ArgumentException("Schema descriptor already contains a field with such name");
        }
    }

    internal void RemoveField(string fieldName)
    {
        var existedField = Fields.First(descriptor => descriptor.Name == fieldName);
        Fields.Remove(existedField);
    }

    internal bool HasField(string fieldName)
    {
        return Fields.Any(descriptor => descriptor.Name == fieldName);
    }
}