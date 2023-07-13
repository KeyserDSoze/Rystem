namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<T> : IFactory<T>
    {
        private readonly IEnumerable<T> _services;
        private readonly IServiceProvider _serviceProvider;
        public Factory(IEnumerable<T> services, IServiceProvider serviceProvider)
        {
            _services = services;
            _serviceProvider = serviceProvider;
        }
        public T Create(string? name = null)
        {
            var index = Map[name ?? string.Empty];
            var service = _services.Skip(index).First();
            if (service is IFactoryWithOptions)
            {
                var options = MapOptions[name ?? string.Empty];
                var optionsService = _serviceProvider.GetServices(options.Type).Skip(options.Index).First()!;
                options.Setter(service, optionsService);
            }
            return service;
        }
        internal static Dictionary<string, int> Map { get; } = new();
        internal static Dictionary<string, MappingOptions> MapOptions { get; } = new();
        internal sealed class MappingOptions
        {
            public int Index { get; init; }
            public Type Type { get; init; }
            public Action<T, object> Setter { get; init; }
        }
    }
}
