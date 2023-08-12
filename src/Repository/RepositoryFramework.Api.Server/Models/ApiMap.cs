using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    internal sealed class ApisMap
    {
        [JsonPropertyName("apis")]
        public List<ApiMap> Apis { get; set; } = new();
    }
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
    internal sealed class RequestApiMap
    {
        [JsonPropertyName("httpMethod")]
        public string? HttpMethod { get; set; }
        [JsonPropertyName("isAuthenticated")]
        public bool IsAuthenticated { get; set; }
        [JsonPropertyName("isAuthorized")]
        public bool IsAuthorized { get; set; }
        [JsonPropertyName("policies")]
        public string[]? Policies { get; set; }
        [JsonPropertyName("repositoryMethod")]
        public string RepositoryMethod { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("requestBody")]
        public object? RequestBody { get; set; }
        [JsonPropertyName("requestQuery")]
        public object? RequestQuery { get; set; }
        [JsonPropertyName("response")]
        public object? Response { get; set; }
        [JsonPropertyName("hasStream")]
        public bool HasStream { get; set; }
    }
}
