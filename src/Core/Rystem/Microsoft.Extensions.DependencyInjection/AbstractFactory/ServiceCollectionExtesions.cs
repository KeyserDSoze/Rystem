using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TInterface, TClass>(this IServiceCollection services,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface
        {
            var count = services.Count(x => x.ServiceType == typeof(TInterface));
            if (Factory<TInterface>.Map.TryAdd(name ?? string.Empty, count))
            {
                services.AddService<TInterface, TClass>(serviceLifetime);
                services.TryAddTransient<IFactory<TInterface>, Factory<TInterface>>();
                return services;
            }
            else
                throw new ArgumentException($"Service name: {name} for your factory {typeof(TInterface).Name} already exists.");
        }

        public static IServiceCollection AddFactory<TInterface, TClass, TOptions>(this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name = null,
                ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
                where TInterface : class
                where TClass : class, TInterface, IServiceWithOptions<TOptions>
                where TOptions : class, new()
        {

            services.AddFactory<TInterface, TClass>(name, serviceLifetime);
            var countOptions = services.Count(x => x.ServiceType == typeof(TOptions));
            if (Factory<TInterface>.MapOptions.TryAdd(name ?? string.Empty, new()
            {
                Index = countOptions,
                Type = typeof(TOptions),
                Setter = (service, options) =>
                {
                    if (service is IServiceWithOptions<TOptions> factoryWithOptions && options is TOptions tOptions)
                    {
                        factoryWithOptions.Options = tOptions;
                    }
                }
            }))
            {
                TOptions options = new();
                createOptions.Invoke(options);
                services.AddSingleton(options);
            }
            else
                throw new ArgumentException($"Options name: {name} for your factory {typeof(TInterface).Name} already exists.");
            return services;
        }
        public static async Task<IServiceCollection> AddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name = null,
                ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
                where TInterface : class
                where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
                where TOptions : class, IServiceOptions<TBuiltOptions>, new()
                where TBuiltOptions : class
        {

            services.AddFactory<TInterface, TClass>(name, serviceLifetime);
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            if (!Factory<TInterface>.MapBuiltOptions.TryAdd(name ?? string.Empty, new()
            {
                Getter = builtOptions,
                Setter = (service, options) =>
                {
                    if (service is IServiceWithOptions<TBuiltOptions> factoryWithOptions && options is TBuiltOptions tOptions)
                    {
                        factoryWithOptions.Options = tOptions;
                    }
                }
            }))
                throw new ArgumentException($"Options name: {name} for your factory {typeof(TInterface).Name} already exists.");
            return services;
        }
    }
}
