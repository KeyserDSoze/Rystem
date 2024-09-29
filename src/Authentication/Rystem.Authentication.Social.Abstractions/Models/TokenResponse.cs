using System.Security.Claims;

namespace Rystem.Authentication.Social
{
    public sealed class TokenResponse
    {
        public required string Username { get; set; }
        public required List<Claim> Claims { get; set; }
        public static TokenResponse? Empty => null;
    }
}
