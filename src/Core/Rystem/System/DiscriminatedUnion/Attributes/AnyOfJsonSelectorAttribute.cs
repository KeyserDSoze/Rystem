namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AnyOfJsonSelectorAttribute : Attribute
    {
        public AnyOfJsonSelectorAttribute(params object[] values)
        {
            Values = values;
        }
        internal string? PropertyName { get; set; }
        internal bool IsRegex { get; set; }
        public object[] Values { get; }
    }
}
