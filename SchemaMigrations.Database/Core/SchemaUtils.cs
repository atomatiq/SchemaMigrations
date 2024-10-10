using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Nice3point.Revit.Extensions;

namespace SchemaMigrations.Database.Core;

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