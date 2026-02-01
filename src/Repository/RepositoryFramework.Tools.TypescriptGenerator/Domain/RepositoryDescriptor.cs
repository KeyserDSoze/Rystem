namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Describes a repository configuration to generate TypeScript code for.
/// Parsed from CLI input like: {Calendar,LeagueKey,Repository,serieA}
/// </summary>
public sealed record RepositoryDescriptor
{
    /// <summary>
    /// The C# model class name (e.g., "Calendar", "User").
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// The C# key class name or primitive type (e.g., "LeagueKey", "string", "Guid").
    /// </summary>
    public required string KeyName { get; init; }

    /// <summary>
    /// The type of repository pattern: Repository, Query, or Command.
    /// </summary>
    public required RepositoryKind Kind { get; init; }

    /// <summary>
    /// The factory name used in TypeScript RepositoryServices.
    /// Defaults to ModelName if not specified.
    /// </summary>
    public required string FactoryName { get; init; }

    /// <summary>
    /// Returns true if the key is a primitive type (string, int, Guid, etc.)
    /// rather than a custom class.
    /// </summary>
    public bool IsPrimitiveKey => PrimitiveTypes.Contains(KeyName.ToLowerInvariant());

    private static readonly HashSet<string> PrimitiveTypes =
    [
        "string",
        "int",
        "long",
        "short",
        "byte",
        "float",
        "double",
        "decimal",
        "bool",
        "boolean",
        "guid",
        "datetime",
        "datetimeoffset",
        "timespan"
    ];

    public override string ToString()
        => $"{{{ModelName},{KeyName},{Kind},{FactoryName}}}";
}
