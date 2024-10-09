namespace ConsoleMigrationTool.MigrationTool;

public static class ChangeDetector
{
    public static List<string> DetectChanges(Type newModelType, string schemaName, Dictionary<string, Type> lastSnapshot)
    {
        var changes = new List<string>();
        var currentProperties = newModelType.GetProperties()
            .ToDictionary(prop => prop.Name, prop => prop.PropertyType);

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
                    changes.Add( $"AddColumn(\"{schemaName}\", \"{pair.Key}\", typeof({fullTypeName}))");
                }
                else
                {
                    changes.Add( $"AddColumn(\"{schemaName}\", \"{pair.Key}\", typeof({type.Name}))");
                }
            }
            else if (value != pair.Value)
            {
                changes.Add($@"ModifyColumn(""{pair.Key}"", {pair.Value.Name}))");
            }
        }

        foreach (var property in lastSnapshot.Keys)
        {
            if (!currentProperties.ContainsKey(property))
            {
                changes.Add($"DropColumn({property})");
            }
        }

        return changes;
    }
}