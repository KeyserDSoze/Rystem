using Rystem.Web.Components;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static RystemWebBuilder AddRystemWeb(this IServiceCollection services)
        {
            services.AddRazorPages();
            return new(services);
        }
    }
}
