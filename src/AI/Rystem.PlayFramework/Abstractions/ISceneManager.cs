namespace Rystem.PlayFramework;

/// <summary>
/// Main interface for executing PlayFramework scenes.
/// </summary>
public interface ISceneManager
{
    /// <summary>
    /// Executes a scene request and streams responses.
    /// </summary>
    /// <param name="message">User input message.</param>
    /// <param name="metadata">Request metadata for rate limiting, telemetry, and custom logic (e.g., userId, tenantId, sessionId).</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of responses.</returns>
    IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        Dictionary<string, object>? metadata = null,
        SceneRequestSettings? settings = null,
        CancellationToken cancellationToken = default);
}
