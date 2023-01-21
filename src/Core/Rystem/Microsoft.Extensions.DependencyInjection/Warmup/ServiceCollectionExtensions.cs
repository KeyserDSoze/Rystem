using Rystem;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWarmUp(this IServiceCollection services,
            Func<IServiceProvider, Task> actionAfterBuild)
        {
            ServiceProviderUtility.Instance.AfterBuildEvents.Add(actionAfterBuild);
            return services;
        }
        public static IServiceCollection AddWarmUp(this IServiceCollection services,
            Action<IServiceProvider> actionAfterBuild)
        {
            ServiceProviderUtility.Instance.AfterBuildEvents.Add((serviceProvider) => 
            {
                actionAfterBuild.Invoke(serviceProvider); 
                return Task.CompletedTask; 
            });
            return services;
        }
        public static async Task<TServiceProvider> WarmUpAsync<TServiceProvider>(this TServiceProvider serviceProvider)
            where TServiceProvider : IServiceProvider
        {
            await ServiceProviderUtility.Instance.AfterBuildAsync(serviceProvider);
            return serviceProvider;
        }
    }
}
