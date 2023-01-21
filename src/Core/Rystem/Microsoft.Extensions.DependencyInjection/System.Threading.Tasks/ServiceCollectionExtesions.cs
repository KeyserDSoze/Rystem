namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddWaitingTheSameThreadThatStartedTheTaskWhenUseNoContext(this IServiceCollection services)
        {
            RystemTask.WaitYourStartingThread = true;
            return services;
        }
    }
}
