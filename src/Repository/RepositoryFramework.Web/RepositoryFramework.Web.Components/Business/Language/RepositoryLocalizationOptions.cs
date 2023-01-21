using System.Globalization;

namespace RepositoryFramework.Web.Components.Business.Language
{
    internal sealed class RepositoryLocalizationOptions
    {
        public static RepositoryLocalizationOptions Instance { get; } = new RepositoryLocalizationOptions();
        private RepositoryLocalizationOptions() { }
        public bool HasLocalization { get; internal set; }
        public List<CultureInfo> SupportedCultures { get; internal set; } = new() { new("en-US"), new("es-ES"), new("it-IT"), new("fr-FR"), new("de-DE") };
        public Dictionary<string, Type> LocalizationInterfaces { get; } = new();
        public void AddLocalization<T, TLocalizer>()
        {
            var name = typeof(T).FullName!;
            if (!LocalizationInterfaces.ContainsKey(name))
                LocalizationInterfaces.Add(name, typeof(TLocalizer));
            else
                LocalizationInterfaces[name] = typeof(TLocalizer);
        }
    }
}
