using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace SchemaMigrations.Database.Core;

/// <summary>
/// Contains common methods to use with schema class
/// </summary>
public static class SchemaUtils
{
    /// <summary>
    /// Indicates is there at least one entity of the given schema in the given document
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static bool HasElements(Schema schema, Document context)
    {
        return new FilteredElementCollector(context)
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .Any();
    }

    /// <summary>
    /// Return all the elements, which have entity of the given schema in the given document
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IList<Element> GetSchemaElements(Schema schema, Document context)
    {
        return new FilteredElementCollector(context)
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .ToElements();
    }
}