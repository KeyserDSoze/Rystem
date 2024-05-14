using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social.Blazor
{
    public sealed class SocialUser
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
