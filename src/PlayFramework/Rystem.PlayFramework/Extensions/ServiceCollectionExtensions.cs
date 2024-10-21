using Rystem.PlayFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPlayFramework(this IServiceCollection services,
            Action<IScenesBuilder> builder)
        {
            services.AddPopulationService();
            services.AddHttpContextAccessor();
            services.AddTransient<ISceneManager, SceneManager>();
            services.AddSingleton<ActorsOpenAiEndpointParser>();
            var sceneBuilder = new ScenesBuilder(services);
            sceneBuilder.AddCustomDirector<MainDirector>();
            builder(sceneBuilder);
            return services;
        }
        public static IServiceCollection AddChat(
           this IServiceCollection services,
           Action<IChatBuilder> builder)
        {
            var chatBuilder = new ChatBuilder(services);
            builder(chatBuilder);
            return services;
        }
    }
}
