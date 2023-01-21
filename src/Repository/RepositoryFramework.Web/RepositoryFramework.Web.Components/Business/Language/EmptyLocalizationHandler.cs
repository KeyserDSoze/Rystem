namespace RepositoryFramework.Web.Components.Business.Language
{
    internal sealed class EmptyLocalizationHandler : ILocalizationHandler
    {
        public string Get(string value, params object[] arguments) => string.Format(value, arguments);

        public string Get<T>(string value, params object[] arguments) => string.Format(value, arguments);

        public string Get(Type type, string value, params object[] arguments) => string.Format(value, arguments);
    }
}
