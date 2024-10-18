using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    [JsonDerivedType(typeof(ToolEnumProperty))]
    [JsonDerivedType(typeof(ToolNumberProperty))]
    [JsonDerivedType(typeof(ToolNonPrimitiveProperty))]
    [JsonDerivedType(typeof(ToolArrayProperty))]
    public class ToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        private const string DefaultTypeName = "string";
        public ToolProperty()
        {
            Type = DefaultTypeName;
        }
    }
}
