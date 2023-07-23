namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class DecoratedService<TService, TImplementation> : IDecoratedService<TService>
        where TService : class
        where TImplementation : class, TService
    {
        public TService Service { get; }
        public DecoratedService(TImplementation service)
        {
            Service = service;
        }
    }
}
