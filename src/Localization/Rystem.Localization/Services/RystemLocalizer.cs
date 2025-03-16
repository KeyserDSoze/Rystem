using System.Globalization;

namespace Rystem.Localization
{
    internal class RystemLocalizer<T> : IRystemLocalizer<T>
    {
        private readonly ILanguages<T> _languages;

        public RystemLocalizer(ILanguages<T> languages)
        {
            _languages = languages;
        }
        private static readonly CultureInfo _defaultLanguage = new("en");
        private T GetLanguage()
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (_languages.Localizer.AllLanguages.TryGetValue(language, out var value))
            {
                return value;
            }
            else if (_languages.Localizer.AllLanguages.TryGetValue(_defaultLanguage.TwoLetterISOLanguageName, out var defaultValue))
            {
                return defaultValue;
            }
            return _languages.Localizer.AllLanguages.First().Value;
        }
        public T Instance => GetLanguage();
    }
}
