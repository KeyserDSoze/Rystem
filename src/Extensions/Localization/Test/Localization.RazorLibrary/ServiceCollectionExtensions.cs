using Localization.RazorLibrary.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Localization.RazorLibrary
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLibraryLocalization(this IServiceCollection services)
        {
            services.AddMultipleLocalization<Shared>(x =>
            {
                x.ResourcesPath = string.Empty;
            });
            //services.AddLocalization(x =>
            //{
            //    x.ResourcesPath = string.Empty;
            //});
            return services;
        }
    }
}
