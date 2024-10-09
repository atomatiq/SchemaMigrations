using Autodesk.Revit.DB.ExtensibleStorage;

namespace SchemaMigrator.Database.Core;

public static class SchemaUtils
{
    public static bool HasElements(Schema schema, Document context)
    {
        return context.GetElements()
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .Any();
    }

    public static Element[] GetSchemaElements(Schema schema, Document context)
    {
        return context.GetElements()
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .ToArray();
    }
}