using Microsoft.Extensions.Localization;
using RepositoryFramework.Web.Components.Resources;

namespace RepositoryFramework.Web.Components.Business.Language
{
    internal sealed class LocalizationHandler : ILocalizationHandler
    {
        private readonly IStringLocalizer _sharedLocalizer;
        private readonly Dictionary<string, IStringLocalizer> _localizations = new();
        public LocalizationHandler(IServiceProvider serviceProvider, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _sharedLocalizer = sharedLocalizer;
            foreach (var localizationInterface in RepositoryLocalizationOptions.Instance.LocalizationInterfaces)
                if (serviceProvider.GetService(localizationInterface.Value) is IStringLocalizer localizer)
                    _localizations.Add(localizationInterface.Key, localizer);
        }
        public string Get(string value, params object[] arguments)
            => _sharedLocalizer[value, arguments];
        public string Get<T>(string value, params object[] arguments)
            => Get(typeof(T), value, arguments);

        public string Get(Type type, string value, params object[] arguments)
        {
            var name = type.FullName!;
            if (_localizations.ContainsKey(name))
                return _localizations[name][value, arguments];
            return string.Format(value, arguments);
        }
    }
}
