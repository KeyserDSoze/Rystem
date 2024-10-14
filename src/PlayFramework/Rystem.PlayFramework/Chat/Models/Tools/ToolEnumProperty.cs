using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public sealed class ToolEnumProperty : ToolProperty
    {
        private const string DefaultTypeName = "string";
        public ToolEnumProperty()
        {
            Type = DefaultTypeName;
        }
        [JsonPropertyName("enum")]
        public List<string>? Enums { get; set; }
    }
}
