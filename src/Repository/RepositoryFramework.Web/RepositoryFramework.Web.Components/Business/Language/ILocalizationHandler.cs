namespace RepositoryFramework.Web.Components.Business.Language
{
    public interface ILocalizationHandler
    {
        string Get(string value, params object[] arguments);
        string Get<T>(string value, params object[] arguments);
        string Get(Type type, string value, params object[] arguments);
    }
}
