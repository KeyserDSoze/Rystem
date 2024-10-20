namespace Rystem.PlayFramework
{
    internal sealed class AsyncActionActor : IActor
    {
        public required Func<SceneContext, CancellationToken, Task<string>> Action { get; init; }
        public Task<string?> GetMessageAsync(SceneContext sceneContext, CancellationToken cancellationToken) => Action(sceneContext, cancellationToken)!;
    }
}
