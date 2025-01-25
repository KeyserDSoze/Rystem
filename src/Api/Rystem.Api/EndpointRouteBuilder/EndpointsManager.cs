namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointsManager
    {
        public string BasePath { get; set; } = "api/";
        public bool RemoveAsyncSuffix { get; set; } = true;
        public List<EndpointValue> Endpoints { get; } = [];
    }
}
