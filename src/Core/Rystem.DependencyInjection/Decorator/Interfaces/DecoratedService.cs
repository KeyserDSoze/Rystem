namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DecoratedService<TService> : IDecoratedService<TService>
        where TService : class
    {
        public TService? Service { get; }
        public DecoratedService(TService? service)
        {
            Service = service;
        }
    }
}
