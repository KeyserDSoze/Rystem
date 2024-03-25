using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using System.Threading.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisLockable(this IServiceCollection services, Action<RedisConfiguration> configuration)
        {
            var redisConfiguration = new RedisConfiguration();
            configuration(redisConfiguration);
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfiguration.ConnectionString));
            services.TryAddSingleton<ILockable, RedisLock>();
            return services;
        }
    }
}
