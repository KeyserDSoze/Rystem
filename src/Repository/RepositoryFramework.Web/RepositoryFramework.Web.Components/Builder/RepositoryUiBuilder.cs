using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework.Web.Components.Business.Language;

namespace RepositoryFramework.Web.Components.Builder
{
    internal sealed class RepositoryUiBuilder : IRepositoryUiBuilder
    {
        public IServiceCollection Services { get; }
        public RepositoryUiBuilder(IServiceCollection services)
            => Services = services;
        public IRepositoryUiBuilder AddSkinForUi(string name, Action<AppPalette> settings)
        {
            var palette = new AppPalette();
            settings.Invoke(palette);
            AppPaletteWrapper.Instance.Skins.Add(name, palette);
            Services.AddSingleton(AppPaletteWrapper.Instance);
            return this;
        }
        public IRepositoryUiBuilder AddDefaultSkinForUi()
        {
            AddSkinForUi("Light", x => { });
            AddSkinForUi("Dark", x =>
            {
                x.Primary = "#375a7f";
                x.Secondary = "#626262";
                x.Success = "#00bc8c";
                x.Info = "#17a2b8";
                x.Warning = "#f39c12";
                x.Danger = "#e74c3c";
                x.Light = "#3b3b3b";
                x.Dark = "#9e9e9e";
                x.BackgroundColor = "#222";
                x.Color = "#e1e1e1";
                x.Table.Color = "#e1e1e1";
                x.Table.Background = "#222";
                x.Table.StripedColor = "#d1d1d1";
                x.Table.StripedBackground = "#333";
                x.Link.Color = "#eee";
                x.Link.Hover = "#bbb";
                x.Button.Color = "#ff6d41";
                x.Button.Background = "#35a0d7";
            });
            return this;
        }
        public IRepositoryUiBuilder WithAuthenticatedUi()
        {
            AppInternalSettings.Instance.IsAuthenticated = true;
            return this;
        }

        public IRepositoryUiBuilder AddDefaultLocalization()
        {
            Services
                .AddLocalization(options => options.ResourcesPath = string.Empty);
            RepositoryLocalizationOptions.Instance.HasLocalization = true;
            Services.Remove(Services.First(x => x.ServiceType == typeof(ILocalizationHandler)));
            Services.TryAddTransient<ILocalizationHandler, LocalizationHandler>();
            return this;
        }
    }
}
