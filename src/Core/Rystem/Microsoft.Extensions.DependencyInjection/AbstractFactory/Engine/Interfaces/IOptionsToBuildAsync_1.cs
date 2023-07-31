namespace Microsoft.Extensions.DependencyInjection
{
    public interface IOptionsToBuildAsync<TService> : IOptionsToBuild
        where TService : class
    {
        Task<Func<IServiceProvider, TService>> BuildAsync();
    }
}
