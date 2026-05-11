namespace Rystem.PlayFramework;

/// <summary>
/// Hook that runs for every <see cref="AiSceneResponse"/> produced by the stream,
/// before the item is written to the SSE channel.
/// </summary>
public interface IPlayFrameworkAfterEachScene
{
    /// <summary>
    /// Called for each scene response.
    /// </summary>
    /// <param name="scene">The scene response from the scene manager.</param>
    /// <param name="context">Shared execution context for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="PlayFrameworkSceneResult.Forward(AiSceneResponse)"/> to forward (optionally modified),
    /// <see cref="PlayFrameworkSceneResult.Suppress()"/> to discard the item,
    /// or <see cref="PlayFrameworkSceneResult.ForwardAndInject(AiSceneResponse, AiSceneResponse[])"/> to forward and append extra items.
    /// </returns>
    Task<PlayFrameworkSceneResult> AfterSceneAsync(
        AiSceneResponse scene,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
