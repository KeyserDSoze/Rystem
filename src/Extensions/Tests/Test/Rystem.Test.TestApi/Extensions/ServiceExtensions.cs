using Rystem.Test.TestApi.Models;

namespace Rystem.Test.TestApi.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddTestServices(this IServiceCollection services)
        {
            services.AddRuntimeServiceProvider();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSingleton<SingletonService>();
            services.AddSingleton<Singleton2Service>();
            services.AddScoped<ScopedService>();
            services.AddScoped<Scoped2Service>();
            services.AddTransient<TransientService>();
            services.AddTransient<Transient2Service>();
            return services;
        }
        public static IApplicationBuilder UseTestApplication(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });
            return app;
        }
    }
}
