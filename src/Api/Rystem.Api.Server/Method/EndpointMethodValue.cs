using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class EndpointMethodValue
    {
        public string[]? Policies { get; set; }
        public string? Name { get; set; }
        public List<EndpointMethodParameterValue> Parameters { get; }
        public MethodInfo Method { get; }
        public string EndpointUri { get; internal set; } = string.Empty;
        public EndpointMethodValue(MethodInfo method)
        {
            Method = method;
            Parameters = method.GetParameters().Select(x =>
            {
                return new EndpointMethodParameterValue(x);
            }).ToList();
        }
    }
}
