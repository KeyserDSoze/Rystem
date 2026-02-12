namespace Rystem.PlayFramework;

/// <summary>
/// Vector search algorithms for RAG similarity search.
/// </summary>
public enum VectorSearchAlgorithm
{
    /// <summary>
    /// Cosine similarity (1 - cosine distance). Default for most embeddings.
    /// Range: -1 to 1 (higher is more similar).
    /// </summary>
    CosineSimilarity,
    
    /// <summary>
    /// Dot product similarity. Efficient but requires normalized vectors.
    /// Range: -∞ to +∞ (higher is more similar).
    /// </summary>
    DotProduct,
    
    /// <summary>
    /// Euclidean distance (L2 distance). Lower values indicate higher similarity.
    /// Range: 0 to +∞ (lower is more similar).
    /// </summary>
    EuclideanDistance,
    
    /// <summary>
    /// Manhattan distance (L1 distance). Lower values indicate higher similarity.
    /// Range: 0 to +∞ (lower is more similar).
    /// </summary>
    ManhattanDistance
}
