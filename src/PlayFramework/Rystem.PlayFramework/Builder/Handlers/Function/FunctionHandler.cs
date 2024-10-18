namespace Rystem.PlayFramework
{
    internal sealed class FunctionHandler
    {
        public List<string> Scenes { get; } = [];
        public HttpHandler? HttpRequest { get; set; }
        public ServiceHandler? Service { get; set; }
        public bool HasHttpRequest => HttpRequest != null;
        public bool HasService => Service != null;
        public Action<IChatClientToolBuilder>? Chooser { get; set; }
    }
}
