namespace SchemaMigrations.Abstractions;

public abstract class Migration
{
    public abstract Dictionary<string, Guid> GuidDictionary { get; }
    public abstract void Up(MigrationBuilder migrationBuilder);
}