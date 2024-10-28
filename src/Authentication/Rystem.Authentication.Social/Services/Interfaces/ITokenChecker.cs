namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? redirectDomain = null, CancellationToken cancellationToken = default);
    }
}
