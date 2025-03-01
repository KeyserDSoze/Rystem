namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default);
    }
}
