namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointValue
    {
        public EndpointValue(Type type)
        {
            Type = type;
            EndpointName = type.Name;
        }
        public Type Type { get; }
        public Dictionary<string, EndpointMethodValue> Methods { get; } = new();
        public string? FactoryName { get; set; }
        public string? EndpointName { get; set; }
        public string? BasePath { get; set; }
    }
}
