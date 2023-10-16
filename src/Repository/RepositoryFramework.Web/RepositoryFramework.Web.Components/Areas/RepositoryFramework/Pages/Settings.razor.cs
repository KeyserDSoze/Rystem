using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.Web.Components.Business.Language;
using RepositoryFramework.Web.Components.Services;

namespace RepositoryFramework.Web.Components
{
    public partial class Settings : ComponentBase
    {
        [Inject]
        public IServiceProvider ServiceProvider { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;
        [Inject]
        public ILoaderService LoadService { get; set; }
        [CascadingParameter(Name = nameof(HttpContext))]
        public HttpContext HttpContext { get; set; }
        private bool _hasPalette;
        private string? _paletteKey;
        private string? _languageKey;
        protected override async Task OnParametersSetAsync()
        {
            AppPaletteWrapper? appPaletteWrapper = ServiceProvider.GetService<AppPaletteWrapper>();
            _hasPalette = appPaletteWrapper != null;
            if (appPaletteWrapper != null && !HttpContext.Request.Cookies.TryGetValue(Constant.PaletteKey, out _paletteKey))
                _paletteKey = appPaletteWrapper.Skins.First().Key;
            if (RepositoryLocalizationOptions.Instance.HasLocalization)
                _languageKey = Thread.CurrentThread.CurrentCulture.Name;
            LoadService.Hide();
            await base.OnParametersSetAsync().NoContext();
        }
        private IEnumerable<LabelValueDropdownItem> GetSkins()
        {
            AppPaletteWrapper? appPaletteWrapper = ServiceProvider.GetService<AppPaletteWrapper>();
            if (appPaletteWrapper != null)
                foreach (var skin in appPaletteWrapper.Skins)
                    yield return new LabelValueDropdownItem
                    {
                        Id = skin.Key,
                        Label = skin.Key,
                        Value = skin.Value
                    };
        }
        private ValueTask ChangeSkin(LabelValueDropdownItem skin)
        {
            NavigationManager.NavigateTo($"../../../../Repository/Settings/Theme/{skin.Id}", true);
            return ValueTask.CompletedTask;
        }
        private IEnumerable<LabelValueDropdownItem> GetLanguages()
        {
            foreach (var language in RepositoryLocalizationOptions.Instance.SupportedCultures)
                yield return new LabelValueDropdownItem
                {
                    Id = language.Name,
                    Label = language.NativeName,
                    Value = language
                };
        }
        private ValueTask ChangeLanguage(LabelValueDropdownItem language)
        {
            NavigationManager.NavigateTo($"../../../../Repository/Language/{language.Id}", true);
            return ValueTask.CompletedTask;
        }
    }
}
