﻿namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static bool TryAddFactory<TService>(this IServiceCollection services,
           TService implementationInstance,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
        {
            var check = true;
            services.AddEngineFactory<TService, TService>(name, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), null);
            return check;
        }
        public static bool TryAddFactory<TService, TOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceForFactoryWithOptions<TOptions>
            where TOptions : class, new()
        {
            var check = true;
            services.AddFactory<TService, TService, TOptions>(createOptions, name, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
        public static bool TryAddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceForFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }

        public static async Task<bool> TryAddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceForFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check));
            return check;
        }
    }
}