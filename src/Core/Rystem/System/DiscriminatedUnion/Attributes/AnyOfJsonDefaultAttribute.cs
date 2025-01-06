namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AnyOfJsonDefaultAttribute : Attribute
    {
        public AnyOfJsonDefaultAttribute()
        {
        }
    }
}
