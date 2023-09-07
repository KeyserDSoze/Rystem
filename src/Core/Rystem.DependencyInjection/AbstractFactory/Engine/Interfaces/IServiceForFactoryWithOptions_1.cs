namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithFactoryWithOptions<in TOptions> : IServiceForFactoryWithOptions
    {
        void SetOptions(TOptions options);
    }
}
