namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static Task<TResponse> ExecuteUntilNowAsync<TResponse, TService>(
            this IServiceCollection services,
            Func<TService, Task<TResponse>> actionToDo)
            where TService : class
        {
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            return actionToDo(serviceProvider.GetService<TService>()!);
        }
        public static Task<TResponse> ExecuteUntilNowAsync<TResponse>(
            this IServiceCollection services,
            Func<IServiceProvider, Task<TResponse>> actionToDo)
        {
            var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
            return actionToDo(serviceProvider);
        }
    }
}
