using Autodesk.Revit.DB.ExtensibleStorage;
using SchemaMigrator.Database.Core;
using SchemaMigrator.Database.Schemas;

namespace SchemaMigrator.Database;

public sealed class DatabaseConnection<T>(Element? element)
    where T : class, new()
{
    private readonly Schema _schema = new Schema<T>().Create();

    private Transaction? _transaction;

    /// <summary>
    ///     Begins a new transaction for database operations.
    /// </summary>
    public void BeginTransaction()
    {
        _transaction = new Transaction(element?.Document, "Save data");
        _transaction.Start();
    }

    /// <summary>
    ///     Closes the connection, committing changes.
    /// </summary>
    public void Close()
    {
        if (_transaction is null) return;
        if (!_transaction.IsValidObject) throw new ObjectDisposedException(nameof(Transaction), "Attempting to close an already closed connection");

        _transaction.Commit();
        _transaction.Dispose();
    }

    public void SaveObject(T value)
    {
        if (element is null)
            throw new ArgumentNullException(nameof(element));
        
        var objType = value.GetType();

        var properties = objType.GetProperties();
        var entity = element.GetEntity(_schema);
        if (entity is null || entity.Schema is null || !entity.IsValidObject)
        {
            entity = new Entity(_schema);
        }

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var propertyValue = property.GetValue(value);
            if (propertyValue != null)
            {
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
        }

        element.SetEntity(entity);
    }

    public T LoadObject()
    {
        if (element is null)
            throw new ArgumentNullException(nameof(element));
        
        var entity = element.GetEntity(_schema);
        var obj = new T();
        var objType = typeof(T);

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