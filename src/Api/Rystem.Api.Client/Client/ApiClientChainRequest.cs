namespace Rystem.Api
{
    public sealed class ApiClientChainRequest<T>
    {
        public Dictionary<string, ApiClientCreateRequestMethod> Methods { get; set; } = new();
    }
}
