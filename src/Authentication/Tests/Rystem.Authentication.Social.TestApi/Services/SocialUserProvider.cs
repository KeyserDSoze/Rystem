using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social.TestApi.Services
{
    internal sealed class SocialUserProvider : ISocialUserProvider
    {
        public Task<ISocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            return Task.FromResult((new SuperSocialUser
            {
                Username = $"a {username}",
                Email = username
            } as ISocialUser)!);
        }

        public async IAsyncEnumerable<Claim> GetClaimsAsync(TokenResponse response, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new Claim(ClaimTypes.Name, response.Username!);
            yield return new Claim(ClaimTypes.Upn, "something");
        }
    }
    public sealed class SuperSocialUser : ISocialUser
    {
        [JsonPropertyName("e")]
        public required string Email { get; set; }
        [JsonPropertyName("u")]
        public string? Username { get; set; }
    }
}
