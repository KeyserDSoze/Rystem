using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public class AuthenticationConfiguration
    {
        [JsonPropertyName("token")]
        public required ApiConfiguration Token { get; set; }
        [JsonPropertyName("refresh")]
        public required ApiConfiguration Refresh { get; set; }
        [JsonPropertyName("visibility")]
        public required ApiConfiguration Visibility { get; set; }
    }
}
