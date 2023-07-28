namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceWithOptions<TOptions> : IServiceWithOptions
    {
        void SetOptions(TOptions options);
    }
}
