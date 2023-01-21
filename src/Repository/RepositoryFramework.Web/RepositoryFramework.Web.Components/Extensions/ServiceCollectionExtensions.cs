using System.Reflection;
using Radzen;
using RepositoryFramework.Web.Components;
using RepositoryFramework.Web.Components.Builder;
using RepositoryFramework.Web.Components.Business.Language;
using RepositoryFramework.Web.Components.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IRepositoryUiBuilder AddRepositoryUi(this IServiceCollection services,
           Action<AppSettings> settings)
        {
            var options = new AppSettings()
            {
                Name = "Repository App",
            };
            settings.Invoke(options);
            services.AddSingleton(options);
            services.AddHttpContextAccessor();
            services.AddSingleton<IAppMenu, AppMenu>();
            services.AddSingleton<IPolicyEvaluatorManager, PolicyEvaluatorManager>();
            services.AddScoped<ILoaderService, LoadService>();
            services.AddSingleton<ILocalizationHandler, EmptyLocalizationHandler>();
            services
                .AddScoped<DialogService>()
                .AddScoped<NotificationService>()
                .AddScoped<TooltipService>()
                .AddScoped<ContextMenuService>();
            services.AddScoped<ICopyService, CopyService>();
            services.AddRazorPages();
            return new RepositoryUiBuilder(services);
        }
        public static IRepositoryUiBuilder AddRepositoryUi<T>(this IServiceCollection services,
           Action<AppSettings> settings)
        {
            var options = new AppSettings()
            {
                Name = "Repository App",
                RazorPagesForRoutingAdditionalAssemblies = new Assembly[1] { typeof(T).Assembly }
            };
            settings.Invoke(options);
            return services
                .AddRepositoryUi(settings);
        }
    }
}
