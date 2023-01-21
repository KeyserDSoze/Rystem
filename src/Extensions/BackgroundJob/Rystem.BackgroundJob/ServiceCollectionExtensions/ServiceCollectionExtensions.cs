using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Timers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBackgroundJobManager<TJobManager>(this IServiceCollection services)
            where TJobManager : class, IBackgroundJobManager
            => services.AddSingleton<IBackgroundJobManager, TJobManager>();
        public static IServiceCollection AddBackgroundJob<TJob>(this IServiceCollection services,
            Action<BackgroundJobOptions> options)
            where TJob : class, IBackgroundJob
        {
            services.TryAddSingleton<IBackgroundJobManager, BackgroundJobManager>();
            services.AddLock();
            services.AddTransient<TJob>();
            var bOptions = new BackgroundJobOptions()
            {
                Key = Guid.NewGuid().ToString(),
                Cron = "0 1 * * *",
                RunImmediately = false
            };
            options.Invoke(bOptions);
            services.AddWarmUp(serviceProvider => Start<TJob>(serviceProvider, bOptions));
            return services;
        }
        private static void Start<TJob>(IServiceProvider serviceProvider,
            BackgroundJobOptions options)
            where TJob : class, IBackgroundJob
        {
            var services = serviceProvider.CreateScope().ServiceProvider;
            var backgroundJobManager = services.GetService<IBackgroundJobManager>();
            if (backgroundJobManager != null)
            {
                string key = $"BackgroundWork_{options.Key}_{typeof(TJob).FullName}";
                backgroundJobManager.RunAsync(
                    serviceProvider.GetService<TJob>()!,
                    options,
                    () => services.CreateScope().ServiceProvider.GetService<TJob>() ?? throw new ArgumentException($"Background job {typeof(TJob).Name} not found."));
            }
            else
                throw new ArgumentException("Background job manager not found.");
        }
    }
}