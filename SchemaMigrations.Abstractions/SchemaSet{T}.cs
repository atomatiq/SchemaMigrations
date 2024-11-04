using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions;

[UsedImplicitly]
public class SchemaSet<T> : List<T> where T : new();