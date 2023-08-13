using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    internal sealed class ApiMap
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        [JsonPropertyName("factoryName")]
        public string? FactoryName { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("key")]
        public object? Key { get; set; }
        [JsonPropertyName("model")]
        public object? Model { get; set; }
        [JsonPropertyName("patternType")]
        public string? PatternType { get; set; }
        [JsonPropertyName("requests")]
        public List<RequestApiMap> Requests { get; set; } = new();
        [JsonPropertyName("keyIsJsonable")]
        public bool KeyIsJsonable { get; set; }
    }
}
