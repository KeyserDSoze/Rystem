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
        var isGeneric = model.GenericTypeParameters.Count > 0;
        var genericParams = isGeneric 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";

        var rawName = $"{baseName}Raw{genericParams}";
        var cleanName = $"{baseName}{genericParams}";
        // Function name must NOT contain <T>; generic params go after = as type params
        var funcName = $"mapRaw{baseName}To{baseName}";

        var sb = new StringBuilder();

        if (isGeneric)
        {
            // Multi-line signature with callback parameters for generic type param mapping
            // Use 'any' types to avoid TS2345: the actual mapper takes TRaw→T, not T→T
            sb.AppendLine($"export const {funcName} = {genericParams}(");
            sb.Append($"  raw: {rawName}");
            foreach (var param in model.GenericTypeParameters)
            {
                sb.AppendLine(",");
                sb.Append($"  map{param}FromRaw: (raw: any) => any = (x: any) => x");
            }
            sb.AppendLine();
            sb.AppendLine($"): {cleanName} => ({{");
        }
        else
        {
            sb.AppendLine($"export const {funcName} = (raw: {rawName}): {cleanName} => ({{");
        }

        foreach (var property in model.Properties)
        {
            var cleanProp = property.TypeScriptName;
            var mapping = GetRawToCleanMapping(property, context, model.GenericTypeParameters);

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
        var isGeneric = model.GenericTypeParameters.Count > 0;
        var genericParams = isGeneric 
            ? $"<{string.Join(", ", model.GenericTypeParameters)}>" 
            : "";

        var rawName = $"{baseName}Raw{genericParams}";
        var cleanName = $"{baseName}{genericParams}";
        // Function name must NOT contain <T>; generic params go after = as type params
        var funcName = $"map{baseName}ToRaw{baseName}";

        var sb = new StringBuilder();

        if (isGeneric)
        {
            // Multi-line signature with callback parameters for generic type param mapping
            // Use 'any' types to avoid TS2345: the actual mapper takes T→TRaw, not T→T
            sb.AppendLine($"export const {funcName} = {genericParams}(");
            sb.Append($"  clean: {cleanName}");
            foreach (var param in model.GenericTypeParameters)
            {
                sb.AppendLine(",");
                sb.Append($"  map{param}ToRaw: (clean: any) => any = (x: any) => x");
            }
            sb.AppendLine();
            sb.AppendLine($"): {rawName} => ({{");
        }
        else
        {
            sb.AppendLine($"export const {funcName} = (clean: {cleanName}): {rawName} => ({{");
        }

        foreach (var property in model.Properties)
        {
            var cleanProp = property.TypeScriptName;
            var rawProp = property.JsonName;
            var mapping = GetCleanToRawMapping(property, context, model.GenericTypeParameters);

            sb.AppendLine($"  {rawProp}: {mapping},");
        }

        sb.AppendLine("});");

        return sb.ToString();
    }

    /// <summary>
    /// Gets the mapping expression for Raw -> Clean.
    /// </summary>
    private static string GetRawToCleanMapping(PropertyDescriptor property, EmitterContext context, IReadOnlyList<string> genericParams)
    {
        var rawAccess = $"raw.{property.JsonName}";
        var type = property.Type;

        // Generic type parameter (T, TKey, etc.) — use callback mapper
        if (type.IsGenericParameter && genericParams.Contains(type.CSharpName))
        {
            var callbackName = $"map{type.CSharpName}FromRaw";
            return property.IsOptional
                ? $"{rawAccess} != null ? {callbackName}({rawAccess}) : undefined"
                : $"{callbackName}({rawAccess})";
        }

        // Date types - parse string to Date
        if (type.IsDate)
        {
            var parseFn = GetDateParseFn(type.DateKind!.Value);
            return property.IsOptional
                ? $"{rawAccess} != null ? {parseFn}({rawAccess}) : undefined"
                : $"{parseFn}({rawAccess})";
        }

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

        // Array of generic parameter (e.g., T[])
        if (type.IsArray && type.ElementType?.IsGenericParameter == true && genericParams.Contains(type.ElementType.CSharpName))
        {
            var callbackName = $"map{type.ElementType.CSharpName}FromRaw";
            return $"{rawAccess}?.map({callbackName}) ?? []";
        }

        // Array of date element types
        if (type.IsArray && type.ElementType != null && type.ElementType.IsDate)
        {
            var parseFn = GetDateParseFn(type.ElementType.DateKind!.Value);
            return $"{rawAccess}?.map({parseFn}) ?? []";
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = GetBaseName(type.ElementType.CSharpName);
            if (context.TypesRequiringRaw.Contains(elementName))
            {
                var mapperName = $"mapRaw{elementName}To{elementName}";
                // Propagate callbacks if element type is a generic model
                if (genericParams.Count > 0 && IsNestedGenericModel(type.ElementType, context))
                {
                    var callbacks = BuildFromRawCallbacks(genericParams);
                    return $"{rawAccess}?.map(item => {mapperName}(item, {callbacks})) ?? []";
                }
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

        // Dictionary with generic parameter value type (e.g., Record<string, T>)
        if (type.IsDictionary && type.ValueType?.IsGenericParameter == true && genericParams.Contains(type.ValueType.CSharpName))
        {
            var callbackName = $"map{type.ValueType.CSharpName}FromRaw";
            return property.IsOptional
                ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, {callbackName}(v)])) : {{}}"
                : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, {callbackName}(v)]))";
        }

        // Dictionary with array-of-generic-parameter value type (e.g., Record<string, T[]>)
        if (type.IsDictionary && type.ValueType is { IsArray: true } && type.ValueType.ElementType?.IsGenericParameter == true
            && genericParams.Contains(type.ValueType.ElementType.CSharpName))
        {
            var callbackName = $"map{type.ValueType.ElementType.CSharpName}FromRaw";
            return property.IsOptional
                ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, v?.map({callbackName}) ?? []])) : {{}}"
                : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, v?.map({callbackName}) ?? []]))";
        }

        // Dictionary with date value type
        if (type.IsDictionary && type.ValueType != null && type.ValueType.IsDate)
        {
            var parseFn = GetDateParseFn(type.ValueType.DateKind!.Value);
            return property.IsOptional
                ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, {parseFn}(v)])) : {{}}"
                : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, {parseFn}(v)]))";
        }

        // Dictionary with array-of-date value type
        if (type.IsDictionary && type.ValueType is { IsArray: true, ElementType.IsDate: true })
        {
            var parseFn = GetDateParseFn(type.ValueType.ElementType!.DateKind!.Value);
            return property.IsOptional
                ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, v?.map({parseFn}) ?? []])) : {{}}"
                : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, v?.map({parseFn}) ?? []]))";
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
                    if (genericParams.Count > 0 && IsNestedGenericModel(type.ValueType.ElementType, context))
                    {
                        var callbacks = BuildFromRawCallbacks(genericParams);
                        return property.IsOptional
                            ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, v?.map(item => {mapperName}(item, {callbacks})) ?? []])) : {{}}"
                            : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, v?.map(item => {mapperName}(item, {callbacks})) ?? []]))";
                    }
                    return property.IsOptional
                        ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, v?.map({mapperName}) ?? []])) : {{}}"
                        : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, v?.map({mapperName}) ?? []]))";
                }
            }
            else if (context.TypesRequiringRaw.Contains(valueName))
            {
                var mapperName = $"mapRaw{valueName}To{valueName}";
                if (genericParams.Count > 0 && IsNestedGenericModel(type.ValueType, context))
                {
                    var callbacks = BuildFromRawCallbacks(genericParams);
                    return property.IsOptional
                        ? $"{rawAccess} ? Object.fromEntries(Object.entries({rawAccess}).map(([k, v]) => [k, {mapperName}(v, {callbacks})])) : {{}}"
                        : $"Object.fromEntries(Object.entries({rawAccess} ?? {{}}).map(([k, v]) => [k, {mapperName}(v, {callbacks})]))";
                }
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
                if (genericParams.Count > 0 && IsNestedGenericModel(type, context))
                {
                    var callbacks = BuildFromRawCallbacks(genericParams);
                    return property.IsOptional
                        ? $"{rawAccess} ? {mapperName}({rawAccess}, {callbacks}) : undefined"
                        : $"{mapperName}({rawAccess}!, {callbacks})";
                }
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
    private static string GetCleanToRawMapping(PropertyDescriptor property, EmitterContext context, IReadOnlyList<string> genericParams)
    {
        var cleanAccess = $"clean.{property.TypeScriptName}";
        var type = property.Type;

        // Generic type parameter (T, TKey, etc.) — use callback mapper
        if (type.IsGenericParameter && genericParams.Contains(type.CSharpName))
        {
            var callbackName = $"map{type.CSharpName}ToRaw";
            return property.IsOptional
                ? $"{cleanAccess} != null ? {callbackName}({cleanAccess}) : undefined"
                : $"{callbackName}({cleanAccess})";
        }

        // Date types - format Date to string
        if (type.IsDate)
        {
            var formatFn = GetDateFormatFn(type.DateKind!.Value);
            return property.IsOptional
                ? $"{cleanAccess} != null ? {formatFn}({cleanAccess}) : undefined"
                : $"{formatFn}({cleanAccess})";
        }

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

        // Array of generic parameter (e.g., T[])
        if (type.IsArray && type.ElementType?.IsGenericParameter == true && genericParams.Contains(type.ElementType.CSharpName))
        {
            var callbackName = $"map{type.ElementType.CSharpName}ToRaw";
            return $"{cleanAccess}?.map({callbackName}) ?? []";
        }

        // Array of date element types
        if (type.IsArray && type.ElementType != null && type.ElementType.IsDate)
        {
            var formatFn = GetDateFormatFn(type.ElementType.DateKind!.Value);
            return $"{cleanAccess}?.map({formatFn}) ?? []";
        }

        // Array of complex types
        if (type.IsArray && type.ElementType != null && !type.ElementType.IsPrimitive && !type.ElementType.IsEnum)
        {
            var elementName = GetBaseName(type.ElementType.CSharpName);
            if (context.TypesRequiringRaw.Contains(elementName))
            {
                var mapperName = $"map{elementName}ToRaw{elementName}";
                if (genericParams.Count > 0 && IsNestedGenericModel(type.ElementType, context))
                {
                    var callbacks = BuildToRawCallbacks(genericParams);
                    return $"{cleanAccess}?.map(item => {mapperName}(item, {callbacks})) ?? []";
                }
                return $"{cleanAccess}?.map({mapperName}) ?? []";
            }
            return $"{cleanAccess} ?? []";
        }

        // Array of primitives
        if (type.IsArray)
        {
            return $"{cleanAccess} ?? []";
        }

        // Dictionary with generic parameter value type (e.g., Record<string, T>)
        if (type.IsDictionary && type.ValueType?.IsGenericParameter == true && genericParams.Contains(type.ValueType.CSharpName))
        {
            var callbackName = $"map{type.ValueType.CSharpName}ToRaw";
            return property.IsOptional
                ? $"{cleanAccess} ? Object.fromEntries(Object.entries({cleanAccess}).map(([k, v]) => [k, {callbackName}(v)])) : {{}}"
                : $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, {callbackName}(v)]))";
        }

        // Dictionary with array-of-generic-parameter value type (e.g., Record<string, T[]>)
        if (type.IsDictionary && type.ValueType is { IsArray: true } && type.ValueType.ElementType?.IsGenericParameter == true
            && genericParams.Contains(type.ValueType.ElementType.CSharpName))
        {
            var callbackName = $"map{type.ValueType.ElementType.CSharpName}ToRaw";
            return property.IsOptional
                ? $"{cleanAccess} ? Object.fromEntries(Object.entries({cleanAccess}).map(([k, v]) => [k, v?.map({callbackName}) ?? []])) : {{}}"
                : $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, v?.map({callbackName}) ?? []]))";
        }

        // Dictionary with date value type
        if (type.IsDictionary && type.ValueType != null && type.ValueType.IsDate)
        {
            var formatFn = GetDateFormatFn(type.ValueType.DateKind!.Value);
            return property.IsOptional
                ? $"{cleanAccess} ? Object.fromEntries(Object.entries({cleanAccess}).map(([k, v]) => [k, {formatFn}(v)])) : {{}}"
                : $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, {formatFn}(v)]))";
        }

        // Dictionary with array-of-date value type
        if (type.IsDictionary && type.ValueType is { IsArray: true, ElementType.IsDate: true })
        {
            var formatFn = GetDateFormatFn(type.ValueType.ElementType!.DateKind!.Value);
            return property.IsOptional
                ? $"{cleanAccess} ? Object.fromEntries(Object.entries({cleanAccess}).map(([k, v]) => [k, v?.map({formatFn}) ?? []])) : {{}}"
                : $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, v?.map({formatFn}) ?? []]))";
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
                    if (genericParams.Count > 0 && IsNestedGenericModel(type.ValueType.ElementType, context))
                    {
                        var callbacks = BuildToRawCallbacks(genericParams);
                        return $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, v?.map(item => {mapperName}(item, {callbacks})) ?? []]))";
                    }
                    return $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, v?.map({mapperName}) ?? []]))";
                }
            }
            else if (context.TypesRequiringRaw.Contains(valueName))
            {
                var mapperName = $"map{valueName}ToRaw{valueName}";
                if (genericParams.Count > 0 && IsNestedGenericModel(type.ValueType, context))
                {
                    var callbacks = BuildToRawCallbacks(genericParams);
                    return $"Object.fromEntries(Object.entries({cleanAccess} ?? {{}}).map(([k, v]) => [k, {mapperName}(v, {callbacks})]))";
                }
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
                if (genericParams.Count > 0 && IsNestedGenericModel(type, context))
                {
                    var callbacks = BuildToRawCallbacks(genericParams);
                    return property.IsOptional
                        ? $"{cleanAccess} ? {mapperName}({cleanAccess}, {callbacks}) : undefined"
                        : $"{mapperName}({cleanAccess}!, {callbacks})";
                }
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
    /// Gets the TypeScript parse function name for a date type (string -> Date).
    /// </summary>
    private static string GetDateParseFn(DateTypeKind kind) => kind switch
    {
        DateTypeKind.DateTime => "parseDateTime",
        DateTypeKind.DateTimeOffset => "parseDateTimeOffset",
        DateTypeKind.DateOnly => "parseDateOnly",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>
    /// Gets the TypeScript format function name for a date type (Date -> string).
    /// </summary>
    private static string GetDateFormatFn(DateTypeKind kind) => kind switch
    {
        DateTypeKind.DateTime => "formatDateTime",
        DateTypeKind.DateTimeOffset => "formatDateTimeOffset",
        DateTypeKind.DateOnly => "formatDateOnly",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>
    /// Builds comma-separated callback names for Raw -> Clean direction.
    /// E.g., for ["T"] -> "mapTFromRaw"; for ["T", "U"] -> "mapTFromRaw, mapUFromRaw"
    /// </summary>
    private static string BuildFromRawCallbacks(IReadOnlyList<string> genericParams)
        => string.Join(", ", genericParams.Select(p => $"map{p}FromRaw"));

    /// <summary>
    /// Builds comma-separated callback names for Clean -> Raw direction.
    /// E.g., for ["T"] -> "mapTToRaw"; for ["T", "U"] -> "mapTToRaw, mapUToRaw"
    /// </summary>
    private static string BuildToRawCallbacks(IReadOnlyList<string> genericParams)
        => string.Join(", ", genericParams.Select(p => $"map{p}ToRaw"));

    /// <summary>
    /// Checks if a TypeDescriptor refers to a generic model (has generic type parameters)
    /// by looking it up in the EmitterContext.
    /// </summary>
    private static bool IsNestedGenericModel(TypeDescriptor type, EmitterContext context)
    {
        // Check by CSharpName (e.g., "EntityVersion`1") in AllModels
        if (context.AllModels.TryGetValue(type.CSharpName, out var model) && model.IsGenericType)
            return true;
        // Fallback: check CLR type (handles case where closed generic was stored in AllModels)
        return type.ClrType?.IsGenericType == true && type.ClrType.ContainsGenericParameters;
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
