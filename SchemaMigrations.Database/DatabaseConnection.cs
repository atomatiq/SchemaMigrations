using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using JetBrains.Annotations;
using SchemaMigrations.Abstractions.Attributes;
using SchemaMigrations.Database.Core;
using SchemaMigrations.Database.Schemas;

namespace SchemaMigrations.Database;

/// <summary>
/// This class used to save and load T objects in extensible storage entity
/// </summary>
/// <param name="element"></param>
/// <typeparam name="T"></typeparam>
[PublicAPI]
public sealed class DatabaseConnection<T>(Element element)
    where T : class, new()
{
    private readonly Schema _schema = Schema<T>.Create(element);

    /// <summary>
    /// Saves T object in the entity of the element, using T object properties one by one
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="System.ArgumentNullException"></exception>
    public void SaveObject(T value)
    {
        var objectType = value.GetType();

        var properties = objectType.GetProperties();
        var entity = element.GetEntity(_schema);
        if (entity?.Schema is null || !entity.IsValidObject)
        {
            entity = new Entity(_schema);
        }

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var propertyValue = property.GetValue(value);
            if (propertyValue == null) continue;

            var attribute = property.GetCustomAttribute<UnitAttribute>();

            var methodGetByName = entity.GetType()
                .GetMethods().FirstOrDefault(methodInfo =>
                {
                    if (methodInfo.Name != nameof(Entity.Set)) return false;
                    var parameters = methodInfo.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType.IsGenericParameter;
                })!;
            var methodGetByNameAndUnits = entity.GetType()
                .GetMethods().FirstOrDefault(methodInfo =>
                {
                    if (methodInfo.Name != nameof(Entity.Set)) return false;
                    var parameters = methodInfo.GetParameters();
                    return parameters.Length == 3 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType.IsGenericParameter &&
                           parameters[2].ParameterType == typeof(ForgeTypeId);
                })!;

            var propertyType = GetPropertyType(property);

            if (attribute is null)
            {
                methodGetByName.MakeGenericMethod(propertyType).Invoke(entity, [propertyName, propertyValue]);
            }
            else
            {
                methodGetByNameAndUnits.MakeGenericMethod(propertyType).Invoke(entity, [propertyName, propertyValue, new ForgeTypeId(attribute.UnitTypeId)]);
            }
        }

        element.SetEntity(entity);
    }

    /// <summary>
    /// Read information from the entity of the element and constructs a new T object with saved values. If there is no entity, returns new T. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException"></exception>
    public T LoadObject()
    {
        var entity = element.GetEntity(_schema);
        var obj = new T();
        var objType = typeof(T);

        if (entity is null || !entity.IsValidObject || entity.Schema is null || !entity.Schema.IsValidObject)
        {
            return obj;
        }

        var properties = objType.GetProperties();
        var methodSetByName = typeof(Entity).GetMethods().FirstOrDefault(methodInfo =>
        {
            if (methodInfo.Name != nameof(Entity.Get)) return false;
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 1 &&
                   parameters[0].ParameterType == typeof(string);
        })!;

        var methodSetByNameAndUnits = typeof(Entity).GetMethods().FirstOrDefault(methodInfo =>
        {
            if (methodInfo.Name != nameof(Entity.Get)) return false;
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 2 &&
                   parameters[0].ParameterType == typeof(string) &&
                   parameters[1].ParameterType == typeof(ForgeTypeId);
        })!;

        foreach (var property in properties)
        {
            var propertyType = GetPropertyType(property);

            var attribute = property.GetCustomAttribute<UnitAttribute>();

            if (attribute is null)
            {
                var value = methodSetByName
                    .MakeGenericMethod(propertyType)
                    .Invoke(entity, [property.Name]);

                property.SetValue(obj, value);
            }

            else
            {
                var value = methodSetByNameAndUnits
                    .MakeGenericMethod(propertyType)
                    .Invoke(entity, [property.Name, new ForgeTypeId(attribute.UnitTypeId)]);

                property.SetValue(obj, value);
            }
        }

        return obj;
    }

    private static Type GetPropertyType(PropertyInfo property)
    {
        var propertyType = property.PropertyType;

        if (propertyType.IsGenericType)
        {
            propertyType = GetGenericPropertyType(propertyType);
        }

        return propertyType;
    }

    private static Type GetGenericPropertyType(Type propertyType)
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

        return propertyType;
    }

    /// <summary>
    /// Checks is there an existing object in the schema
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException"></exception>
    public bool HasObject()
    {
        var entity = element.GetEntity(_schema);

        return entity is { IsValidObject: true, Schema.IsValidObject: true };
    }

    /// <summary>
    /// Deletes data from current element
    /// </summary>
    /// <exception cref="System.ArgumentNullException"></exception>
    public void DeleteObject()
    {
        var entity = element.GetEntity(_schema);

        if (entity is null || !entity.IsValidObject || entity.Schema is null || !entity.Schema.IsValidObject)
        {
            return;
        }

        element.DeleteEntity(_schema);
    }

    /// <summary>
    ///     Saves the specified value associated with a field in the database.
    /// </summary>
    /// <typeparam name="TProperty">The type of the value to be saved.</typeparam>
    /// <param name="field">The field name in which to save the value.</param>
    /// <param name="value">The value to be saved.</param>
    public void SaveProperty<TProperty>(string field, TProperty value)
    {
        if (value is null) return;

        var entity = element.GetEntity(_schema);
        if (entity?.Schema is null || !entity.IsValidObject)
        {
            entity = new Entity(_schema);
        }

        entity.Set(field, value);
        element.SetEntity(entity);
    }

    /// <summary>
    /// Loads the value associated with the specified field from the database.
    /// </summary>
    /// <typeparam name="TProperty">The type of the value to be loaded.</typeparam>
    /// <param name="fieldName">The field name from which to load the value.</param>
    /// <returns>The value loaded from the database.</returns>
    [Pure]
    public TProperty? LoadProperty<TProperty>(string fieldName)
    {
        var field = _schema.GetField(fieldName);
        var entity = element.GetEntity(_schema);
        return entity is null || field is null ? default : entity.Get<TProperty>(field);
    }

    /// <summary>
    /// found all entities of <see cref="DatabaseConnection{T}"/> schema and delete them, then calls EraseSchemaAndAllEntities for the active document
    /// </summary>
    public void Delete()
    {
        using var transaction = new Transaction(element.Document, "Delete data");
        transaction.Start();
        foreach (var entityElement in SchemaUtils.GetSchemaElements(_schema, element.Document))
        {
            entityElement.DeleteEntity(_schema);
        }

        element.Document.EraseSchemaAndAllEntities(_schema);

        transaction.Commit();
    }
}