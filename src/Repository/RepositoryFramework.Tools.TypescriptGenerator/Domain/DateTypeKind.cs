namespace RepositoryFramework.Tools.TypescriptGenerator.Domain;

/// <summary>
/// Identifies which C# date/time type a TypeDescriptor represents.
/// Used to select the correct parse/format function in generated TypeScript.
/// Only includes types that map to JavaScript Date (DateTime, DateTimeOffset, DateOnly).
/// TimeOnly and TimeSpan remain as string and are not included.
/// </summary>
public enum DateTypeKind
{
    DateTime,
    DateTimeOffset,
    DateOnly
}
