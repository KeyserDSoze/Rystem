namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<TService> : IFactory<TService>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, FactoryService<TService>> _map;
        public Factory(IServiceProvider serviceProvider, Dictionary<string, FactoryService<TService>> map)
        {
            _serviceProvider = serviceProvider;
            _map = map;
        }
        public TService Create(string? name = null)
            => _map[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, false);
        public TService CreateWithoutDecoration(string? name = null)
            => _map[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, true);
        public bool Exists(string? name = null)
            => _map.ContainsKey(name ?? string.Empty);
    }
}
