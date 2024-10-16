using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using SchemaMigrations.Abstractions;

namespace SchemaMigrations.Database.Core;

internal static class SchemaMigrationUtils
{
    private static void MigrateSchema(Schema oldSchema, Schema newSchema, Document context)
    {
        var instances = new FilteredElementCollector(context)
            .WhereElementIsNotElementType()
            .ToArray();
        var types = new FilteredElementCollector(context)
            .WhereElementIsElementType()
            .ToArray();
        foreach (var instance in instances)
        {
            MigrateElement(instance, oldSchema, newSchema);
        }

        foreach (var type in types)
        {
            MigrateElement(type, oldSchema, newSchema);
        }

        context.EraseSchemaAndAllEntities(oldSchema);
    }

    internal static List<Schema> MigrateSchemas(Dictionary<string, Guid> lastExistedGuids, MigrationBuilder migrationBuilder, Document context)
    {
        var result = new List<Schema>();
        foreach (var guidPair in lastExistedGuids)
        {
            var existingSchema = Schema.Lookup(guidPair.Value);
            var resultSchema = Create(guidPair.Key, migrationBuilder);
            if (existingSchema is not null && SchemaUtils.HasElements(existingSchema, context))
            {
                MigrateSchema(existingSchema, resultSchema, context);
            }

            result.Add(resultSchema);
        }

        return result;
    }

    internal static Schema Create(string schemaName, MigrationBuilder migrationBuilder)
    {
        var data = migrationBuilder.BuildersData.First(data => data.Name == schemaName);
        var schemaDescriptor = migrationBuilder.Schemas.First(schema => schema.SchemaName == schemaName);

        var schemaBuilder = new SchemaBuilder(data.Guid)
            .SetSchemaName(data.Name)
            .SetDocumentation(data.Documentation)
            .SetVendorId(data.VendorId);

        foreach (var field in schemaDescriptor.Fields)
        {
            var propertyType = field.Type;

            if (propertyType.IsGenericType)
            {
                var genericTypeDefinition = propertyType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>))
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    schemaBuilder.AddArrayField(field.Name, elementType);
                }
                else if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    var genericArgs = propertyType.GetGenericArguments();
                    var keyType = genericArgs[0];
                    var valueType = genericArgs[1];
                    schemaBuilder.AddMapField(field.Name, keyType, valueType);
                }
            }
            else
            {
                schemaBuilder.AddSimpleField(field.Name, propertyType);
            }
        }

        var resultSchema = schemaBuilder.Finish();
        return resultSchema;
    }

    private static void MigrateElement(Element element, Schema oldSchema, Schema newSchema)
    {
        var firstEntity = element.GetEntity(oldSchema);
        if (firstEntity is null || firstEntity.Schema is null || !firstEntity.Schema.IsValidObject)
            return;

        var secondEntity = new Entity(newSchema);

        var oldFields = oldSchema.ListFields();
        foreach (var field in oldFields)
        {
            var getMethod = firstEntity.GetType().GetMethod(nameof(Entity.Get), [typeof(Field)])!;
            var setMethod = secondEntity.GetType().GetMethods().FirstOrDefault(methodInfo =>
            {
                if (methodInfo.Name != nameof(Entity.Set)) return false;
                var parameters = methodInfo.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType == typeof(string) &&
                       parameters[1].ParameterType.IsGenericParameter;
            })!;
            var genericSetMethod = MakeGenericInvoker(field, setMethod);
            var genericGetMethod = MakeGenericInvoker(field, getMethod);
            var value = genericGetMethod.Invoke(firstEntity, [field]);
            genericSetMethod.Invoke(secondEntity, [field.FieldName, value]);
        }

        element.SetEntity(secondEntity);
        element.DeleteEntity(firstEntity.Schema);
    }

    private static MethodInfo MakeGenericInvoker(Field field, MethodInfo invoker)
    {
        var containerType = field.ContainerType switch
        {
            ContainerType.Simple => field.ValueType,
            ContainerType.Array => typeof(IList<>).MakeGenericType(field.ValueType),
            ContainerType.Map => typeof(IDictionary<,>).MakeGenericType(field.KeyType, field.ValueType),
            _ => throw new ArgumentOutOfRangeException()
        };

        return invoker.MakeGenericMethod(containerType);
    }
}