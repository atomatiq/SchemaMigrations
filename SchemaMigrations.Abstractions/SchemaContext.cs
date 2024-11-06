using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions;

/// <summary>
/// Base class to store all the schema sets in a project. Migration generator uses properties of its inheritor to generate a migration.
/// Every schema must be added here as a <see cref="SchemaSet{T}"/>, where T is stored object in this schema
/// </summary>
[PublicAPI]
public abstract class SchemaContext;