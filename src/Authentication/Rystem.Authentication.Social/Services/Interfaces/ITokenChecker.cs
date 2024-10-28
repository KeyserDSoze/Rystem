namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default);
    }
}
