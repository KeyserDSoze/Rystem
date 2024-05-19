using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    public class SocialUser
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        public static SocialUser Empty { get; } = new();
    }
}
