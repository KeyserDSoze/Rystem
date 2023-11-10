using System.Security.Claims;

namespace Rystem.Authentication.Social.TestApi.Services
{
    internal sealed class SocialUserProvider : ISocialUserProvider
    {
        public Task<SocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SuperSocialUser
            {
                Username = $"a {username}",
                Email = username
            } as SocialUser);
        }

        public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new Claim(ClaimTypes.Name, username!);
            yield return new Claim(ClaimTypes.Upn, "something");
        }
    }
    public sealed class SuperSocialUser : SocialUser
    {
        public string Email { get; set; }
    }
}
