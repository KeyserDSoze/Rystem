namespace Rystem.PlayFramework;

/// <summary>
/// Common RAG provider identifiers (optional, can use string keys instead).
/// </summary>
public enum RagProvider
{
    /// <summary>
    /// Azure AI Search (formerly Azure Cognitive Search).
    /// </summary>
    Azure,
    
    /// <summary>
    /// Pinecone vector database.
    /// </summary>
    Pinecone,
    
    /// <summary>
    /// Qdrant vector database.
    /// </summary>
    Qdrant,
    
    /// <summary>
    /// Chroma vector database.
    /// </summary>
    Chroma,
    
    /// <summary>
    /// Weaviate vector database.
    /// </summary>
    Weaviate,
    
    /// <summary>
    /// Milvus vector database.
    /// </summary>
    Milvus,
    
    /// <summary>
    /// Custom implementation.
    /// </summary>
    Custom
}
