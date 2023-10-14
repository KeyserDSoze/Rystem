namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointsManager
    {
        public string BasePath { get; set; } = "api/";
        public List<EndpointValue> Endpoints { get; } = new();
    }
}
