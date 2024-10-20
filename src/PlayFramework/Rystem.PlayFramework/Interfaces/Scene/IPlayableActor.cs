namespace Rystem.PlayFramework
{
    public interface IPlayableActor
    {
        Task<string?> GetMessageAsync(SceneContext context, CancellationToken cancellationToken);
    }
}
