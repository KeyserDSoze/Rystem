namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceOptions<T> : IServiceOptions
        where T : class
    {
        Task<Func<T>> BuildAsync();
    }
}
