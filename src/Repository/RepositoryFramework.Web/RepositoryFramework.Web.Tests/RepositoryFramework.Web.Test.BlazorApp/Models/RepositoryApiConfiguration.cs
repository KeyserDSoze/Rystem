using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public class RepositoryApiConfiguration
    {
        [JsonPropertyName("delete")]
        public required ApiConfiguration Delete { get; set; }
        [JsonPropertyName("exists")]
        public required ApiConfiguration Exists { get; set; }
        [JsonPropertyName("get")]
        public required ApiConfiguration Get { get; set; }
        [JsonPropertyName("insert")]
        public required ApiConfiguration Insert { get; set; }
        [JsonPropertyName("query")]
        public required ApiConfiguration Query { get; set; }
        [JsonPropertyName("update")]
        public required ApiConfiguration Update { get; set; }
    }

}
