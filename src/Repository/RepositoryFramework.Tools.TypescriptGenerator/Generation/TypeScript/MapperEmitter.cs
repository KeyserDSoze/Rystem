using System.Text;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Utils;

namespace RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;

/// <summary>
/// Emits TypeScript mapping functions (Raw <-> Clean).
/// </summary>
public static class MapperEmitter
{
    /// <summary>
    /// Generates mapping functions for a model (both directions).
    /// </summary>
    public static string Emit(ModelDescriptor model, EmitterContext context)
    {
        if (model.IsEnum)
            return string.Empty;

        if (!model.RequiresRawType)
            return string.Empty;

        var sb = new StringBuilder();

        // Generate Raw -> Clean mapper
        sb.AppendLine(EmitRawToClean(model, context));
        sb.AppendLine();

        // Generate Clean -> Raw mapper
        sb.AppendLine(EmitCleanToRaw(model, context));

        return sb.ToString();
    }

    /// <summary>
    /// Generates a function to map Raw -> Clean.
    /// </summary>
    public static string EmitRawToClean(ModelDescriptor model, EmitterContext context)
    {
        var baseName = model.GetBaseTypeName();
        var genericParams = model.GenericTypeParameters.Count > 0 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";

        var rawName = $"{baseName}Raw{genericParams}";
        var cleanName = $"{baseName}{genericParams}";
        var funcName = $"mapRaw{baseName}{genericParams}To{baseName}{genericParams}";

        var sb = new StringBuilder();

        sb.AppendLine($"export const {funcName} = (raw: {rawName}): {cleanName} => ({{");

        foreach (var property in model.Properties)
        {
            var cleanProp = property.TypeScriptName;
            var rawProp = property.JsonName;
            var mapping = GetRawToCleanMapping(property, context);

            sb.AppendLine($"  {cleanProp}: {mapping},");
        }

        sb.AppendLine("});");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a function to map Clean -> Raw.
    /// </summary>
    public static string EmitCleanToRaw(ModelDescriptor model, EmitterContext context)
    {
        var baseName = model.GetBaseTypeName();
        var genericParams = model.GenericTypeParameters.Count > 0 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";

        var rawName = $"{baseName}Raw{genericParams}";
        var cleanName = $"{baseName}{genericParams}";
        var funcName = $"map{baseName}{genericParams}ToRaw{baseName}{genericParams}";

        var sb = new StringBuilder();

        sb.AppendLine($"export const {funcName} = (clean: {cleanName}): {rawName} => ({{");

        foreach (var property in model.Properties)
        {
            var cleanProp = property.TypeScriptName;
            var rawProp = property.JsonName;
            var mapping = GetCleanToRawMapping(property, context);

            sb.AppendLine($"  {rawProp}: {mapping},");
        }

        sb.AppendLine("});");

        return sb.ToString();
    }

    /// <summary>
    /// Gets the mapping expression for Raw -> Clean.
    /// </summary>
    private static string GetRawToCleanMapping(PropertyDescriptor property, EmitterContext context)
    {
        var rawAccess = $"raw.{property.JsonName}";
        var type = property.Type;

        // Primitive types - direct mapping
        if (type.IsPrimitive)
        {
            return GetDefaultValue(rawAccess, type, property.IsOptional);
        }

        // Enum types - direct mapping
        if (type.IsEnum)
        {
            return GetDefaultValue(rawAccess, type, property.IsOptional);
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = type.ElementType.CSharpName;
            if (context.TypesRequiringRaw.Contains(elementName))
            {
                var mapperName = $"mapRaw{elementName}To{elementName}";
                return property.IsOptional
                    ? $"{rawAccess}?.map({mapperName}) ?? []"
                    : $"{rawAccess}?.map({mapperName}) ?? []";
            }
            return $"{rawAccess} ?? []";
        }

        // Array of primitives
        if (type.IsArray)
        {
            return $"{rawAccess} ?? []";
        }

        // Dictionary with complex value type
        if (type.IsDictionary && type.ValueType != null && !type.ValueType.IsPrimitive && !type.ValueType.IsEnum)
        {
            var valueName = type.ValueType.CSharpName;
            if (type.ValueType.IsArray && type.ValueType.ElementType != null)
            {
                var elementName = type.ValueType.ElementType.CSharpName;
                if (context.TypesRequiringRaw.Contains(elementName))
                {
                    var mapperName = $"mapRaw{elementName}To{elementName}";
                    return property.IsOptional
                        ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, v?.map({mapperName}) ?? []])) : {{}}"
                        : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, v?.map({mapperName}) ?? []]))";
                }
            }
            else if (context.TypesRequiringRaw.Contains(valueName))
            {
                var mapperName = $"mapRaw{valueName}To{valueName}";
                return property.IsOptional
                    ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, {mapperName}(v)])) : {{}}"
                    : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, {mapperName}(v)]))";
            }
        }

        // Dictionary
        if (type.IsDictionary)
        {
            return $"{rawAccess} ?? {{}}";
        }

        // Complex type
        if (context.TypesRequiringRaw.Contains(type.CSharpName))
        {
            var mapperName = $"mapRaw{type.CSharpName}To{type.CSharpName}";
            return property.IsOptional
                ? $"{rawAccess} ? {mapperName}({rawAccess}) : null"
                : $"{mapperName}({rawAccess}!)";
        }

        return rawAccess;
    }

    /// <summary>
    /// Gets the mapping expression for Clean -> Raw.
    /// </summary>
    private static string GetCleanToRawMapping(PropertyDescriptor property, EmitterContext context)
    {
        var cleanAccess = $"clean.{property.TypeScriptName}";
        var type = property.Type;

        // Primitive types - direct mapping
        if (type.IsPrimitive || type.IsEnum)
        {
            return cleanAccess;
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = type.ElementType.CSharpName;
            if (context.TypesRequiringRaw.Contains(elementName))
            {
                var mapperName = $"map{elementName}ToRaw{elementName}";
                return $"{cleanAccess}?.map({mapperName}) ?? []";
            }
            return $"{cleanAccess} ?? []";
        }

        // Array of primitives
        if (type.IsArray)
        {
            return $"{cleanAccess} ?? []";
        }

        // Dictionary with complex value type
        if (type.IsDictionary && type.ValueType != null && !type.ValueType.IsPrimitive && !type.ValueType.IsEnum)
        {
            var valueName = type.ValueType.CSharpName;
            if (type.ValueType.IsArray && type.ValueType.ElementType != null)
            {
                var elementName = type.ValueType.ElementType.CSharpName;
                if (context.TypesRequiringRaw.Contains(elementName))
                {
                    var mapperName = $"map{elementName}ToRaw{elementName}";
                    return $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, v?.map({mapperName}) ?? []]))";
                }
            }
            else if (context.TypesRequiringRaw.Contains(valueName))
            {
                var mapperName = $"map{valueName}ToRaw{valueName}";
                return $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, {mapperName}(v)]))";
            }
        }

        // Dictionary
        if (type.IsDictionary)
        {
            return $"{cleanAccess} ?? {{}}";
        }

        // Complex type
        if (context.TypesRequiringRaw.Contains(type.CSharpName))
        {
            var mapperName = $"map{type.CSharpName}ToRaw{type.CSharpName}";
            return property.IsOptional
                ? $"{cleanAccess} ? {mapperName}({cleanAccess}) : null"
                : $"{mapperName}({cleanAccess}!)";
        }

        return cleanAccess;
    }

    /// <summary>
    /// Gets a default value expression with null coalescing.
    /// </summary>
    private static string GetDefaultValue(string access, TypeDescriptor type, bool isOptional)
    {
        if (!isOptional)
            return access;

        return type.TypeScriptName switch
        {
            "number" => $"{access} ?? 0",
            "string" => access,
            "boolean" => $"{access} ?? false",
            _ => access
        };
    }

    /// <summary>
    /// Generates mappers for all models that need them.
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
