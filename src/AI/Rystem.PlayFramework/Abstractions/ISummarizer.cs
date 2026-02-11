namespace Rystem.PlayFramework;

/// <summary>
/// Interface for conversation summarization.
/// </summary>
public interface ISummarizer
{
    /// <summary>
    /// Determines if summarization should occur based on conversation length.
    /// </summary>
    /// <param name="responses">List of responses to check.</param>
    /// <returns>True if summarization should occur.</returns>
    bool ShouldSummarize(List<AiSceneResponse> responses);

    /// <summary>
    /// Summarizes conversation history.
    /// </summary>
    /// <param name="responses">Responses to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary text.</returns>
    Task<string> SummarizeAsync(
        List<AiSceneResponse> responses,
        CancellationToken cancellationToken = default);
}
