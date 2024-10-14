using Rystem.OpenAi.Actors;

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
            services.AddSwaggerGen(setup =>
            {
                setup.OperationFilter<ActorsOpenAiFilter>();
            });
            var actorBuilder = new ScenesBuilder(services);
            builder(actorBuilder);
            return services;
        }
    }
}
