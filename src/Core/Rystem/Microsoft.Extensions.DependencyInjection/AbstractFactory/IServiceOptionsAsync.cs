namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceOptionsAsync<T> : IServiceOptions
        where T : class
    {
        Task<Func<T>> BuildAsync();
    }
}
