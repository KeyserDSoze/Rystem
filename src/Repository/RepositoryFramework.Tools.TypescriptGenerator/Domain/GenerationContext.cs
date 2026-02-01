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

    /// <summary>
    /// Whether to include project dependencies (referenced projects and NuGet packages).
    /// When true, all DLLs in the output directory will be loaded and scanned for types.
    /// Default: false.
    /// </summary>
    public bool IncludeDependencies { get; init; } = false;

    /// <summary>
    /// When <see cref="IncludeDependencies"/> is true, only load dependencies
    /// whose assembly name starts with this prefix.
    /// For example, "MyCompany." will only load "MyCompany.Core.dll", "MyCompany.Models.dll", etc.
    /// If null or empty, all dependencies are loaded (except system assemblies).
    /// </summary>
    public string? DependencyPrefix { get; init; }
}
