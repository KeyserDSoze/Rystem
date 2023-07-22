using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static void SendInError<TInterface>(string name)
        {
            throw new ArgumentException($"Options name: {name} for your factory {typeof(TInterface).Name} already exists.");
        }
        public static IServiceCollection AddFactory<TInterface, TClass>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
           where TInterface : class
           where TClass : class, TInterface
            => services.AddFactory<TInterface, TClass>(name, serviceLifetime, () => SendInError<TInterface>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TInterface, TClass, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TInterface, TClass, TOptions>(createOptions, name, serviceLifetime, () => SendInError<TInterface>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TInterface, TClass, TOptions, TBuiltOptions>(createOptions, name, serviceLifetime, () => SendInError<TInterface>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(createOptions, name, serviceLifetime, () => SendInError<TInterface>(name ?? string.Empty));

        private static void InformThatItsAlreadyInstalled(ref bool check)
        {
            check = false;
        }

        public static bool TryAddFactory<TInterface, TClass>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
           where TInterface : class
           where TClass : class, TInterface
        {
            var check = true;
            services.AddFactory<TInterface, TClass>(name, serviceLifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        public static bool TryAddFactory<TInterface, TClass, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TOptions>
            where TOptions : class, new()
        {
            var check = true;
            services.AddFactory<TInterface, TClass, TOptions>(createOptions, name, serviceLifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        public static bool TryAddFactory<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            services.AddFactory<TInterface, TClass, TOptions, TBuiltOptions>(createOptions, name, serviceLifetime, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }

        public static async Task<bool> TryAddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            await services
                .AddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(createOptions, name, serviceLifetime, () => InformThatItsAlreadyInstalled(ref check))
                .NoContext();
            return check;
        }

        private static IServiceCollection AddFactory<TInterface, TClass>(this IServiceCollection services,
            string? name,
            ServiceLifetime serviceLifetime,
            Action? whenExists)
            where TInterface : class
            where TClass : class, TInterface
        {
            services.TryAddTransient(typeof(IFactory<>), typeof(Factory<>));
            var count = services.Count(x => x.ServiceType == typeof(TInterface));
            if (Factory<TInterface>.Map.TryAdd(name ?? string.Empty, count))
            {
                services.AddService<TInterface, TClass>(serviceLifetime);
            }
            else
                whenExists?.Invoke();
            return services;
        }

        public static IServiceCollection AddFactory<TInterface, TClass, TOptions>(this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                ServiceLifetime serviceLifetime,
                Action? whenExists)
                where TInterface : class
                where TClass : class, TInterface, IServiceWithOptions<TOptions>
                where TOptions : class, new()
        {

            services.AddFactory<TInterface, TClass>(name, serviceLifetime, whenExists);
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
                whenExists?.Invoke();
            return services;
        }
        private static IServiceCollection AddFactory<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime serviceLifetime,
            Action? whenExists)
               where TInterface : class
               where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
               where TOptions : class, IServiceOptions<TBuiltOptions>, new()
               where TBuiltOptions : class
        {
            var countOptions = services.Count(x => x.ServiceType == typeof(TBuiltOptions));
            services.AddFactory<TInterface, TClass>(name, serviceLifetime, whenExists);
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            if (serviceLifetime == ServiceLifetime.Transient)
                services.AddTransient(serviceProvider => builtOptions.Invoke());
            else if (serviceLifetime == ServiceLifetime.Singleton)
                services.AddSingleton(serviceProvider => builtOptions.Invoke());
            else
                services.AddScoped(serviceProvider => builtOptions.Invoke());
            if (!Factory<TInterface>.MapOptions.TryAdd(name ?? string.Empty, new()
            {
                Index = countOptions,
                Type = typeof(TBuiltOptions),
                Setter = (service, options) =>
                {
                    if (service is IServiceWithOptions<TBuiltOptions> factoryWithOptions && options is TBuiltOptions tOptions)
                    {
                        factoryWithOptions.Options = tOptions;
                    }
                }
            }))
                whenExists?.Invoke();
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TInterface, TClass, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime serviceLifetime,
            Action? whenExists)
                where TInterface : class
                where TClass : class, TInterface, IServiceWithOptions<TBuiltOptions>
                where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
                where TBuiltOptions : class
        {
            var countOptions = services.Count(x => x.ServiceType == typeof(TBuiltOptions));
            services.AddFactory<TInterface, TClass>(name, serviceLifetime, whenExists);
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync().NoContext();
            if (serviceLifetime == ServiceLifetime.Transient)
                services.AddTransient(serviceProvider => builtOptions.Invoke());
            else if (serviceLifetime == ServiceLifetime.Singleton)
                services.AddSingleton(serviceProvider => builtOptions.Invoke());
            else
                services.AddScoped(serviceProvider => builtOptions.Invoke());
            if (!Factory<TInterface>.MapOptions.TryAdd(name ?? string.Empty, new()
            {
                Index = countOptions,
                Type = typeof(TBuiltOptions),
                Setter = (service, options) =>
                {
                    if (service is IServiceWithOptions<TBuiltOptions> factoryWithOptions && options is TBuiltOptions tOptions)
                    {
                        factoryWithOptions.Options = tOptions;
                    }
                }
            }))
                whenExists?.Invoke();
            return services;
        }
    }
}
