using System.Linq.Expressions;

namespace RepositoryFramework.Web
{
    public interface IRepositoryPropertyUiHelper<T, TKey>
        where TKey : notnull
    {
        Task<Dictionary<string, PropertyUiSettings>> SettingsAsync(IServiceProvider serviceProvider, Entity<T, TKey>? entity = null);
        IRepositoryPropertyUiHelper<T, TKey> MapDefault<TProperty>(Expression<Func<T, TProperty>> navigationProperty, TProperty defaultValue);
        IRepositoryPropertyUiHelper<T, TKey> MapDefault<TProperty>(Expression<Func<T, TProperty>> navigationProperty, TKey defaultKey);
        IRepositoryPropertyUiHelper<T, TKey> SetTextEditor<TProperty>(Expression<Func<T, TProperty>> navigationProperty, int minHeight);
        IRepositoryPropertyUiHelper<T, TKey> MapChoice<TProperty>(Expression<Func<T, TProperty>> navigationProperty,
            Func<IServiceProvider, Entity<T, TKey>?, Task<IEnumerable<LabelValueDropdownItem>>> retriever,
            Func<TProperty, string> labelComparer);
        IRepositoryPropertyUiHelper<T, TKey> MapChoices<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> navigationProperty,
            Func<IServiceProvider, Entity<T, TKey>?, Task<IEnumerable<LabelValueDropdownItem>>> retriever,
            Func<TProperty, string> labelComparer);
    }
}
