namespace SchemaMigrations.Abstractions;

// ReSharper disable once ClassNeverInstantiated.Global
public class SchemaBuilderData
{
    public required Guid Guid { get; set; }
    public required string Name { get; set; }
    public required string Documentation { get; set; }
    public required string VendorId { get; set; }
}