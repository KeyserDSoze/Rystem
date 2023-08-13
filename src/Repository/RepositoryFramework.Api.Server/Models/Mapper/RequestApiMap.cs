using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace RepositoryFramework
{
    internal sealed class RequestApiMap
    {
        [JsonPropertyName("httpMethod")]
        public string? HttpMethod => Sample.RequestBody == null ? HttpMethods.Get.ToString() : HttpMethods.Post.ToString();
        [JsonPropertyName("isAuthenticated")]
        public bool IsAuthenticated { get; set; }
        [JsonPropertyName("isAuthorized")]
        public bool IsAuthorized { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("policies")]
        public string[]? Policies { get; set; }
        [JsonPropertyName("repositoryMethod")]
        public string RepositoryMethod { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("streamUri")]
        public string? StreamUri { get; set; }
        [JsonPropertyName("hasStream")]
        public bool HasStream => StreamUri != null;
        [JsonPropertyName("sample")]
        public SampleRequestApiMap Sample { get; set; }
    }
}
