using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace Rystem.PlayFramework.Test.Api
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Weather", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            services.AddChat(x =>
            {
                x.AddConfiguration("openai", builder =>
                {
                    builder.ApiKey = configuration["OpenAi:Key"]!;
                    builder.Uri = configuration["OpenAi:Uri"]!;
                    builder.Model = configuration["OpenAi:Model"]!;
                    builder.ChatClientBuilder = (chatClient) =>
                    {
                        chatClient.AddPriceModel(new ChatPriceSettings
                        {
                            InputToken = 0.02M,
                            OutputToken = 0.02M
                        });
                    };
                });
            });
            services.AddHttpClient("apiDomain", x =>
            {
                x.BaseAddress = new Uri(configuration["Api:Uri"]!);
            });
            services.AddPlayFramework(scenes =>
            {
                scenes.Configure(settings =>
                {
                    settings.OpenAi.Name = "openai";
                })
                .AddScene(scene =>
                {
                    scene
                        .WithName("Weather")
                        .WithDescription("Get information about the weather")
                        .WithHttpClient("apiDomain")
                        .WithOpenAi(null)
                        .WithApi(pathBuilder =>
                        {
                            pathBuilder
                                .Map(new Regex("Country/*"))
                                .Map(new Regex("City/*"))
                                .Map("Weather/");
                        })
                            .WithActors(actors =>
                            {
                                actors
                                    .AddActor("Nel caso non esistesse la città richiesta potresti aggiungerla con il numero dei suoi abitanti.")
                                    .AddActor("Ricordati che va sempre aggiunta anche la nazione, quindi se non c'è la nazione aggiungi anche quella.")
                                    .AddActor("Non chiamare alcun meteo prima di assicurarti che tutto sia stato popolato correttamente.")
                                    .AddActor<ActorWithDbRequest>();
                            });
                });
            });
            return services;
        }
    }
    public static class WebApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseEndpoints(x => x.MapControllers());
            app.UseAiEndpoints();
            return app;
        }
    }
}
