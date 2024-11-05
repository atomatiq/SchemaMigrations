namespace SchemaMigrations.Generator.MigrationTool;

internal static class ChangeDetector
{
    internal static List<string> DetectChanges(Type newModelType, string schemaName, Dictionary<string, Type> lastSnapshot)
    {
        var changes = new List<string>();
        var currentProperties = newModelType.GetProperties()
            .ToDictionary(property => property.Name, property => property.PropertyType);

        foreach (var pair in currentProperties)
        {
            if (!lastSnapshot.TryGetValue(pair.Key, out var value))
            {
                var type = pair.Value;
                if (type.IsGenericType)
                {
                    var genericTypeName = type.GetGenericTypeDefinition().Name;
                    var genericArguments = string.Join(", ", type.GetGenericArguments().Select(t => t.Name));
                    var fullTypeName = $"{genericTypeName.Substring(0, genericTypeName.IndexOf('`'))}<{genericArguments}>";
                    changes.Add( $"""AddColumn("{schemaName}", "{pair.Key}", typeof({fullTypeName}))""");
                }
                else
                {
                    changes.Add( $"""AddColumn("{schemaName}", "{pair.Key}", typeof({type.Name}))""");
                }
            }
            else if (value != pair.Value)
            {
                // TODO: support type modification
                //changes.Add($@"ModifyColumn(""{pair.Key}"", {pair.Value.Name}))");
            }
        }

        foreach (var property in lastSnapshot.Keys)
        {
            if (!currentProperties.ContainsKey(property))
            {
                changes.Add($"DropColumn(\"{schemaName}\", \"{property}\")");
            }
        }

        return changes;
    }
}