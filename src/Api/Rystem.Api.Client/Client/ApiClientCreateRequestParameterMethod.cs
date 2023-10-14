namespace Rystem.Api
{
    public sealed class ApiClientCreateRequestParameterMethod
    {
        public string Name { get; set; }
        public Action<ApiClientRequestBearer, object> Executor { get; set; }
        public Func<ApiClientRequestBearer, object, Task> ExecutorAsync { get; set; }
    }
}
