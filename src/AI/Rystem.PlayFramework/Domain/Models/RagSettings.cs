namespace Rystem.PlayFramework;

/// <summary>
/// Configuration settings for RAG (Retrieval-Augmented Generation).
/// </summary>
public sealed class RagSettings
{
    /// <summary>
    /// Whether RAG is enabled for this configuration.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Number of top results to retrieve from the knowledge base.
    /// Default: 10.
    /// </summary>
    public int TopK { get; set; } = 10;
    
    /// <summary>
    /// Vector search algorithm to use for similarity matching.
    /// Default: CosineSimilarity.
    /// </summary>
    public VectorSearchAlgorithm SearchAlgorithm { get; set; } = VectorSearchAlgorithm.CosineSimilarity;
    
    /// <summary>
    /// Factory key used to resolve IRagService (can be null/empty for default).
    /// </summary>
    public string? FactoryKey { get; set; }
    
    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0).
    /// Documents below this score are filtered out.
    /// Default: null (no filtering).
    /// </summary>
    public double? MinimumScore { get; set; }
    
    /// <summary>
    /// Custom settings for specific implementations (Azure-specific, Pinecone-specific, etc.).
    /// Use this to pass provider-specific configuration.
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
