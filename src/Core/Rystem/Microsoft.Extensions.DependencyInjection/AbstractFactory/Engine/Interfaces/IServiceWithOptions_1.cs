namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithOptions<in TOptions> : IServiceWithOptions
    {
        void SetOptions(TOptions options);
    }
}
