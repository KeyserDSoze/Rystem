namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonAnyOfChooserAttribute : Attribute
    {
        public JsonAnyOfChooserAttribute(params object[] values)
        {
            Values = values;
        }
        public object[] Values { get; }
    }
}
