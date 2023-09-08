namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithFactoryWithOptions<in TOptions> : IServiceWithFactoryWithOptions
        where TOptions : class, IFactoryOptions
    {
        void SetOptions(TOptions options);
    }
}
