using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Emits TypeScript Raw interfaces (using JSON property names).
/// </summary>
public static class RawTypeEmitter
{
    /// <summary>
    /// Generates a TypeScript Raw interface from a ModelDescriptor.
    /// </summary>
    public static string Emit(ModelDescriptor model, EmitterContext context)
    {
        if (model.IsEnum)
            throw new ArgumentException("Use EnumEmitter for enum types.", nameof(model));

        var sb = new StringBuilder();

        // Use GetBaseTypeName with generic parameters
        var baseTypeName = model.GetBaseTypeName();
        var genericParams = model.GenericTypeParameters.Count > 0 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";

        sb.AppendLine($"export interface {baseTypeName}Raw{genericParams} {{");

        foreach (var property in model.Properties)
        {
            var tsType = GetRawTypeString(property, context);
            var optional = property.IsOptional ? "?" : "";
            sb.AppendLine($"  {property.JsonName}{optional}: {tsType};");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Gets the TypeScript type string for a property in Raw format.
    /// </summary>
    private static string GetRawTypeString(PropertyDescriptor property, EmitterContext context)
    {
        return GetTypeString(property.Type, context, useRaw: true);
    }

    /// <summary>
    /// Converts a TypeDescriptor to a TypeScript type string.
    /// </summary>
    internal static string GetTypeString(TypeDescriptor type, EmitterContext context, bool useRaw)
    {
        // Primitive types
        if (type.IsPrimitive)
        {
            return type.TypeScriptName;
        }

        // Enum types - use the enum name directly
        if (type.IsEnum)
        {
            return type.CSharpName;
        }

        // Array types
        if (type.IsArray && type.ElementType != null)
        {
            var elementType = GetTypeString(type.ElementType, context, useRaw);
            return $"{elementType}[]";
        }

        // Dictionary types
        if (type.IsDictionary && type.KeyType != null && type.ValueType != null)
        {
            var keyType = GetTypeString(type.KeyType, context, useRaw);
            var valueType = GetTypeString(type.ValueType, context, useRaw);
            return $"Record<{keyType}, {valueType}>";
        }

        // Complex types - check if they need Raw suffix
        // Use TypeScriptName to handle generics correctly (e.g., VersionEntry<T> instead of VersionEntry`1)
        var complexTypeName = type.TypeScriptName;

        // Check if this type requires Raw format
        // For generic types, we need to check the base name without type parameters
        var baseTypeName = type.CSharpName;
        if (baseTypeName.Contains('`'))
        {
            baseTypeName = baseTypeName[..baseTypeName.IndexOf('`')];
        }

        if (useRaw && context.TypesRequiringRaw.Contains(baseTypeName))
        {
            // Insert Raw before the generic parameters
            if (complexTypeName.Contains('<'))
            {
                var genericStart = complexTypeName.IndexOf('<');
                return $"{complexTypeName[..genericStart]}Raw{complexTypeName[genericStart..]}";
            }
            return $"{complexTypeName}Raw";
        }

        return complexTypeName;
    }

    /// <summary>
    /// Generates multiple Raw interfaces.
    /// </summary>
    public static string EmitAll(IEnumerable<ModelDescriptor> models, EmitterContext context)
    {
        var sb = new StringBuilder();

        foreach (var model in models.Where(m => !m.IsEnum && m.RequiresRawType))
        {
            sb.AppendLine(Emit(model, context));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
