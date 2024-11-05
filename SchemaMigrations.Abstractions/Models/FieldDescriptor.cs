using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions.Models;

/// <summary>
/// This class used by generated migrations to describe fields of future schema
/// </summary>
/// <param name="Name">Field Name</param>
/// <param name="Type">Field Type</param>
[PublicAPI]
public record FieldDescriptor(string Name, Type Type);