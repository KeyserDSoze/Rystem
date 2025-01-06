namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AnyOfJsonRegexClassSelectorAttribute : AnyOfJsonSelectorAttribute
    {
        public AnyOfJsonRegexClassSelectorAttribute(string propertyName, params string[] values) : base(values)
        {
            IsRegex = true;
            PropertyName = propertyName;
        }
    }
}
