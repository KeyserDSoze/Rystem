using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Web.Components
{
    public static class AppMenuBuilderExtensions
    {
        public static IAppMenuBuilder ConfigureAppMenu(this IServiceCollection services)
            => new AppMenuBuilder(services);
    }
}
