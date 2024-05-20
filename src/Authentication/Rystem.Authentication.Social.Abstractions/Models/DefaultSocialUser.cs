using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class DefaultSocialUser : ISocialUser
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
