using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    internal sealed class SampleRequestApiMap
    {
        [JsonPropertyName("baseUri")]
        public string BaseUri { get; set; }
        [JsonPropertyName("sampleUri")]
        public string Url => $"{BaseUri}{(RequestQuery != null ? $"?{string.Join('&', RequestQuery.Select(x => $"{x.Key}={x.Value}"))}" : string.Empty)}";
        [JsonPropertyName("requestQuery")]
        public Dictionary<string, string>? RequestQuery { get; set; }
        [JsonPropertyName("requestBody")]
        public object? RequestBody { get; set; }
        [JsonPropertyName("response")]
        public object? Response { get; set; }
    }
}
