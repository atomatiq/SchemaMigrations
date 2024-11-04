using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions;

[UsedImplicitly]
public class SchemaBuilderData
{
    public required Guid Guid { get; set; }
    public required string Name { get; set; }
    public required string Documentation { get; set; }
    public required string VendorId { get; set; }
}