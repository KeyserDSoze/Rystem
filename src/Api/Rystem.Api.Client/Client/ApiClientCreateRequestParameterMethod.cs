namespace Rystem.Api
{
    public sealed class ApiClientCreateRequestParameterMethod
    {
        public string? Name { get; set; }
        public Action<ApiClientRequestBearer, object?>? Executor { get; set; }
    }
}
