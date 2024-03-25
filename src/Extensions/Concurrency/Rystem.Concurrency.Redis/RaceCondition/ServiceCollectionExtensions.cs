using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRaceConditionWithRedis(this IServiceCollection services, Action<RedisConfiguration> configuration)
        {
            services.AddRedisLock(configuration);
            services.AddRaceConditionExecutor();
            return services;
        }
    }
}
