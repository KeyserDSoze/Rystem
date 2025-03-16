using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social.TestApi.Models
{
    public sealed class SocialUser : ISocialUser, ILocalizedSocialUser
    {
        [JsonPropertyName("u")]
        public string? Username { get; set; }
        [JsonPropertyName("l")]
        public string? Language { get; set; }
    }
}
