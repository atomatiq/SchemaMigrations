using JetBrains.Annotations;

namespace SchemaMigrations.Abstractions;

[UsedImplicitly]
public abstract class Migration
{
    [UsedImplicitly] public abstract Dictionary<string, Guid> GuidDictionary { get; }

    [UsedImplicitly]
    public abstract void Up(MigrationBuilder migrationBuilder);
}