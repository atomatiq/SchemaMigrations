using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace SchemaMigrations.Database.Core;

internal static class SchemaUtils
{
    internal static bool HasElements(Schema schema, Document context)
    {
        return new FilteredElementCollector(context)
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .Any();
    }

    internal static IList<Element> GetSchemaElements(Schema schema, Document context)
    {
        return new FilteredElementCollector(context)
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .ToElements();
    }
}