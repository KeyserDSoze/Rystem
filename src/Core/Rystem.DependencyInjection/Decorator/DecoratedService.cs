namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DecoratedService<TService> : IDecoratedService<TService>, IServiceForFactory
        where TService : class
    {
        public TService Service { get; }
        public DecoratedService(TService service)
        {
            Service = service;
        }
        public string FactorySourceName { get; private set; } = null!;
        public void SetFactoryName(string name)
        {
            FactorySourceName = name;
        }
    }
}
