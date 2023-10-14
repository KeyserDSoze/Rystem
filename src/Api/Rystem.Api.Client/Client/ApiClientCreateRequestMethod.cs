namespace Rystem.Api
{
    public sealed class ApiClientCreateRequestMethod
    {
        public bool IsPost { get; set; }
        public string FixedPath { get; set; }
        public List<ApiClientCreateRequestParameterMethod> Parameters { get; set; } = new();
    }
}
