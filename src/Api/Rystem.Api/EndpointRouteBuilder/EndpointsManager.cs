namespace Microsoft.AspNetCore.Builder
{
    public sealed class EndpointsManager
    {
        public List<EndpointValue> Endpoints { get; } = new();
    }
}
