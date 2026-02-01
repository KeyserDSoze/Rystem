namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Defines the type of repository pattern to generate.
/// </summary>
public enum RepositoryKind
{
    /// <summary>
    /// Full Repository pattern (both read and write operations).
    /// Generates a service with get, insert, update, delete, query methods.
    /// </summary>
    Repository,

    /// <summary>
    /// Query-only pattern (CQRS read side).
    /// Generates a service with get and query methods only.
    /// </summary>
    Query,

    /// <summary>
    /// Command-only pattern (CQRS write side).
    /// Generates a service with insert, update, delete methods only.
    /// </summary>
    Command
}
