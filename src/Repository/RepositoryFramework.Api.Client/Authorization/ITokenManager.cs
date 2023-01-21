namespace RepositoryFramework.Api.Client.Authorization
{
    public interface ITokenManager
    {
        Task<string?> GetTokenAsync();
        Task EnrichWithAuthorizationAsync(HttpClient client);
    }
}
