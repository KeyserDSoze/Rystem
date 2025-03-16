using System.Globalization;

namespace Rystem.Localization
{
    internal class RepositoryLocalizer<T> : IRepositoryLocalizer<T>
    {
        private readonly ILanguages<T> _languages;

        public RepositoryLocalizer(ILanguages<T> languages)
        {
            _languages = languages;
        }
        private static readonly CultureInfo s_defaultLanguage = new("en");
        private T GetLanguage()
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_languages.Localizer.AllLanguages.TryGetValue(language, out var value))
            {
                return value;
            }
            else if (_languages.Localizer.AllLanguages.TryGetValue(s_defaultLanguage.TwoLetterISOLanguageName, out var defaultValue))
            {
                return defaultValue;
            }
            return _languages.Localizer.AllLanguages.First().Value;
        }
        public T Instance => GetLanguage();
        public static implicit operator T(RepositoryLocalizer<T> localizer)
        {
            return localizer.Instance;
        }
    }
}
