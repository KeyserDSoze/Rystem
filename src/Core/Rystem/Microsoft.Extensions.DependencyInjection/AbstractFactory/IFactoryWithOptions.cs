namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactoryWithOptions<TOptions> : IFactoryWithOptions
    {
        TOptions Options { get; set; }
    }
}
