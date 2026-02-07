using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Emits TypeScript helper classes with utility methods.
/// </summary>
public static class HelperEmitter
{
    /// <summary>
    /// Generates a helper class for a model if it has complex logic needs.
    /// </summary>
    public static string Emit(ModelDescriptor model, EmitterContext context)
    {
        if (model.IsEnum)
            return string.Empty;

        // Only generate helpers for models with nested types or arrays
        var hasArrayProperties = model.Properties.Any(p => p.Type.IsArray);
        var hasDictionaryProperties = model.Properties.Any(p => p.Type.IsDictionary);

        if (!hasArrayProperties && !hasDictionaryProperties)
            return string.Empty;

        var sb = new StringBuilder();
        var baseName = model.GetBaseTypeName();
        var genericParams = model.GenericTypeParameters.Count > 0 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";
        var typeName = $"{baseName}{genericParams}";
        var helperName = $"{baseName}Helper";

        sb.AppendLine($"export class {helperName} {{");

        // Generate getter helpers for array properties
        foreach (var property in model.Properties.Where(p => p.Type.IsArray))
        {
            var methodName = $"get{property.CSharpName.ToPascalCase()}";
            var elementType = CleanTypeEmitter.GetTypeString(property.Type.ElementType!, context);
            var paramName = baseName.ToCamelCase();

            sb.AppendLine($"  static {methodName}{genericParams}({paramName}: {typeName}): {elementType}[] {{");
            sb.AppendLine($"    return {paramName}.{property.TypeScriptName} ?? [];");
            sb.AppendLine("  }");
            sb.AppendLine();
        }

        // Generate getter helpers for dictionary properties
        foreach (var property in model.Properties.Where(p => p.Type.IsDictionary))
        {
            var keyType = property.Type.KeyType?.TypeScriptName ?? "string";
            var valueType = CleanTypeEmitter.GetTypeString(property.Type.ValueType!, context);
            var methodName = $"get{property.CSharpName.ToPascalCase()}Keys";
            var paramName = baseName.ToCamelCase();

            sb.AppendLine($"  static {methodName}{genericParams}({paramName}: {typeName}): {keyType}[] {{");
            sb.AppendLine($"    return Object.keys({paramName}.{property.TypeScriptName} ?? {{}});");
            sb.AppendLine("  }");
            sb.AppendLine();

            var getValueMethod = $"get{property.CSharpName.ToPascalCase()}Value";
            sb.AppendLine($"  static {getValueMethod}{genericParams}({paramName}: {typeName}, key: {keyType}): {valueType} | undefined {{");
            sb.AppendLine($"    return {paramName}.{property.TypeScriptName}?.[key];");
            sb.AppendLine("  }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates helpers for all models that need them.
    /// </summary>
    public static string EmitAll(IEnumerable<ModelDescriptor> models, EmitterContext context)
    {
        var sb = new StringBuilder();

        foreach (var model in models.Where(m => !m.IsEnum))
        {
            var helper = Emit(model, context);
            if (!string.IsNullOrEmpty(helper))
            {
                sb.AppendLine(helper);
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }
}
