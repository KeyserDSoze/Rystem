namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Describes a property of a C# model for TypeScript generation.
/// </summary>
public sealed record PropertyDescriptor
{
    /// <summary>
    /// The original C# property name (e.g., "UserName").
    /// </summary>
    public required string CSharpName { get; init; }

    /// <summary>
    /// The JSON serialization name from [JsonPropertyName] attribute.
    /// If no attribute, equals CSharpName.
    /// </summary>
    public required string JsonName { get; init; }

    /// <summary>
    /// The clean TypeScript property name (camelCase of CSharpName).
    /// </summary>
    public required string TypeScriptName { get; init; }

    /// <summary>
    /// The type descriptor for this property.
    /// </summary>
    public required TypeDescriptor Type { get; init; }

    /// <summary>
    /// Whether this property is required (not nullable and no default).
    /// </summary>
    public required bool IsRequired { get; init; }

    /// <summary>
    /// Whether this property should be optional in TypeScript (use ?:).
    /// </summary>
    public required bool IsOptional { get; init; }

    /// <summary>
    /// Whether the JSON name differs from the C# name.
    /// Used to determine if Raw types are needed.
    /// </summary>
    public bool HasCustomJsonName => !string.Equals(JsonName, CSharpName, StringComparison.Ordinal);

    /// <summary>
    /// Nested complex type that needs to be generated.
    /// Null if the property type is primitive.
    /// </summary>
    public ModelDescriptor? NestedModel { get; init; }
}
