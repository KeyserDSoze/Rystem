namespace RepositoryFramework
{
    internal sealed class ApisMap
    {
        public Dictionary<string, ApiMap> Apis { get; set; } = new();
    }
    internal sealed class ApiMap
    {
        public object? Key { get; set; }
        public object? Model { get; set; }
        public string? PatternType { get; set; }
        public List<RequestApiMap> Requests { get; set; } = new();
    }
    internal sealed class RequestApiMap
    {
        public string? HttpMethod { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsAuthorized { get; set; }
        public List<string> Policies { get; set; } = new();
        public string RepositoryMethod { get; set; }
        public bool KeyIsJsonable { get; set; }
        public string Uri { get; set; }
        public string ResponseWith { get; set; }
        public object? Response { get; set; }
        public bool HasStream { get; set; }
    }
}
