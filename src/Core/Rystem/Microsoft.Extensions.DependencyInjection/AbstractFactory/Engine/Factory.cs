namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<TService> : IFactory<TService>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FactoryServices<TService> _map;
        public Factory(IServiceProvider serviceProvider, FactoryServices<TService> map)
        {
            _serviceProvider = serviceProvider;
            _map = map;
        }
        public TService Create(string? name = null)
            => _map.Services[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, true);
        public TService CreateWithoutDecoration(string? name = null)
            => _map.Services[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, false);
        public bool Exists(string? name = null)
            => _map.Services.ContainsKey(name ?? string.Empty);
    }
}
