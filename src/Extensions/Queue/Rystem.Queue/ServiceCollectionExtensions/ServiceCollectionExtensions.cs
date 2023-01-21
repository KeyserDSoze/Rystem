using Rystem.Queue;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryQueue<T, TQueueManager>(this IServiceCollection services,
            Action<QueueProperty<T>>? options = null)
            where TQueueManager : class, IQueueManager<T>
            => services.AddQueueIntegration<T, TQueueManager, MemoryQueue<T>>(options);
        public static IServiceCollection AddMemoryStackQueue<T, TQueueManager>(this IServiceCollection services,
            Action<QueueProperty<T>>? options = null)
            where TQueueManager : class, IQueueManager<T>
            => services.AddQueueIntegration<T, TQueueManager, MemoryStackQueue<T>>(options);
        public static IServiceCollection AddQueueIntegration<T, TQueueManager, TQueue>(this IServiceCollection services,
            Action<QueueProperty<T>>? options = null)
            where TQueue : class, IQueue<T>
            where TQueueManager : class, IQueueManager<T>
        {
            var settings = new QueueProperty<T>();
            options?.Invoke(settings);
            services.AddSingleton(settings);
            services.AddSingleton<IQueue<T>, TQueue>();
            services.AddTransient<IQueueManager<T>, TQueueManager>();
            services.AddBackgroundJob<QueueJobManager<T>>(x =>
            {
                x.Cron = settings.BackgroundJobCronFormat;
                x.RunImmediately = false;
            });
            return services;
        }
    }
}