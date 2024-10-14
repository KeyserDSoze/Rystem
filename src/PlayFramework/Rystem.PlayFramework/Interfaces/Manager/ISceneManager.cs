namespace Rystem.OpenAi.Actors
{
    public interface ISceneManager
    {
        IAsyncEnumerable<AiSceneResponse> ExecuteAsync(string sceneName, CancellationToken cancellationToken);
    }
}
