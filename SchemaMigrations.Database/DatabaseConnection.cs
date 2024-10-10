using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Nice3point.Revit.Toolkit;
using SchemaMigrations.Database.Core;
using SchemaMigrations.Database.Schemas;

namespace SchemaMigrations.Database;

/// <summary>
/// This class used to save and load T objects in extensible storage entity
/// </summary>
/// <param name="element"></param>
/// <typeparam name="T"></typeparam>
public sealed class DatabaseConnection<T>(Element? element)
    where T : class, new()
{
    private readonly Schema _schema = new Schema<T>().Create();

    private Transaction? _transaction;

    /// <summary>
    /// Saves T object in entity of element of this connection, using T object properties one by one
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void SaveObject(T value)
    {
        if (element is null)
            throw new ArgumentNullException(nameof(element));
        
        var objectType = value.GetType();

        var properties = objectType.GetProperties();
        var entity = element.GetEntity(_schema);
        if (entity is null || entity.Schema is null || !entity.IsValidObject)
        {
            entity = new Entity(_schema);
        }

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var propertyValue = property.GetValue(value);
            if (propertyValue == null) continue;
            
            var propertyType = property.PropertyType;
            var method = entity.GetType()
                .GetMethods().FirstOrDefault(methodInfo =>
                {
                    if (methodInfo.Name != nameof(Entity.Set)) return false;
                    var parameters = methodInfo.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType.IsGenericParameter;
                })!;

            if (propertyType.IsGenericType)
            {
                var genericTypeDefinition = propertyType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>))
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    propertyType = typeof(IList<>).MakeGenericType(elementType);
                }
                else if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    var genericArgs = propertyType.GetGenericArguments();
                    var keyType = genericArgs[0];
                    var valueType = genericArgs[1];
                    propertyType = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
                }
            }

            method.MakeGenericMethod(propertyType).Invoke(entity, [propertyName, propertyValue]);
        }

        element.SetEntity(entity);
    }

    /// <summary>
    /// Read information from entity of element of this connection and constructs a new T object with saved values. If there are no entity, returns new T. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public T LoadObject()
    {
        if (element is null)
            throw new ArgumentNullException(nameof(element));
        
        var entity = element.GetEntity(_schema);
        var obj = new T();
        var objType = typeof(T);

        if (entity is null || !entity.IsValidObject || entity.Schema is null || !entity.Schema.IsValidObject)
        {
            return obj;
        }

        var properties = objType.GetProperties();
        var method = typeof(Entity).GetMethods().FirstOrDefault(methodInfo =>
        {
            if (methodInfo.Name != nameof(Entity.Get)) return false;
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 1 &&
                   parameters[0].ParameterType == typeof(string);
        })!;

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;

            if (propertyType.IsGenericType)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    propertyType = typeof(IList<>).MakeGenericType(elementType);
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericArgs = propertyType.GetGenericArguments();
                    var keyType = genericArgs[0];
                    var valueType = genericArgs[1];
                    propertyType = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
                }
            }

            var value = method
                .MakeGenericMethod(propertyType)
                .Invoke(entity, [property.Name]);

            property.SetValue(obj, value);
        }

        return obj;
    }

    /// <summary>
    /// found all entities of Database Connection schema and delete them, then calls EraseSchemaAndAllEntities for active document
    /// </summary>
    public void Delete()
    {
        using var transaction = new Transaction(Context.ActiveDocument, "Delete data");
        transaction.Start();
        foreach (var entityElement in SchemaUtils.GetSchemaElements(_schema, Context.ActiveDocument!))
        {
            entityElement.DeleteEntity(_schema);
        }
        Context.ActiveDocument!.EraseSchemaAndAllEntities(_schema);

        transaction.Commit();
    }
}