using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static void SendInError<TService>(string name)
        {
            throw new ArgumentException($"Options name: {name} for your factory {typeof(TService).Name} already exists.");
        }
        public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddFactory<TService, TImplementation>(name, lifetime, () => SendInError<TService>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => SendInError<TService>(name ?? string.Empty));

        private static void InformThatItsAlreadyInstalled(ref bool check)
        {
            check = false;
        }

        public static bool TryAddFactory<TService, TImplementation>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
        {
            var check = true;
            services.AddFactory<TService, TImplementation>(name, lifetime, () => InformThatItsAlreadyInstalled(ref check), null);
            return check;
        }
        public static bool TryAddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TOptions>
            where TOptions : class, new()
        {
            var check = true;
            services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        public static bool TryAddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }

        public static async Task<bool> TryAddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }

        private static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists,
            Func<IServiceProvider, TService, TService>? addingBehaviorToFactory)
            where TService : class
            where TImplementation : class, TService
        {
            name ??= string.Empty;
            services.TryAddTransient(typeof(IFactory<>), typeof(Factory<>));
            if (Factory<TService>.Map.TryAdd(name, new()
            {
                ServiceFactory = ServiceFactory,
                ImplementationType = typeof(TImplementation)
            }))
            {
                services.TryAddService<TImplementation>(lifetime);
                services.AddOrOverrideService(serviceProvider => ServiceFactory(serviceProvider, false), lifetime);
            }
            else
                whenExists?.Invoke();

            TService ServiceFactory(IServiceProvider serviceProvider, bool withoutDecoration)
            {
                var factory = Factory<TService>.Map[name];
                var service = GetService(factory.ImplementationType, null);

                if (!withoutDecoration && factory.DecoratorTypes != null)
                {
                    foreach (var decoratorType in factory.DecoratorTypes)
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

                TService GetService(Type implementationType, Action<TService>? afterCreation)
                {
                    var service = (TService)serviceProvider.GetRequiredService(implementationType);
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
        private static TService AddOptionsToFactory<TService, TOptions>(
            IServiceProvider serviceProvider,
            TService service,
            Func<TOptions>? optionsCreator,
            string? optionsName)
             where TOptions : class
        {
            if (service is IServiceWithOptions<TOptions> serviceWithOptions)
            {
                if (optionsName != null)
                {
                    var optionsFactory = serviceProvider.GetRequiredService<IOptionsFactory<TOptions>>();
                    var options = optionsFactory.Create(optionsName);
                    serviceWithOptions.Options = options;
                }
                else
                    serviceWithOptions.Options = optionsCreator?.Invoke();
            }
            else if (service is IServiceWithOptions serviceWithCustomOptions)
            {
                var key = $"Rystem.Options.{typeof(TService).Name}.{typeof(TOptions).Name}.{optionsName}";
                if (!s_optionsSetter.ContainsKey(key))
                {
                    var property = serviceWithCustomOptions.GetType().GetProperty(nameof(IServiceWithOptions<TOptions>.Options));
                    if (property?.PropertyType != null)
                    {
                        if (optionsName != null)
                        {
                            var currentType = typeof(IOptionsFactory<>).MakeGenericType(property.PropertyType);
                            s_optionsSetter.TryAdd(key, (serviceProvider, service) =>
                            {
                                var optionsFactory = (dynamic)serviceProvider.GetRequiredService(currentType);
                                var options = optionsFactory.Create(optionsName);
                                property.SetValue(serviceWithCustomOptions, options);
                            });
                        }
                        else
                            s_optionsSetter.TryAdd(key, (serviceProvider, service) =>
                            {
                                property.SetValue(serviceWithCustomOptions, optionsCreator?.Invoke());
                            });
                    }
                }
                s_optionsSetter[key]?.Invoke(serviceProvider, serviceWithCustomOptions);
            }
            return service;
        }
        private static readonly Dictionary<string, Action<IServiceProvider, object>?> s_optionsSetter = new();
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                ServiceLifetime lifetime,
                Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithOptions<TOptions>
                where TOptions : class, new()
        {
            var optionsName = $"Rystem.Factory.{name}.{typeof(TService).Name}";
            services.AddOptions<TOptions>(optionsName)
                .Configure(createOptions);
            services.AddFactory<TService, TImplementation>(
                name,
                lifetime,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory<TService, TOptions>(serviceProvider, service, null, optionsName)
                );
            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
               where TService : class
               where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
               where TOptions : class, IServiceOptions<TBuiltOptions>, new()
               where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            services.AddFactory<TService, TImplementation>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions, null)
                );
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
                where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
                where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            services.AddFactory<TService, TImplementation>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions, null)
                );
            return services;
        }
    }
}
