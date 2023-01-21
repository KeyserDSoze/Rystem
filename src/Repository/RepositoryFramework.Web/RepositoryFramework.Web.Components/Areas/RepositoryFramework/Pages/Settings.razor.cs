using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using RepositoryFramework.Web.Components.Business.Language;

namespace RepositoryFramework.Web.Components
{
    public partial class Settings
    {
        [Inject]
        public AppPaletteWrapper AppPaletteWrapper { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;
        [CascadingParameter(Name = nameof(HttpContext))]
        public HttpContext HttpContext { get; set; }
        private string? _paletteKey;
        private string? _languageKey;
        protected override async Task OnParametersSetAsync()
        {
            if (!HttpContext.Request.Cookies.TryGetValue(Constant.PaletteKey, out _paletteKey))
                _paletteKey = AppPaletteWrapper.Skins.First().Key;
            if (RepositoryLocalizationOptions.Instance.HasLocalization)
                _languageKey = Thread.CurrentThread.CurrentCulture.Name;
            LoadService.Hide();
            await base.OnParametersSetAsync().NoContext();
        }
        private IEnumerable<LabelValueDropdownItem> GetSkins()
        {
            foreach (var skin in AppPaletteWrapper.Skins)
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
