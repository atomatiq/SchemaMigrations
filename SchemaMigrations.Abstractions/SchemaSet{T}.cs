using System.Collections.Generic;

namespace SchemaMigrations.Abstractions;

public class SchemaSet<T> : List<T> where T : new();