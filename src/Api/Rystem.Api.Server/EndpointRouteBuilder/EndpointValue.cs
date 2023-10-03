namespace Microsoft.AspNetCore.Builder
{
    internal sealed class EndpointValue
    {
        public EndpointValue(Type type) { Type = type; }
        public Type Type { get; }
        public Dictionary<string, EndpointMethodValue> Methods { get; } = new();
        public string? FactoryName { get; set; }
        public string? EndpointName { get; set; }
    }
}
