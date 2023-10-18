using System.Diagnostics.Tracing;

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
            return Create(name, decorationCount, false).FirstOrDefault();
        }
        public TService? CreateWithoutDecoration(string? name = null)
            => Create(name, 0, false).FirstOrDefault();
        public IEnumerable<TService> CreateAll(string? name = null)
        {
            var map = _serviceProvider.GetRequiredService<ServiceFactoryMap>();
            var factoryName = name.GetFactoryName<TService>();
            var decorationCount = map.DecorationCount[factoryName];
            return Create(name, decorationCount, true);
        }
        public IEnumerable<TService> CreateAllWithoutDecoration(string? name = null)
            => Create(name, 0, true);
        private IEnumerable<TService> Create(string? name, int decoration, bool enumerate)
        {
            name ??= string.Empty;
            var decoratorName = decoration > 0 ? name.GetDecoratorName<TService>(decoration) : name;
            var factoryName = decoratorName.GetFactoryName<TService>();
            IEnumerable<TService> services;
            if (enumerate && decoration == 0)
                services = _serviceProvider.GetKeyedServices<TService>(factoryName);
            else
                services = new List<TService>() { _serviceProvider.GetKeyedService<TService>(factoryName)! };
            foreach (var service in services)
            {
                if (service != null)
                {
                    if (service is IServiceForFactory factoryService)
                        factoryService.SetFactoryName(name);
                    if (service is IDecoratorService<TService> decoratorService && decoration >= 0)
                        decoratorService.SetDecoratedServices(Create(name, decoration - 1, enumerate)!);
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
                    yield return service;
                }
            }
        }

        public bool Exists(string? name = null)
        {
            var factoryName = name.GetFactoryName<TService>();
            var service = _serviceProvider.GetKeyedService<TService>(factoryName);
            return service != null;
        }
    }
}
