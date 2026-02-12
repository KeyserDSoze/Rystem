namespace Rystem.PlayFramework;

/// <summary>
/// Interface for RAG (Retrieval-Augmented Generation) services.
/// Implementations should handle embedding generation and vector search.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Searches the knowledge base for relevant documents matching the query.
    /// </summary>
    /// <param name="request">Search request with query and settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with relevant documents.</returns>
    Task<RagResult> SearchAsync(RagRequest request, CancellationToken cancellationToken = default);
}
