using Microsoft.Extensions.DependencyInjection;

namespace Rystem
{
    internal sealed class ServiceProviderUtility
    {
        public static ServiceProviderUtility Instance { get; set; } = new();
        private ServiceProviderUtility() { }
        public List<Func<IServiceProvider, Task>> AfterBuildEvents { get; } = new();
        public async Task AfterBuildAsync(IServiceProvider providers)
        {
            foreach (var buildEvent in AfterBuildEvents)
            {
                var scope = providers.CreateScope();
                _ = await Try.WithDefaultOnCatchAsync(() => buildEvent.Invoke(scope.ServiceProvider));
                scope.Dispose();
            }
        }
    }
}