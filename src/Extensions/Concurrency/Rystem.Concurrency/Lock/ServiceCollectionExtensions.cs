using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLock(this IServiceCollection services)
        {
            services.TryAddSingleton<ILock, LockExecutor>();
            return services.AddInMemoryLockable();
        }

        public static IServiceCollection AddLockExecutor<TLock>(this IServiceCollection services)
            where TLock : class, ILock
            => services.AddSingleton<ILock, TLock>();
    }
}