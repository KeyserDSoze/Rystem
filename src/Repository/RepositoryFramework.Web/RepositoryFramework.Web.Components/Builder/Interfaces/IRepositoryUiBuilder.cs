using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Web.Components.Builder
{
    public interface IRepositoryUiBuilder
    {
        IServiceCollection Services { get; }
        IRepositoryUiBuilder AddSkinForUi(string name, Action<AppPalette> settings);
        IRepositoryUiBuilder AddDefaultSkinForUi();
        IRepositoryUiBuilder WithAuthenticatedUi();
        IRepositoryUiBuilder AddDefaultLocalization();
    }
}
