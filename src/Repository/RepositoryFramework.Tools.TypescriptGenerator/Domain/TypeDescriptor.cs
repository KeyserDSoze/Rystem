namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Describes a C# type for TypeScript generation.
/// </summary>
public sealed record TypeDescriptor
{
    /// <summary>
    /// The original C# type name (e.g., "String", "Calendar", "List`1").
    /// </summary>
    public required string CSharpName { get; init; }

    /// <summary>
    /// The full C# type name including namespace.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// The TypeScript equivalent type name.
    /// </summary>
    public required string TypeScriptName { get; init; }

    /// <summary>
    /// Whether this is a primitive type (string, number, boolean, etc.).
    /// </summary>
    public required bool IsPrimitive { get; init; }

    /// <summary>
    /// Whether this type is nullable.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Whether this is an array or collection type.
    /// </summary>
    public required bool IsArray { get; init; }

    /// <summary>
    /// Whether this is a dictionary/record type.
    /// </summary>
    public required bool IsDictionary { get; init; }

    /// <summary>
    /// Whether this is an enum type.
    /// </summary>
    public required bool IsEnum { get; init; }

    /// <summary>
    /// For collections/arrays, the element type descriptor.
    /// </summary>
    public TypeDescriptor? ElementType { get; init; }

    /// <summary>
    /// For dictionaries, the key type descriptor.
    /// </summary>
    public TypeDescriptor? KeyType { get; init; }

    /// <summary>
    /// For dictionaries, the value type descriptor.
    /// </summary>
    public TypeDescriptor? ValueType { get; init; }

    /// <summary>
    /// The underlying .NET Type (for further analysis if needed).
    /// </summary>
    public Type? ClrType { get; init; }

    /// <summary>
    /// Gets the TypeScript type string for use in generated code.
    /// </summary>
    public string ToTypeScriptString()
    {
        var typeName = TypeScriptName;

        if (IsArray && ElementType != null)
        {
            typeName = $"{ElementType.ToTypeScriptString()}[]";
        }
        else if (IsDictionary && KeyType != null && ValueType != null)
        {
            typeName = $"Record<{KeyType.ToTypeScriptString()}, {ValueType.ToTypeScriptString()}>";
        }

        if (IsNullable && !IsPrimitive)
        {
            typeName = $"{typeName} | null";
        }

        return typeName;
    }
}
