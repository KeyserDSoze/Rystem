namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Contains all the configuration for a TypeScript generation run.
/// </summary>
public sealed record GenerationContext
{
    /// <summary>
    /// The destination folder for generated TypeScript files.
    /// </summary>
    public required string DestinationPath { get; init; }

    /// <summary>
    /// The list of repository descriptors to generate code for.
    /// </summary>
    public required IReadOnlyList<RepositoryDescriptor> Repositories { get; init; }

    /// <summary>
    /// The path to the C# project or assembly to analyze.
    /// If null, will search for a .csproj in the current directory.
    /// </summary>
    public string? ProjectPath { get; init; }

    /// <summary>
    /// Whether to overwrite existing files (default: true).
    /// </summary>
    public bool Overwrite { get; init; } = true;
}
