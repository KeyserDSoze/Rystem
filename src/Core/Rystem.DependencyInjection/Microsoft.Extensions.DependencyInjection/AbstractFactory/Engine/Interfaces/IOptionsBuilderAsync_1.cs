namespace Microsoft.Extensions.DependencyInjection
{
    public interface IOptionsBuilderAsync<TService> : IOptionsBuilder
        where TService : class
    {
        Task<Func<IServiceProvider, TService>> BuildAsync();
    }
}
