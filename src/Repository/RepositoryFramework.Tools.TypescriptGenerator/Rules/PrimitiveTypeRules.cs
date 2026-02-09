using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.Tools.TypescriptGenerator.Rules;

/// <summary>
/// Rules for mapping C# primitive types to TypeScript types.
/// </summary>
public static class PrimitiveTypeRules
{
    /// <summary>
    /// Maps a C# type to its TypeScript equivalent.
    /// Returns null if the type is not a primitive.
    /// </summary>
    public static string? GetTypeScriptType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        return type switch
        {
            // String types
            _ when type == typeof(string) => "string",
            _ when type == typeof(char) => "string",

            // Number types
            _ when type == typeof(byte) => "number",
            _ when type == typeof(sbyte) => "number",
            _ when type == typeof(short) => "number",
            _ when type == typeof(ushort) => "number",
            _ when type == typeof(int) => "number",
            _ when type == typeof(uint) => "number",
            _ when type == typeof(long) => "number",
            _ when type == typeof(ulong) => "number",
            _ when type == typeof(float) => "number",
            _ when type == typeof(double) => "number",
            _ when type == typeof(decimal) => "number",

            // Boolean
            _ when type == typeof(bool) => "boolean",

            // Date/Time types -> string (ISO format)
            _ when type == typeof(DateTime) => "string",
            _ when type == typeof(DateTimeOffset) => "string",
            _ when type == typeof(TimeSpan) => "string",
            _ when type == typeof(DateOnly) => "string",
            _ when type == typeof(TimeOnly) => "string",

            // GUID -> string
            _ when type == typeof(Guid) => "string",

            // Object/dynamic -> any
            _ when type == typeof(object) => "unknown",

            // Not a primitive
            _ => null
        };
    }

    /// <summary>
    /// Maps a type name string to its TypeScript equivalent.
    /// Used for key types specified in CLI.
    /// </summary>
    public static string GetTypeScriptTypeFromName(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "string" => "string",
            "char" => "string",
            "byte" => "number",
            "sbyte" => "number",
            "short" => "number",
            "int16" => "number",
            "ushort" => "number",
            "uint16" => "number",
            "int" => "number",
            "int32" => "number",
            "uint" => "number",
            "uint32" => "number",
            "long" => "number",
            "int64" => "number",
            "ulong" => "number",
            "uint64" => "number",
            "float" => "number",
            "single" => "number",
            "double" => "number",
            "decimal" => "number",
            "bool" => "boolean",
            "boolean" => "boolean",
            "datetime" => "string",
            "datetimeoffset" => "string",
            "timespan" => "string",
            "dateonly" => "string",
            "timeonly" => "string",
            "guid" => "string",
            "object" => "unknown",
            _ => typeName // Return as-is for complex types
        };
    }

    /// <summary>
    /// Returns true if the type is considered primitive for TypeScript purposes.
    /// </summary>
    public static bool IsPrimitive(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return GetTypeScriptType(underlyingType) != null;
    }

    /// <summary>
    /// Returns true if the type name represents a primitive type.
    /// </summary>
    public static bool IsPrimitiveName(string typeName)
    {
        var lower = typeName.ToLowerInvariant();
        return lower is "string" or "char" or "byte" or "sbyte" or "short" or "int16"
            or "ushort" or "uint16" or "int" or "int32" or "uint" or "uint32"
            or "long" or "int64" or "ulong" or "uint64" or "float" or "single"
            or "double" or "decimal" or "bool" or "boolean" or "datetime"
            or "datetimeoffset" or "timespan" or "dateonly" or "timeonly" or "guid";
    }

    /// <summary>
    /// Gets the DateTypeKind for a C# type, or null if it's not a date type that maps to JavaScript Date.
    /// Only DateTime, DateTimeOffset, and DateOnly map to Date. TimeOnly and TimeSpan remain as string.
    /// </summary>
    public static DateTypeKind? GetDateKind(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (underlyingType == typeof(DateTime)) return DateTypeKind.DateTime;
        if (underlyingType == typeof(DateTimeOffset)) return DateTypeKind.DateTimeOffset;
        if (underlyingType == typeof(DateOnly)) return DateTypeKind.DateOnly;
        return null;
    }
}
