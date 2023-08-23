namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceForFactoryWithOptions<in TOptions> : IServiceForFactoryWithOptions
    {
        void SetOptions(TOptions options);
    }
}
