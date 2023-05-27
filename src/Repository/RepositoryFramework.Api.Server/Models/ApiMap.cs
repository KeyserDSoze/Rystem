namespace RepositoryFramework
{
    internal sealed class ApisMap
    {
        public Dictionary<string, List<ApiMap>> Apis { get; set; } = new();
    }
    internal sealed class ApiMap
    {
        public PatternType PatternType { get; set; }
        public RepositoryMethods Method { get; set; }
        public ApiRequestMap Request { get; set; } = new();
        public object SampleKey { get; set; }
        public object SampleResponse { get; set; }
    }
    internal sealed class ApiRequestMap
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAuthorized { get; set; }
    }
}
