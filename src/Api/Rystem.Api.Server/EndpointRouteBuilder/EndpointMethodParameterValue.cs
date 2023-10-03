using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class EndpointMethodParameterValue
    {
        public ParameterInfo Info { get; set; }
        public bool IsPrimitive { get; }
        public Type Type { get; }
        public string Name { get; }
        public EndpointMethodParameterValue(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            IsPrimitive = parameterInfo.ParameterType.IsPrimitive();
            Type = parameterInfo.ParameterType;
            Name = parameterInfo.Name!;
        }
    }
}
