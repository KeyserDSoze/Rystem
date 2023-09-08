namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWaitingTheSameThreadThatStartedTheTaskWhenUseNoContext(this IServiceCollection services)
        {
            RystemTask.WaitYourStartingThread = true;
            return services;
        }
    }
}
