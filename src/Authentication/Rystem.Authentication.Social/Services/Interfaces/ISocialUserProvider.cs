using System.Security.Claims;

namespace Rystem.Authentication.Social
{
    public interface ISocialUserProvider
    {
        Task<ISocialUser> GetAsync(string? username, IEnumerable<Claim> claims, CancellationToken cancellationToken);
        IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken);
    }
}
