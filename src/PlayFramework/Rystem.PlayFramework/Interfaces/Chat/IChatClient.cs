namespace Rystem.PlayFramework
{
    public interface IChatClient : IChatClientBuilder, IChatClientToolBuilder
    {
        IAsyncEnumerable<ChatResponse> ExecuteStreamAsync(CancellationToken cancellationToken = default);
        Task<ChatResponse> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
