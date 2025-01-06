namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AnyOfJsonClassSelectorAttribute : AnyOfJsonSelectorAttribute
    {
        public AnyOfJsonClassSelectorAttribute(string propertyName, params object[] values) : base(values)
        {
            PropertyName = propertyName;
        }
    }
}
