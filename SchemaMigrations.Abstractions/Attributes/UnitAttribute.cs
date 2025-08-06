namespace SchemaMigrations.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class UnitAttribute(string unitTypeId, string specTypeId) : Attribute
{
    public string UnitTypeId { get; } = unitTypeId;
    public string SpecTypeId { get; } = specTypeId;
}