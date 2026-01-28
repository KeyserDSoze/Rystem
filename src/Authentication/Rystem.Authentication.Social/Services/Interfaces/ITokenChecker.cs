namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        /// <summary>
        /// Check token and get username with settings (supports PKCE and additional parameters)
        /// </summary>
        /// <param name="code">Authorization code from OAuth provider</param>
        /// <param name="settings">Token checker settings (domain, redirectPath, additional parameters like code_verifier)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Token response with username and claims, or error message</returns>
        Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, TokenCheckerSettings settings, CancellationToken cancellationToken = default);
    }
}
