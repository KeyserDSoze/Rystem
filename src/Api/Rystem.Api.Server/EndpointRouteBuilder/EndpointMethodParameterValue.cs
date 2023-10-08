using System.Reflection;
using Rystem.Api;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class EndpointMethodParameterValue
    {
        public ParameterInfo Info { get; set; }
        public bool IsPrimitive { get; }
        public Type Type { get; }
        public string Name { get; }
        public ApiParameterLocation Location { get; }
        public int Position { get; }
        public bool IsRequired { get; }
        public EndpointMethodParameterValue(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            IsPrimitive = parameterInfo.ParameterType.IsPrimitive();
            Type = parameterInfo.ParameterType;
            Name = (parameterInfo.GetCustomAttribute(typeof(ApiParameterNameAttribute)) as ApiParameterNameAttribute)?.Name ?? parameterInfo.Name!;
            IsRequired = parameterInfo.GetCustomAttribute(typeof(ApiParameterNotRequiredAttribute)) == null;
            var location = (parameterInfo.GetCustomAttribute(typeof(ApiParameterLocationAttribute)) as ApiParameterLocationAttribute);
            if (location != null)
            {
                Location = location.Location;
                Position = location.Position ?? 0;
            }
            else
            {
                Location = IsPrimitive ? ApiParameterLocation.Query : ApiParameterLocation.Body;
            }
        }
    }
}
