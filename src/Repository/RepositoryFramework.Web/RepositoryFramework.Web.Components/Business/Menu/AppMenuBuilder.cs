using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Web.Components
{
    internal sealed class AppMenuBuilder : IAppMenuBuilder
    {
        public IServiceCollection Services { get; }
        public AppMenuBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public IAppMenuBuilder AddSingleItem(AppMenuSingleItem singleItem)
        {
            AppMenu.Items.Add(singleItem);
            return this;
        }
        public IAppMenuBuilder AddComplexItem(AppMenuComplexItem complexItem)
        {
            AppMenu.Items.Add(complexItem);
            return this;
        }
    }
}
