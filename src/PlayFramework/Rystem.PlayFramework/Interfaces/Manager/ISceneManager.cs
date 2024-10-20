namespace Rystem.PlayFramework
{
    public interface ISceneManager
    {
        IAsyncEnumerable<AiSceneResponse> ExecuteAsync(string sceneName, Action<SceneRequestSettings>? settings = null, CancellationToken cancellationToken = default);
    }
}
