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
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of responses.</returns>
    IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        SceneRequestSettings? settings = null,
        CancellationToken cancellationToken = default);
}
