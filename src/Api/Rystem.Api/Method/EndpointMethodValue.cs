using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointMethodValue
    {
        public string[]? Policies { get; set; }
        public string Name { get; set; }
        public List<EndpointMethodParameterValue> Parameters { get; }
        public MethodInfo Method { get; }
        public string EndpointUri { get; set; } = string.Empty;
        public bool IsPost { get; private set; }
        public bool IsMultipart { get; private set; }
        public EndpointMethodValue(MethodInfo method, string? forcedName)
        {
            Method = method;
            Name = forcedName ?? method.Name;
            Parameters = [.. method.GetParameters().Select(x =>
            {
                return new EndpointMethodParameterValue(x);
            })];
            Update();
        }
        internal void Update()
        {
            var nonPrimitivesCount = Parameters.Count(x => x.Location == ApiParameterLocation.Body);
            IsPost = nonPrimitivesCount > 0;
            IsMultipart = nonPrimitivesCount > 1 || Parameters.Any(x => x.StreamType != StreamType.None);
        }
    }
}
