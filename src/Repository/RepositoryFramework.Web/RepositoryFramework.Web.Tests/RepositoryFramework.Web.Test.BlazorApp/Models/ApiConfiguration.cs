using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public class ApiConfiguration
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = null!;
        [JsonPropertyName("mocked")]
        public bool Mocked { get; set; }
        [JsonPropertyName("method")]
        public string Method { get; set; } = null!;
    }

}
