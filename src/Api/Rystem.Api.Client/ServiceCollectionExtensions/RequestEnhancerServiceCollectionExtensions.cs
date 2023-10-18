using Rystem.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RystemApiServiceCollectionExtensions
    {
        public static IServiceCollection AddEnhancerForAllEndpoints<TEnhancer>(this IServiceCollection services)
            where TEnhancer : class, IRequestEnhancer
        {
            services
                .AddFactory<IRequestEnhancer, TEnhancer>("ApiHttpClient");
            return services;
        }
        public static IServiceCollection AddEnhancerForEndpoint<TEnhancer, T>(this IServiceCollection services)
            where TEnhancer : class, IRequestEnhancer
            where T : class
        {
            services
                .AddFactory<IRequestEnhancer, TEnhancer>($"ApiHttpClient_{typeof(T).FullName}");
            return services;
        }
    }
}
