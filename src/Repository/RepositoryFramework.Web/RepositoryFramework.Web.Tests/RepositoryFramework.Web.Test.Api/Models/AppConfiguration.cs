using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public sealed class AppConfiguration
    {
        [JsonPropertyName("appDomain")]
        public required string AppDomain { get; set; }
        [JsonPropertyName("apiDomain")]
        public required string ApiDomain { get; set; }
        [JsonPropertyName("color")]
        public required RepositoryApiConfiguration Color { get; set; }
        [JsonPropertyName("businessRule")]
        public required RepositoryApiConfiguration BusinessRule { get; set; }
        [JsonPropertyName("formDesign")]
        public required RepositoryApiConfiguration FormDesign { get; set; }
        [JsonPropertyName("language")]
        public required RepositoryApiConfiguration Language { get; set; }
        [JsonPropertyName("pageDesign")]
        public required RepositoryApiConfiguration PageDesign { get; set; }
        [JsonPropertyName("userManager")]
        public required UserApiConfiguration UserManager { get; set; }
        [JsonPropertyName("authentication")]
        public required AuthenticationConfiguration Authentication { get; set; }
        [JsonPropertyName("whistle")]
        public required WhistleConfiguration Whistle { get; set; }
    }
}
