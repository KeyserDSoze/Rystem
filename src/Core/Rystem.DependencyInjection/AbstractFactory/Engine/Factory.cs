namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<TService> : IFactory<TService>
        where TService : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFactoryFallback<TService>? _fallback;
        public Factory(IServiceProvider serviceProvider, IFactoryFallback<TService>? fallback = null)
        {
            _serviceProvider = serviceProvider;
            _fallback = fallback;
        }
        public TService? Create(AnyOf<string?, Enum>? name = null)
        {
            var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
            var nameAsString = name.AsString();
            var factoryName = nameAsString.GetFactoryName<TService>();
            var decorationCount = map.DecorationCount.TryGetValue(factoryName, out var value) ? value : 0;
            return Create(name, decorationCount, false).FirstOrDefault();
        }
        public TService? CreateWithoutDecoration(AnyOf<string?, Enum>? name = null)
            => Create(name, 0, false).FirstOrDefault();
        public IEnumerable<TService> CreateAll(AnyOf<string?, Enum>? name = null)
        {
            var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
            var nameAsString = name.AsString();
            var factoryName = nameAsString.GetFactoryName<TService>();
            var decorationCount = map.DecorationCount.TryGetValue(factoryName, out var value) ? value : 0;
            return Create(name, decorationCount, true);
        }
        public IEnumerable<TService> CreateAllWithoutDecoration(AnyOf<string?, Enum>? name = null)
            => Create(name, 0, true);
        private IEnumerable<TService> Create(AnyOf<string?, Enum>? name, int decoration, bool enumerate)
        {
            var nameAsString = name.AsString() ?? string.Empty;
            var decoratorName = decoration > 0 ? nameAsString.GetDecoratorName<TService>(decoration) : nameAsString;
            var factoryName = decoratorName.GetFactoryName<TService>();
            IEnumerable<TService> services;
            if (enumerate && decoration == 0)
                services = _serviceProvider.GetKeyedServices<TService>(factoryName);
            else
                services = [_serviceProvider.GetKeyedService<TService>(factoryName)!];
            if (services.FirstOrDefault() == null && _fallback != null)
            {
                services = [_fallback.Create(name)!];
            }
            if (services != null)
            {
                foreach (var service in services)
                {
                    if (service != null)
                    {
                        if (service is IServiceForFactory factoryService && !factoryService.FactoryNameAlreadySetup)
                        {
                            factoryService.SetFactoryName(nameAsString);
                            factoryService.FactoryNameAlreadySetup = true;
                        }
                        if (service is IFactoryName serviceWithName && !serviceWithName.FactoryNameAlreadySetup)
                        {
                            serviceWithName.SetFactoryName(name);
                            serviceWithName.FactoryNameAlreadySetup = true;
                        }
                        if (service is IDecoratorService<TService> decoratorService && decoration >= 0)
                            decoratorService.SetDecoratedServices(Create(name, decoration - 1, enumerate)!);
                        if (service is IServiceWithFactoryWithOptions serviceWithCustomOptions && !serviceWithCustomOptions.OptionsAlreadySetup)
                        {
                            var optionsName = nameAsString.GetOptionsName<TService>();
                            var options = _serviceProvider.GetKeyedService<IFactoryOptions>(optionsName.GetFactoryName<IFactoryOptions>());
                            if (options != null)
                            {
                                var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
                                if (map.OptionsSetter.TryGetValue(optionsName, out var optionsSetter))
                                    optionsSetter.Invoke(serviceWithCustomOptions, options);
                            }
                            serviceWithCustomOptions.OptionsAlreadySetup = true;
                        }
                        yield return service;
                    }
                }
            }
        }

        public bool Exists(AnyOf<string?, Enum>? name = null)
        {
            var nameAsString = name.AsString() ?? string.Empty;
            var factoryName = nameAsString.GetFactoryName<TService>();
            var service = _serviceProvider.GetKeyedService<TService>(factoryName);
            return service != null;
        }
    }
}
