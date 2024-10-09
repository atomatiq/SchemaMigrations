namespace SchemaMigrator.Database.Schemas;

public class SchemaBuilderData
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string Documentation { get; set; }
    public string VendorId { get; set; }
    public Type ModelType { get; set; }
}