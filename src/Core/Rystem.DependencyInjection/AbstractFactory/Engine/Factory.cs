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
            var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
            var factoryName = name.GetFactoryName<TService>();
            var decorationCount = map.DecorationCount[factoryName];
            return Create(name, decorationCount);
        }
        public TService? CreateWithoutDecoration(string? name = null)
        {
            return Create(name, 0);
        }
        private TService? Create(string? name, int decoration)
        {
            name ??= string.Empty;
            var decoratorName = decoration > 0 ? name.GetDecoratorName<TService>(decoration) : name;
            var factoryName = decoratorName.GetFactoryName<TService>();
            var service = _serviceProvider.GetKeyedService<TService>(factoryName);
            if (service is IServiceForFactory factoryService)
                factoryService.SetFactoryName(name);
            if (service is IDecoratorService<TService> decoratorService && decoration >= 0)
                decoratorService.SetDecoratedService(Create(name, decoration - 1)!);
            if (service is IServiceWithFactoryWithOptions serviceWithCustomOptions)
            {
                var optionsName = name.GetOptionsName<TService>();
                var options = _serviceProvider.GetKeyedService<IFactoryOptions>(optionsName.GetFactoryName<IFactoryOptions>());
                if (options != null)
                {
                    var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
                    if (map.OptionsSetter.TryGetValue(optionsName, out var optionsSetter))
                        optionsSetter.Invoke(serviceWithCustomOptions, options);
                }
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
