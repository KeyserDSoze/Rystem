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
    /// The TypeScript-friendly display name (e.g., "EntityVersions<Book>").
    /// For closed generics, includes the concrete type arguments.
    /// Falls back to Name if not explicitly set.
    /// </summary>
    public string TypeScriptName
    {
        get => string.IsNullOrEmpty(_typeScriptName) ? Name : _typeScriptName;
        init => _typeScriptName = value;
    }
    private readonly string _typeScriptName = string.Empty;

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
    /// Whether this model needs both Raw and Clean interfaces.
    /// True when any property has a custom JsonPropertyName or contains a date type
    /// that requires conversion between string (JSON) and Date (TypeScript).
    /// </summary>
    public bool RequiresRawType => Properties.Any(p => p.HasCustomJsonName || p.Type.ContainsDateType);

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
    /// Whether this is a generic type definition (e.g., EntityVersions&lt;T&gt;).
    /// </summary>
    public bool IsGenericType => GenericTypeParameters.Count > 0;

    /// <summary>
    /// Generic type parameters (e.g., ["T", "TKey"]).
    /// Empty list for non-generic types.
    /// </summary>
    public IReadOnlyList<string> GenericTypeParameters { get; init; } = [];

    /// <summary>
    /// For closed generic types (e.g., EntityVersions&lt;Timeline&gt;), 
    /// this is the open generic base type (EntityVersions&lt;T&gt;).
    /// </summary>
    public string? GenericBaseTypeName { get; init; }

    /// <summary>
    /// Gets the TypeScript file name for this model.
    /// For open generics like EntityVersions&lt;T&gt;, generates: entityversions.ts
    /// Closed generics (EntityVersions&lt;Book&gt;) are NOT generated - they reuse the open generic.
    /// </summary>
    public string GetFileName()
    {
        // Always use base name (without backticks) for file naming
        var baseName = GetBaseTypeName();
        return $"{baseName.ToLowerInvariant()}.ts";
    }

    /// <summary>
    /// Gets the TypeScript Raw interface name.
    /// </summary>
    public string GetRawTypeName() => IsGenericType 
        ? $"{GetBaseTypeName()}<{string.Join(", ", GenericTypeParameters)}>Raw"
        : $"{Name}Raw";

    /// <summary>
    /// Gets the TypeScript Clean interface name.
    /// </summary>
    public string GetCleanTypeName() => IsGenericType
        ? $"{GetBaseTypeName()}<{string.Join(", ", GenericTypeParameters)}>"
        : Name;

    /// <summary>
    /// Gets the base type name without generic parameters.
    /// E.g., "EntityVersions" from "EntityVersions`1".
    /// </summary>
    public string GetBaseTypeName()
    {
        var backtickIndex = Name.IndexOf('`');
        return backtickIndex > 0 ? Name[..backtickIndex] : Name;
    }
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
