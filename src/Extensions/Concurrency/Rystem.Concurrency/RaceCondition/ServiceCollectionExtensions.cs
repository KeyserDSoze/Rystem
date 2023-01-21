using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Concurrent;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRaceCondition(this IServiceCollection services)
        {
            services.AddLock();
            services.TryAddSingleton<IRaceCodition, RaceConditionExecutor>();
            return services;
        }

        public static IServiceCollection AddRaceConditionExecutor<TRaceCondition>(this IServiceCollection services)
            where TRaceCondition : class, IRaceCodition
            => services.AddSingleton<IRaceCodition, TRaceCondition>();
    }
}