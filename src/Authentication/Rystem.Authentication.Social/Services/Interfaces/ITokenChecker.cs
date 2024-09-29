namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken);
    }
}
