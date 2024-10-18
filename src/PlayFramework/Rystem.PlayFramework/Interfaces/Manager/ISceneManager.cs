namespace Rystem.PlayFramework
{
    public interface ISceneManager
    {
        IAsyncEnumerable<AiSceneResponse> ExecuteAsync(string sceneName, CancellationToken cancellationToken);
    }
}
