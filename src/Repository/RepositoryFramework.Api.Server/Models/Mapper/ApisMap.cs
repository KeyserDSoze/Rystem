using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    internal sealed class ApisMap
    {
        [JsonPropertyName("apis")]
        public List<ApiMap> Apis { get; set; } = new();
    }
}
