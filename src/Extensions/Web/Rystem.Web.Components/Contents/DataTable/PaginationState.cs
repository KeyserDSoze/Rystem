using System.Linq.Expressions;
using System.Reflection;

namespace Rystem.Web.Components.Contents.DataTable
{
    public sealed class PaginationState
    {
        public int ItemsPerPage { get; private set; } = int.MaxValue;
        public int CurrentPageIndex { get; set; }
        public int? TotalItemCount { get; private set; }
        public int? LastPageIndex => (TotalItemCount - 1) / ItemsPerPage;
        public int SkipValue => ItemsPerPage * CurrentPageIndex;
        public PaginationState SetTotalItemCount(int total)
        {
            TotalItemCount = total;
            CurrentPageIndex = 1;
            return this;
        }
        public PaginationState SetItemsPerPage(int itemsPerPage)
        {
            ItemsPerPage = itemsPerPage;
            CurrentPageIndex = 1;
            return this;
        }
    }
    internal enum OrderingType
    {
        None,
        Ascending,
        Descending
    }
    internal sealed class ColumnOptions
    {
        public bool IsActive { get; set; } = true;
        public OrderingType Order { get; set; }
        public Type Type { get; set; }
        public string Value { get; set; }
        public string Label { get; set; }
    }
    public sealed class FilterWrapper<T>
    {
        public SearchWrapper<T> Search { get; } = new();
        public OrderWrapper<T> Order { get; } = new();
        public IEnumerable<T> Apply(IEnumerable<T> items)
        {
            foreach (var searched in Search.GetLambdaExpressions())
                items = items.Where(searched);
            var order = Order.GetOrders().FirstOrDefault();
            if (order != null)
            {
                Add(order);
                foreach (var furtherOrder in Order.GetOrders().Skip(1))
                    Add(furtherOrder.Value);
                void Add(OrderValue<T> orderValue)
                {
                    if (orderValue.ByDescending)
                        queryBuilder.OrderByDescending(orderValue.LambdaExpression);
                    else
                        queryBuilder.OrderBy(orderValue.LambdaExpression);
                }
            }
            return items;
        }
    }
    public sealed class SearchValue<T>
    {
        public string? Expression { get; private set; }
        public Expression<Func<T, bool>>? LambdaExpression { get; private set; }
        public required BaseProperty BaseProperty { get; init; }
        public void UpdateLambda(string? expression)
        {
            if (expression != null)
            {
                Expression = expression;
                LambdaExpression = expression.Deserialize<T, bool>();
            }
            else
            {
                Expression = null;
                LambdaExpression = null;
            }
        }
    }
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
    public sealed class OrderWrapper<T>
    {
        private readonly Dictionary<string, OrderValue<T>> _orders = new();
        public void Add(BaseProperty baseProperty, bool byDescending = false)
        {
            var expression = $"x => x.{baseProperty.NavigationPath}";
            var orderExpression = expression.Deserialize<T, object>();
            Remove(baseProperty);
            _orders.Add(baseProperty.NavigationPath, new OrderValue<T>
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
        public IEnumerable<OrderValue<T>> GetOrders()
            => _orders.Select(x => x.Value);
    }
    public sealed class OrderValue<T>
    {
        public required bool ByDescending { get; init; }
        public required string Expression { get; init; }
        public required Expression<Func<T, object>> LambdaExpression { get; init; }
        public required BaseProperty BaseProperty { get; init; }
    }
}
