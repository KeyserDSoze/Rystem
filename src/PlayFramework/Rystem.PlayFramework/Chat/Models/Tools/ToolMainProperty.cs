using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public sealed class ToolMainProperty : ToolNonPrimitiveProperty
    {
        public ToolMainProperty() : base() { }
        [JsonPropertyName("required")]
        public List<string>? Required { get; private set; }
        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
        public ToolNonPrimitiveProperty AddRequired(params string[] names)
        {
            Required ??= new List<string>();
            Required.AddRange(names);
            return this;
        }
    }
}
