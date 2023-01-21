using System.Linq.Expressions;
using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public sealed class OrderWrapper<T, TKey>
        where TKey : notnull
    {
        private readonly Dictionary<string, OrderValue<T, TKey>> _orders = new();
        public void Add(BaseProperty baseProperty, bool byDescending = false)
        {
            var expression = $"x => x.{baseProperty.GetFurtherProperty().Title}";
            var orderExpression = expression.Deserialize<T, object>();
            Remove(baseProperty);
            _orders.Add(baseProperty.NavigationPath, new OrderValue<T, TKey>
            {
                ByDescending = byDescending,
                Expression = expression,
                BaseProperty = baseProperty,
                LambdaExpression = orderExpression
            });
        }
        public void Remove(BaseProperty baseProperty)
        {
            if (_orders.ContainsKey(baseProperty.NavigationPath))
                _orders.Remove(baseProperty.NavigationPath);
        }
        public IEnumerable<string> GetExpressions()
        {
            foreach (var order in _orders)
                yield return $"{order.Key}_{order.Value.Expression}_{order.Value.ByDescending}";
        }
        public Expression<Func<T, object>>? Apply(QueryBuilder<T, TKey> queryBuilder)
        {
            var order = _orders.FirstOrDefault();
            if (order.Value != null)
            {
                Add(order.Value);
                foreach (var furtherOrder in _orders.Skip(1))
                    Add(furtherOrder.Value);
                void Add(OrderValue<T, TKey> orderValue)
                {
                    if (orderValue.ByDescending)
                        queryBuilder.OrderByDescending(orderValue.LambdaExpression);
                    else
                        queryBuilder.OrderBy(orderValue.LambdaExpression);
                }
            }
            return null;
        }
    }
}
