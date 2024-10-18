using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public sealed class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;
        [JsonPropertyName("parameters")]
        public ToolNonPrimitiveProperty Parameters { get; set; } = null!;
    }
}
