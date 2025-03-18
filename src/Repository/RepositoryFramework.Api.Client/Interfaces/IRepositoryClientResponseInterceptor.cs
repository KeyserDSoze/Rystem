namespace RepositoryFramework.Api.Client
{
    /// <summary>
    /// Interface for global interceptor response for your repository or CQRS clients.
    /// </summary>
    public interface IRepositoryResponseClientInterceptor
    {
        Task<HttpResponseMessage> CheckResponseAsync(HttpClient client, HttpResponseMessage response, Func<HttpClient, Task<HttpResponseMessage>> request);
    }
    /// <summary>
    /// Interface for global interceptor response for your repository or CQRS clients.
    /// </summary>
    public interface IRepositoryClientResponseInterceptor<T> : IRepositoryResponseClientInterceptor
    {
    }
    /// <summary>
    /// Interface for global interceptor response for your repository or CQRS clients.
    /// </summary>
    public interface IRepositoryResponseClientInterceptor<T, TKey> : IRepositoryClientResponseInterceptor<T>, IRepositoryResponseClientInterceptor
        where TKey : notnull
    {
    }
}
