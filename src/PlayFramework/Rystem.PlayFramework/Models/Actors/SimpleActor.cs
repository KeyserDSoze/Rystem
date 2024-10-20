namespace Rystem.PlayFramework
{
    internal sealed class SimpleActor : IActor
    {
        public required string Role { get; init; }
        public Task<string?> GetMessageAsync(SceneContext context, CancellationToken cancellationToken) => Task.FromResult(Role)!;
    }
}
