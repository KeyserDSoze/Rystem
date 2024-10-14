using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public sealed class ToolArrayProperty : ToolProperty
    {
        [JsonPropertyName("items")]
        public ToolProperty? Items { get; set; }
    }
}
