namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static bool TryAddFactory<TService, TImplementation>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
        {
            var check = true;
            services.AddFactory<TService, TImplementation>(name, lifetime, null, null, () => InformThatItsAlreadyInstalled(ref check), null);
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
            services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, lifetime, null, null, () => InformThatItsAlreadyInstalled(ref check));
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
            services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, null, null, () => InformThatItsAlreadyInstalled(ref check));
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
                .AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, null, null, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
    }
}
