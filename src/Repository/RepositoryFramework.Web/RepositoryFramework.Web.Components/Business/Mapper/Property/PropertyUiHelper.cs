using System.Linq.Expressions;
using RepositoryFramework.Web.Components;

namespace RepositoryFramework.Web
{
    internal sealed class PropertyUiHelper<T, TKey> : IRepositoryPropertyUiHelper<T, TKey>
        where TKey : notnull
    {
        private readonly Dictionary<string, RepositoryUiPropertyConfiguratorHelper<T, TKey>> _retrieves = new();
        public async Task<Dictionary<string, PropertyUiSettings>> SettingsAsync(IServiceProvider serviceProvider, Entity<T, TKey>? entity = null)
        {
            var values = new Dictionary<string, PropertyUiSettings>();
            foreach (var helper in _retrieves)
            {
                values.Add(helper.Key, new PropertyUiSettings
                {
                    Default = helper.Value.Default,
                    DefaultKey = helper.Value.DefaultKey,
                    ValueRetriever = helper.Value.ValueRetriever,
                    IsMultiple = helper.Value.IsMultiple,
                    HasTextEditor = helper.Value.HasTextEditor,
                    MinHeight = helper.Value.MinHeight,
                    LabelComparer = helper.Value.LabelComparer,
                    Values = helper.Value.Retriever != null ? await helper.Value.Retriever(serviceProvider, entity).NoContext() : null
                });
            }
            return values;
        }
        private RepositoryUiPropertyConfiguratorHelper<T, TKey> GetHelper<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
        {
            var name = navigationProperty.Body.ToString();
            name = name.Contains('.') ? $"{Constant.ValueWithSeparator}{name[(name.IndexOf('.') + 1)..]}" : Constant.Value;
            if (!_retrieves.ContainsKey(name))
                _retrieves.Add(name, new RepositoryUiPropertyConfiguratorHelper<T, TKey>());
            var retrieve = _retrieves[name];
            return retrieve;
        }
        public IRepositoryPropertyUiHelper<T, TKey> MapDefault<TProperty>(Expression<Func<T, TProperty>> navigationProperty, TProperty defaultValue)
        {
            var retrieve = GetHelper(navigationProperty);
            retrieve.Default = defaultValue;
            return this;
        }
        public IRepositoryPropertyUiHelper<T, TKey> MapDefault<TProperty>(Expression<Func<T, TProperty>> navigationProperty, TKey defaultKey)
        {
            var retrieve = GetHelper(navigationProperty);
            retrieve.DefaultKey = defaultKey;
            var function = navigationProperty.Compile();
            retrieve.ValueRetriever = (entity) => entity != null ? function.Invoke((T)entity) : null;
            return this;
        }
        public IRepositoryPropertyUiHelper<T, TKey> SetTextEditor<TProperty>(Expression<Func<T, TProperty>> navigationProperty,
            int minHeight)
        {
            var retrieve = GetHelper(navigationProperty);
            retrieve.HasTextEditor = true;
            retrieve.MinHeight = minHeight;
            return this;
        }
        public IRepositoryPropertyUiHelper<T, TKey> MapChoice<TProperty>(Expression<Func<T, TProperty>> navigationProperty,
            Func<IServiceProvider, Entity<T, TKey>?, Task<IEnumerable<LabelValueDropdownItem>>> retriever,
            Func<TProperty, string> labelComparer)
        {
            var retrieve = GetHelper(navigationProperty);
            retrieve.IsMultiple = false;
            retrieve.Retriever = retriever;
            retrieve.LabelComparer = x => x != null ? labelComparer((TProperty)x) : string.Empty;
            return this;
        }
        public IRepositoryPropertyUiHelper<T, TKey> MapChoices<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> navigationProperty,
            Func<IServiceProvider, Entity<T, TKey>?, Task<IEnumerable<LabelValueDropdownItem>>> retriever,
            Func<TProperty, string> labelComparer)
        {
            var retrieve = GetHelper(navigationProperty);
            retrieve.IsMultiple = true;
            retrieve.Retriever = retriever;
            retrieve.LabelComparer = x => x != null ? labelComparer((TProperty)x) : string.Empty;
            return this;
        }
    }
}
