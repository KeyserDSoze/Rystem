namespace Rystem.Api
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ApiParameterNameAttribute : Attribute
    {
        public string Name { get; }
        public ApiParameterNameAttribute(string name) => Name = name;
    }
}
