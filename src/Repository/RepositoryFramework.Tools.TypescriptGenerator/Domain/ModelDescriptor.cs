namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Describes a C# model (class/record/struct) for TypeScript generation.
/// </summary>
public sealed record ModelDescriptor
{
    /// <summary>
    /// The C# class/record name (e.g., "Calendar").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The full C# type name including namespace.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// The namespace of the type.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// All properties of this model.
    /// </summary>
    public required IReadOnlyList<PropertyDescriptor> Properties { get; init; }

    /// <summary>
    /// Whether this model has any property with a custom JsonPropertyName.
    /// If true, we need to generate both Raw and Clean interfaces.
    /// </summary>
    public bool RequiresRawType => Properties.Any(p => p.HasCustomJsonName);

    /// <summary>
    /// Whether this is an enum type.
    /// </summary>
    public required bool IsEnum { get; init; }

    /// <summary>
    /// For enum types, the enum values.
    /// </summary>
    public IReadOnlyList<EnumValueDescriptor>? EnumValues { get; init; }

    /// <summary>
    /// All nested complex types that need to be generated.
    /// </summary>
    public IReadOnlyList<ModelDescriptor> NestedTypes { get; init; } = [];

    /// <summary>
    /// The depth at which this model was discovered (for dependency resolution).
    /// Lower depth = closer to root model.
    /// </summary>
    public int DiscoveryDepth { get; init; } = 0;

    /// <summary>
    /// The model that first discovered this type (for dependency resolution).
    /// </summary>
    public string? DiscoveredByModel { get; init; }

    /// <summary>
    /// The underlying .NET Type.
    /// </summary>
    public Type? ClrType { get; init; }

    /// <summary>
    /// Gets the TypeScript file name for this model.
    /// </summary>
    public string GetFileName() => $"{Name.ToLowerInvariant()}.ts";

    /// <summary>
    /// Gets the TypeScript Raw interface name.
    /// </summary>
    public string GetRawTypeName() => $"{Name}Raw";

    /// <summary>
    /// Gets the TypeScript Clean interface name.
    /// </summary>
    public string GetCleanTypeName() => Name;
}

/// <summary>
/// Describes an enum value.
/// </summary>
public sealed record EnumValueDescriptor
{
    /// <summary>
    /// The enum member name (e.g., "Active").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The numeric value of the enum member.
    /// </summary>
    public required int Value { get; init; }
}
