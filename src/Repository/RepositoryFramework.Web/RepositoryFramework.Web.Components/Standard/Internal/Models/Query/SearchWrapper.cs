using System.Linq.Expressions;
using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public sealed class SearchWrapper<T>
    {
        private readonly Dictionary<string, SearchValue<T>> _searched = new();
        public SearchValue<T> Get(BaseProperty baseProperty)
        {
            var name = $"{baseProperty.NavigationPath}.{baseProperty.Self.Name}";
            if (!_searched.ContainsKey(name))
                _searched.Add(name, new()
                {
                    BaseProperty = baseProperty
                });
            return _searched[name];
        }
        public IEnumerable<string> GetExpressions()
        {
            foreach (var search in _searched)
                if (search.Value.Expression != null)
                    yield return search.Value.Expression;
        }
        public IEnumerable<Expression<Func<T, bool>>> GetLambdaExpressions()
        {
            foreach (var search in _searched)
                if (search.Value.LambdaExpression != null)
                    yield return search.Value.LambdaExpression;
        }
    }
}
