using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Emits TypeScript Clean interfaces (using readable property names).
/// </summary>
public static class CleanTypeEmitter
{
    /// <summary>
    /// Generates a TypeScript Clean interface from a ModelDescriptor.
    /// </summary>
    public static string Emit(ModelDescriptor model, EmitterContext context)
    {
        if (model.IsEnum)
            throw new ArgumentException("Use EnumEmitter for enum types.", nameof(model));

        var sb = new StringBuilder();

        // Use TypeScriptName which already has clean generic syntax (EntityVersions<T>)
        sb.AppendLine($"export interface {model.TypeScriptName} {{");

        foreach (var property in model.Properties)
        {
            var tsType = GetCleanTypeString(property, context);
            var optional = property.IsOptional ? "?" : "";
            sb.AppendLine($"  {property.TypeScriptName}{optional}: {tsType};");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Gets the TypeScript type string for a property in Clean format.
    /// </summary>
    private static string GetCleanTypeString(PropertyDescriptor property, EmitterContext context)
    {
        return GetTypeString(property.Type, context);
    }

    /// <summary>
    /// Converts a TypeDescriptor to a TypeScript type string (clean format).
    /// </summary>
    internal static string GetTypeString(TypeDescriptor type, EmitterContext context)
    {
        // Primitive types
        if (type.IsPrimitive)
        {
            return type.IsDate ? "Date" : type.TypeScriptName;
        }

        // Enum types - use the enum name directly
        if (type.IsEnum)
        {
            return type.CSharpName;
        }

        // Union types (AnyOf<T0, T1, ...>) - emit as T0 | T1 | ...
        if (type.IsUnion && type.UnionTypes != null)
        {
            var memberTypes = type.UnionTypes.Select(t => GetTypeString(t, context));
            return string.Join(" | ", memberTypes);
        }

        // Array types
        if (type.IsArray && type.ElementType != null)
        {
            var elementType = GetTypeString(type.ElementType, context);
            return $"{elementType}[]";
        }

        // Dictionary types
        if (type.IsDictionary && type.KeyType != null && type.ValueType != null)
        {
            var keyType = GetTypeString(type.KeyType, context);
            var valueType = GetTypeString(type.ValueType, context);
            return $"Record<{keyType}, {valueType}>";
        }

        // Complex types - use TypeScript-friendly name (handles generics correctly)
        return type.TypeScriptName;
    }

    /// <summary>
    /// Generates multiple Clean interfaces.
    /// </summary>
    public static string EmitAll(IEnumerable<ModelDescriptor> models, EmitterContext context)
    {
        var sb = new StringBuilder();

        foreach (var model in models.Where(m => !m.IsEnum))
        {
            sb.AppendLine(Emit(model, context));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
