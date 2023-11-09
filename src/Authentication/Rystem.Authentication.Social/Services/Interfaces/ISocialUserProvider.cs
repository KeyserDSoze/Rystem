using System.Security.Claims;

namespace Rystem.Authentication.Social
{
    public interface ISocialUserProvider
    {
        Task<SocialUser> GetAsync(string? username, IEnumerable<Claim> claims, CancellationToken cancellationToken);
    }
}
