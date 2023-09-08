namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<TService> : IFactory<TService>
        where TService : class
    {
        private readonly IServiceProvider _serviceProvider;
        public Factory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public TService? Create(string? name = null)
        {
            var factoryName = name.GetFactoryName<TService>();
            return Create(name, factoryName);
        }
        public TService? CreateWithoutDecoration(string? name = null)
        {
            var decoratedName = name.GetDecoratedName<TService>().GetFactoryName<TService>();
            return Create(name, decoratedName);
        }
        private TService? Create(string? name, string factoryName)
        {
            name ??= string.Empty;
            var service = _serviceProvider.GetKeyedService<TService>(factoryName);
            if (service is IServiceForFactory factoryService)
                factoryService.SetFactoryName(name);
            if (service is IDecoratorService<TService> decoratorService)
                decoratorService.SetDecoratedService(CreateWithoutDecoration(name)!);
            if (service is IServiceWithFactoryWithOptions serviceWithCustomOptions)
            {
                var optionsName = name.GetOptionsName<TService>().GetFactoryName<IFactoryOptions>();
                var options = _serviceProvider.GetKeyedService<IFactoryOptions>(optionsName);
                var dynamicServiceWithCustomOptions = (dynamic)serviceWithCustomOptions;
                dynamicServiceWithCustomOptions
                    .SetOptions(options);
            }
            return service;
        }

        public bool Exists(string? name = null)
        {
            var factoryName = name.GetFactoryName<TService>();
            var service = _serviceProvider.GetKeyedService<TService>(factoryName);
            return service != null;
        }
    }
}
