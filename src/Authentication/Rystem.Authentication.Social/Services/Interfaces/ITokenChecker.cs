namespace Rystem.Authentication.Social
{
    public interface ITokenChecker
    {
        Task<string> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken);
    }
}
