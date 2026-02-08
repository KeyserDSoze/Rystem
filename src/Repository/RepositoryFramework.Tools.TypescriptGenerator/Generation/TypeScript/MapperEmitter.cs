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
        // Function name must NOT contain <T>; generic params go after = as type params
        var funcName = $"mapRaw{baseName}To{baseName}";

        var sb = new StringBuilder();

        // For generics: export const fn = <T>(raw: Raw<T>): Clean<T> => ({
        // For non-generics: export const fn = (raw: Raw): Clean => ({
        sb.AppendLine($"export const {funcName} = {genericParams}(raw: {rawName}): {cleanName} => ({{");

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
        // Function name must NOT contain <T>; generic params go after = as type params
        var funcName = $"map{baseName}ToRaw{baseName}";

        var sb = new StringBuilder();

        // For generics: export const fn = <T>(clean: Clean<T>): Raw<T> => ({
        // For non-generics: export const fn = (clean: Clean): Raw => ({
        sb.AppendLine($"export const {funcName} = {genericParams}(clean: {cleanName}): {rawName} => ({{");

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

        // Union types (AnyOf) - direct pass-through
        if (type.IsUnion)
        {
            return rawAccess;
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = GetBaseName(type.ElementType.CSharpName);
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
            var valueName = GetBaseName(type.ValueType.CSharpName);
            if (type.ValueType.IsArray && type.ValueType.ElementType != null)
            {
                var elementName = GetBaseName(type.ValueType.ElementType.CSharpName);
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
        {
            var typeName = GetBaseName(type.CSharpName);
            if (context.TypesRequiringRaw.Contains(typeName))
            {
                var mapperName = $"mapRaw{typeName}To{typeName}";
                return property.IsOptional
                    ? $"{rawAccess} ? {mapperName}({rawAccess}) : undefined"
                    : $"{mapperName}({rawAccess}!)";
            }
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

        // Union types (AnyOf) - direct pass-through
        if (type.IsUnion)
        {
            return cleanAccess;
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = GetBaseName(type.ElementType.CSharpName);
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
            var valueName = GetBaseName(type.ValueType.CSharpName);
            if (type.ValueType.IsArray && type.ValueType.ElementType != null)
            {
                var elementName = GetBaseName(type.ValueType.ElementType.CSharpName);
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
        {
            var typeName = GetBaseName(type.CSharpName);
            if (context.TypesRequiringRaw.Contains(typeName))
            {
                var mapperName = $"map{typeName}ToRaw{typeName}";
                return property.IsOptional
                    ? $"{cleanAccess} ? {mapperName}({cleanAccess}) : undefined"
                    : $"{mapperName}({cleanAccess}!)";
            }
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
    /// Gets the base name of a C# type, stripping the generic backtick suffix.
    /// E.g., "EntityVersion`1" -> "EntityVersion", "Book" -> "Book".
    /// </summary>
    private static string GetBaseName(string csharpName)
    {
        var backtickIndex = csharpName.IndexOf('`');
        return backtickIndex > 0 ? csharpName[..backtickIndex] : csharpName;
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
