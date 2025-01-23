using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointValue
    {
        public EndpointValue(Type type)
        {
            Type = type;
            EndpointName = type.IsInterface ? type.Name[1..] : type.Name;
        }
        public Type Type { get; }
        public Dictionary<string, EndpointMethodValue> Methods { get; } = [];
        public AnyOf<string, Enum>? FactoryName { get; set; }
        public bool IsFactory { get; set; }
        public string? EndpointName { get; set; }
        public string? BasePath { get; set; }
    }
}
