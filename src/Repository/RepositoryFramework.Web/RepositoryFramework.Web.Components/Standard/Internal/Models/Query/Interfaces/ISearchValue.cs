using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public interface ISearchValue
    {
        string? Expression { get; }
        void UpdateLambda(string? expression);
        BaseProperty BaseProperty { get; }
    }
}
