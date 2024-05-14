using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social.Blazor
{
    public sealed class Token
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
        [JsonPropertyName("isExpired")]
        public bool IsExpired { get; set; }
        [JsonPropertyName("expiresIn")]
        public long ExpiresIn { get; set; }
        [JsonPropertyName("exping")]
        public DateTime Expiring { get; set; }
    }
}
