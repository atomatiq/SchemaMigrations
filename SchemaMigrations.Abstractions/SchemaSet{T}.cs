using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions;

/// <summary>
/// Describes a schema (property name) and a stored object (typeof(T)) for Migration generator
/// </summary>
[PublicAPI]
public class SchemaSet<T> : List<T> where T : new();