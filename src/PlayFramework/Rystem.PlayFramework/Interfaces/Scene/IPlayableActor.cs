namespace Rystem.PlayFramework
{
    public interface IPlayableActor
    {
        Task<string?> GetMessageAsync(SceneContext sceneContext, CancellationToken cancellationToken);
    }
}
