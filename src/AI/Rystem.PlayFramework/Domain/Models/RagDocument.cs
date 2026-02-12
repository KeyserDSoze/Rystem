namespace Rystem.PlayFramework;

/// <summary>
/// A single document retrieved from the knowledge base.
/// </summary>
public sealed class RagDocument
{
    /// <summary>
    /// Text content of the document.
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Relevance score (0.0 to 1.0, higher is more relevant).
    /// Note: Score interpretation depends on SearchAlgorithm used.
    /// </summary>
    public double Score { get; init; }
    
    /// <summary>
    /// Unique identifier for this document (if available).
    /// </summary>
    public string? Id { get; init; }
    
    /// <summary>
    /// Source of the document (e.g., filename, URL, database).
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Title or heading of the document.
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// Optional metadata (date, author, category, tags, etc.).
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
