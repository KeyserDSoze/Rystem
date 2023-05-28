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
        public RequestApiMap Request { get; set; } = new();
        public object Model { get; set; }
    }
    internal sealed class RequestApiMap
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAuthorized { get; set; }
        public List<string> Policies { get; set; }
        public object? Key { get; set; }
        public string Method { get; set; }
        public bool KeyIsJsonable { get; set; }
        public string Uri { get; set; }
    }
    internal sealed class RequestQuerystringApiMap
    {

    }
}
