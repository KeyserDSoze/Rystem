namespace Rystem.PlayFramework
{
    public interface ISceneManager
    {
        IAsyncEnumerable<AiSceneResponse> ExecuteAsync(string sceneName, Dictionary<object, object>? properties = null, CancellationToken cancellationToken = default);
    }
}
