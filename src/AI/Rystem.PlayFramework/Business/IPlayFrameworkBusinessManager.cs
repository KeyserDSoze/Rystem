namespace Rystem.PlayFramework;

/// <summary>
/// Orchestrates the full business pipeline for a PlayFramework request:
/// before-execution guards → scene streaming → after-each-scene hooks → on-terminal hooks.
/// </summary>
public interface IPlayFrameworkBusinessManager
{
    /// <summary>
    /// Executes the full pipeline and streams results.
    /// </summary>
    /// <param name="factoryName">The factory name used to resolve hooks and the scene manager.</param>
    /// <param name="context">The execution context built from the incoming request.</param>
    /// <param name="cancellationToken">
    /// Cancellation token. The caller (endpoint handler) is responsible for linking a timeout
    /// <see cref="System.Threading.CancellationTokenSource"/> before invoking this method.
    /// </param>
    /// <returns>
    /// An async stream of tuples.
    /// When <c>Deny</c> is non-null the stream yields exactly one item and terminates: the
    /// endpoint handler must respond with the deny status code instead of opening SSE.
    /// When <c>Scene</c> is non-null the item should be written as an SSE event.
    /// </returns>
    IAsyncEnumerable<(AiSceneResponse? Scene, PlayFrameworkDenyResult? Deny)> ExecuteAsync(
        string factoryName,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
