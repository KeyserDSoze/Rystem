namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AnyOfJsonRegexSelectorAttribute : AnyOfJsonSelectorAttribute
    {
        public AnyOfJsonRegexSelectorAttribute(params string[] values) : base(values)
        {
            IsRegex = true;
        }
    }
}
