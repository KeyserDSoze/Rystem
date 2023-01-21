using System.Linq.Expressions;

namespace RepositoryFramework
{
    public sealed class MultipleFilterExpression : IFilterExpression
    {
        public IFilterExpression FilterByType<T>()
        {
            var name = typeof(T).FullName!;
            if (Filters.TryGetValue(name, out var value))
                return value;
            return FilterByDefault();
        }
        public IFilterExpression FilterByDefault()
        {
            if (Filters.Count > 0)
                return Filters.First().Value;
            else
                return IFilterExpression.Empty;
        }
        public List<FilteringOperation> Operations { get; } = new();
        public SerializableFilter Serialize()
            => FilterByDefault().Serialize();
        public string ToKey()
            => FilterByDefault().ToKey();
        public IFilterExpression Translate(IRepositoryFilterTranslator translator)
            => FilterByDefault().Translate(translator);
        public Dictionary<string, FilterExpression> Filters { get; } = new();
        public IQueryable<T> Apply<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(enumerable.AsQueryable(), operations);
        public IQueryable<TValue> Apply<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(dictionary.Select(x => x.Value).AsQueryable(), operations);
        public IQueryable<T> Apply<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => FilterByType<T>().Apply(queryable, operations);
        public IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => FilterByType<T>().ApplyAsAsyncEnumerable(enumerable, operations);
        public IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => FilterByType<T>().ApplyAsAsyncEnumerable(queryable, operations);
        public IQueryable<dynamic> ApplyAsSelect<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => FilterByType<T>().ApplyAsSelect(enumerable, operations);
        public IQueryable<dynamic> ApplyAsSelect<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsSelect(dictionary.Select(x => x.Value), operations);
        public IQueryable<dynamic> ApplyAsSelect<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsSelect(queryable.AsEnumerable(), operations);
        public IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
          => FilterByType<T>().ApplyAsGroupBy(enumerable, operations);
        public IQueryable<IGrouping<dynamic, TValue>> ApplyAsGroupBy<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsGroupBy(dictionary.Select(x => x.Value), operations);
        public IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsGroupBy(queryable.AsEnumerable(), operations);
        public LambdaExpression? GetFirstSelect<T>()
            => FilterByType<T>().GetFirstSelect<T>();
        public LambdaExpression? DefaultSelect
            => FilterByDefault().DefaultSelect;
        public LambdaExpression? GetFirstGroupBy<T>()
            => FilterByType<T>().GetFirstGroupBy<T>();
        public LambdaExpression? DefaultGroupBy
            => FilterByDefault().DefaultGroupBy;
    }
}
