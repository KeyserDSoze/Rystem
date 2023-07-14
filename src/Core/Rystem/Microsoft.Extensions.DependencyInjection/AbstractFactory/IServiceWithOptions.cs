namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithOptions<TOptions> : IServiceWithOptions
    {
        TOptions? Options { get; set; }
    }
}
