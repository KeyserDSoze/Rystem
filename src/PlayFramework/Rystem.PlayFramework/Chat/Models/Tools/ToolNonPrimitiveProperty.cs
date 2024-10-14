using System.Text.Json.Serialization;

namespace Rystem.PlayFramework
{
    public sealed class ToolNonPrimitiveProperty : ToolProperty
    {
        private const string DefaultTypeName = "object";
        public ToolNonPrimitiveProperty()
        {
            Type = DefaultTypeName;
            Properties = new Dictionary<string, ToolProperty>();
        }
        [JsonPropertyName("properties")]
        public Dictionary<string, ToolProperty> Properties { get; }
        [JsonPropertyName("required")]
        public List<string>? Required { get; private set; }
        public ToolNonPrimitiveProperty AddRequired(params string[] names)
        {
            Required ??= new List<string>();
            Required.AddRange(names);
            return this;
        }
        public ToolNonPrimitiveProperty AddEnum(string key, ToolEnumProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddObject(string key, ToolNonPrimitiveProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddPrimitive(string key, ToolProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddNumber(string key, ToolNumberProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddArray(string key, ToolArrayProperty property)
            => AddProperty(key, property);
        internal ToolNonPrimitiveProperty AddProperty<T>(string key, T property)
            where T : ToolProperty
        {
            if (!Properties.ContainsKey(key))
                Properties.Add(key, property);
            return this;
        }
    }
}
