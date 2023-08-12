using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

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
        public string? HttpMethod => Sample.RequestBody == null ? HttpMethods.Get.ToString() : HttpMethods.Post.ToString();
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
        [JsonPropertyName("hasStream")]
        public bool HasStream { get; set; }
        public SampleRequestApiMap Sample { get; set; }
    }
    public class SampleRequestApiMap
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
