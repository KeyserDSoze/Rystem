namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<string> CheckTokenAndGetUsernameAsync(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder, string code, CancellationToken cancellationToken);
    }
}
