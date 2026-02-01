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
    /// The backend factory name used in the API path.
    /// If null or empty, the path will be just the ModelName (e.g., "Rank").
    /// If specified, the path will be "ModelName/BackendFactoryName" (e.g., "Rank/rank").
    /// </summary>
    public string? BackendFactoryName { get; init; }

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

    /// <summary>
    /// Gets the path used for API calls.
    /// If BackendFactoryName is empty, returns just ModelName (e.g., "Rank").
    /// If BackendFactoryName is set, returns "ModelName/BackendFactoryName" (e.g., "Rank/rank").
    /// </summary>
    public string ApiPath
    {
        get
        {
            var modelSimpleName = ModelName.Contains('.') 
                ? ModelName[(ModelName.LastIndexOf('.') + 1)..] 
                : ModelName;

            return string.IsNullOrWhiteSpace(BackendFactoryName) 
                ? modelSimpleName 
                : $"{modelSimpleName}/{BackendFactoryName}";
        }
    }

    public override string ToString()
        => $"{{{ModelName},{KeyName},{Kind},{FactoryName},{BackendFactoryName ?? string.Empty}}}";
}
