namespace Rystem.PlayFramework
{
    public interface IDirector
    {
        Task<DirectorResponse> DirectAsync(SceneContext context, CancellationToken cancellationToken);
    }
}
