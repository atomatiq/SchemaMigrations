using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Nice3point.Revit.Extensions;

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
        return context.GetElements()
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .Any();
    }

    /// <summary>
    /// Return all the elements, which have entity of the given schema in the given document
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Element[] GetSchemaElements(Schema schema, Document context)
    {
        return context.GetElements()
            .WherePasses(new ExtensibleStorageFilter(schema.GUID))
            .ToArray();
    }
}