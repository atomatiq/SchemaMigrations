namespace SchemaMigrator.Database;

public abstract class Migration
{
    public abstract Dictionary<string, Guid> GuidDictionary { get; set; }

    public abstract void Up(MigrationBuilder migrationBuilder);
}