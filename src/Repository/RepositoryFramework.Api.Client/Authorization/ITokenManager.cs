namespace RepositoryFramework.Api.Client.Authorization
{
    public interface ITokenManager
    {
        Task<string?> GetTokenAsync();
        Task<string?> RefreshTokenAsync();
        Task EnrichWithAuthorizationAsync(HttpClient client);
    }
}
