using System.Reflection;
using Rystem.Api;

namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointMethodParameterValue
    {
        public ParameterInfo Info { get; set; }
        public bool IsPrimitive { get; }
        public Type Type { get; }
        public string Name { get; set; }
        public ApiParameterLocation Location { get; set; }
        public int Position { get; set; }
        public bool IsRequired { get; set; }
        public StreamType StreamType { get; set; }
        public string? ContentType { get; set; }
        public object? Example { get; set; }
        private const string SpecialStreamFromAspNetCore = "Microsoft.AspNetCore.Http.IFormFile";
        private const string SpecialStreamFromRystem = "Rystem.Api.IHttpFile";
        public static StreamType IsThisTypeASpecialStream(Type type)
        {
            if (type.IsAssignableFrom(typeof(Stream)))
                return StreamType.Default;
            else if (type.FullName?.StartsWith(SpecialStreamFromAspNetCore, StringComparison.OrdinalIgnoreCase) == true)
                return StreamType.AspNet;
            else if (type.FullName?.StartsWith(SpecialStreamFromRystem, StringComparison.OrdinalIgnoreCase) == true)
                return StreamType.Rystem;
            else
                return StreamType.None;
        }
        public EndpointMethodParameterValue(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            IsPrimitive = parameterInfo.ParameterType.IsPrimitive();
            StreamType = IsThisTypeASpecialStream(parameterInfo.ParameterType);
            Type = parameterInfo.ParameterType;
            Name = parameterInfo.Name!;
            Location = IsPrimitive ? ApiParameterLocation.Query : ApiParameterLocation.Body;
            if (parameterInfo.GetCustomAttribute(typeof(QueryAttribute)) is QueryAttribute fromQuery)
            {
                Location = ApiParameterLocation.Query;
                if (fromQuery.Name != null)
                    Name = fromQuery.Name;
                IsRequired = fromQuery.IsRequired;
            }
            else if (parameterInfo.GetCustomAttribute(typeof(HeaderAttribute)) is HeaderAttribute fromHeader)
            {
                Location = ApiParameterLocation.Header;
                if (fromHeader.Name != null)
                    Name = fromHeader.Name;
                IsRequired = fromHeader.IsRequired;
            }
            else if (parameterInfo.GetCustomAttribute(typeof(CookieAttribute)) is CookieAttribute fromCookie)
            {
                Location = ApiParameterLocation.Cookie;
                if (fromCookie.Name != null)
                    Name = fromCookie.Name;
                IsRequired = fromCookie.IsRequired;
            }
            else if (parameterInfo.GetCustomAttribute(typeof(PathAttribute)) is PathAttribute fromPath)
            {
                Location = ApiParameterLocation.Path;
                if (fromPath.Index > 0)
                {
                    Position = fromPath.Index;
                }
                IsRequired = fromPath.IsRequired;
            }
            else if (parameterInfo.GetCustomAttribute(typeof(FormAttribute)) is FormAttribute fromForm)
            {
                Location = ApiParameterLocation.Body;
                if (fromForm.Name != null)
                {
                    Name = fromForm.Name;
                }
                IsRequired = fromForm.IsRequired;
            }
            else if (parameterInfo.GetCustomAttribute(typeof(BodyAttribute)) is BodyAttribute fromBody)
            {
                Location = ApiParameterLocation.Body;
                IsRequired = fromBody.IsRequired;
            }
            if (parameterInfo.ParameterType.IsGenericType && parameterInfo.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                IsRequired = false;
        }
    }
}
