namespace Rystem.PlayFramework
{
    internal sealed class ActionActor : IActor
    {
        public required Func<SceneContext, string> Action { get; init; }
        public Task<string?> GetMessageAsync(SceneContext context, CancellationToken cancellationToken) => Task.FromResult(Action(context))!;
    }
}
