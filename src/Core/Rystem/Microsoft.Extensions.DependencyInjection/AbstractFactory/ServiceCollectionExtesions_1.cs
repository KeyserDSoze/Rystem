using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
            => services.AddFactory<TService>(name, lifetime, () => SendInError<TService>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));

        public static bool TryAddFactory<TService>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
        {
            var check = true;
            services.AddFactory<TService>(name, lifetime, () => InformThatItsAlreadyInstalled(ref check), null);
            return check;
        }
        public static bool TryAddFactory<TService, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TOptions>
            where TOptions : class, new()
        {
            var check = true;
            services.AddFactory<TService, TOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        public static bool TryAddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            services.AddFactory<TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }

        public static async Task<bool> TryAddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        private static IServiceCollection AddFactory<TService>(this IServiceCollection services,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists,
            Func<IServiceProvider, TService, TService>? addingBehaviorToFactory)
            where TService : class
        {
            name ??= string.Empty;
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            var count = services.Count(x => x.ServiceType == typeof(TService));
            if (map.Services.TryAdd(name, new()
            {
                ServiceFactory = ServiceFactory,
                Implementation = new()
                {
                    Type = typeof(TService),
                    Index = count
                }
            }))
            {
                services.AddService<TService>(lifetime);
            }
            else
                whenExists?.Invoke();

            TService ServiceFactory(IServiceProvider serviceProvider, bool withoutDecoration)
            {
                var factory = map.Services[name];
                var service = GetService(factory.Implementation, null);

                if (!withoutDecoration && factory.Decorators != null)
                {
                    foreach (var decoratorType in factory.Decorators)
                    {
                        service = GetService(decoratorType,
                            decorator =>
                            {
                                if (decorator is IDecoratorService<TService> decorateWithService)
                                {
                                    decorateWithService.SetDecoratedService(service);
                                }
                            });
                    }
                }
                return service;

                TService GetService(FactoryServiceType implementationType, Action<TService>? afterCreation)
                {
                    var service = (TService)serviceProvider.GetServices(implementationType.Type).Skip(implementationType.Index).First()!;
                    addingBehaviorToFactory?.Invoke(serviceProvider, service);
                    afterCreation?.Invoke(service);
                    if (service is IFactoryService factoryService)
                        factoryService.SetFactoryName(name);
                    foreach (var behavior in factory.FurtherBehaviors.Select(x => x.Value))
                        service = behavior.Invoke(serviceProvider, service);
                    return service;
                }
            }

            return services;
        }
        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                ServiceLifetime lifetime,
                Action? whenExists)
                where TService : class, IServiceWithOptions<TOptions>
                where TOptions : class, new()
        {
            var optionsName = GetOptionsName<TService, TOptions>(name ?? string.Empty);
            services.AddOptions<TOptions>(optionsName)
                .Configure(createOptions);
            services.AddFactory<TService>(
                name,
                lifetime,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory<TService, TOptions>(serviceProvider, service, null, optionsName)
                );
            return services;
        }
        private static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
               where TService : class, IServiceWithOptions<TBuiltOptions>
               where TOptions : class, IServiceOptions<TBuiltOptions>, new()
               where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            services.AddFactory<TService>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions, null)
                );
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
                where TService : class, IServiceWithOptions<TBuiltOptions>
                where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
                where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            services.AddFactory<TService>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions, null)
                );
            return services;
        }
    }
}
