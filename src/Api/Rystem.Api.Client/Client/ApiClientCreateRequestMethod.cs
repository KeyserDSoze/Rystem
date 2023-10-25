using Microsoft.AspNetCore.Builder;

namespace Rystem.Api
{
    public sealed class ApiClientCreateRequestMethod
    {
        public bool IsPost { get; set; }
        public string FixedPath { get; set; }
        public StreamType ResultStreamType { get; set; }
        public Type ReturnType { get; set; }
        public List<ApiClientCreateRequestParameterMethod> Parameters { get; set; } = new();
    }
}
