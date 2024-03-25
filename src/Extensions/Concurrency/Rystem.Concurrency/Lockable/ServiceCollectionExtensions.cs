using System.Threading.Concurrent;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryLockable(this IServiceCollection services)
        {
            services.TryAddSingleton<ILockable, MemoryLock>();
            return services;
        }
        public static IServiceCollection AddLockableIntegration<TLockable>(this IServiceCollection services)
            where TLockable : class, ILockable
            => services.AddSingleton<ILockable, TLockable>();
    }
}
