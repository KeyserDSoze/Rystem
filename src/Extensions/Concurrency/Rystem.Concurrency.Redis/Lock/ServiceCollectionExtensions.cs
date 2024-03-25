using System.Threading.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisLock(this IServiceCollection services, Action<RedisConfiguration> configuration)
        {
            services.AddLockExecutor();
            return services.AddRedisLockable(configuration);
        }
        public static IServiceCollection AddLockExecutor<TLock>(this IServiceCollection services)
            where TLock : class, ILock
            => services.AddSingleton<ILock, TLock>();
    }
}
