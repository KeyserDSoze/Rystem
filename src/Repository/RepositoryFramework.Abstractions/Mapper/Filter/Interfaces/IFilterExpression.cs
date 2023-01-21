using System.Linq.Expressions;

namespace RepositoryFramework
{
    public interface IFilterExpression
    {
        public const FilterOperations DefaultOperations = FilterOperations.Where | FilterOperations.OrderBy |
            FilterOperations.OrderByDescending | FilterOperations.ThenBy | FilterOperations.ThenByDescending |
            FilterOperations.Top | FilterOperations.Skip;
        public static IFilterExpression Empty => FilterExpression.Empty;
        List<FilteringOperation> Operations { get; }
        SerializableFilter Serialize();
        string ToKey();
        IFilterExpression Translate(IRepositoryFilterTranslator translator);
        IQueryable<T> Apply<T>(IEnumerable<T> enumerable, FilterOperations operations = DefaultOperations);
        IQueryable<TValue> Apply<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = DefaultOperations);
        IQueryable<T> Apply<T>(IQueryable<T> queryable, FilterOperations operations = DefaultOperations);
        IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IEnumerable<T> enumerable, FilterOperations operations = DefaultOperations);
        IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IQueryable<T> queryable, FilterOperations operations = DefaultOperations);
        IQueryable<dynamic> ApplyAsSelect<T>(IEnumerable<T> enumerable, FilterOperations operations = DefaultOperations);
        IQueryable<dynamic> ApplyAsSelect<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = DefaultOperations);
        IQueryable<dynamic> ApplyAsSelect<T>(IQueryable<T> queryable, FilterOperations operations = DefaultOperations);
        IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IEnumerable<T> enumerable, FilterOperations operations = DefaultOperations);
        IQueryable<IGrouping<dynamic, TValue>> ApplyAsGroupBy<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = DefaultOperations);
        IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IQueryable<T> queryable, FilterOperations operations = DefaultOperations);
        LambdaExpression? GetFirstSelect<T>();
        LambdaExpression? DefaultSelect { get; }
        LambdaExpression? DefaultGroupBy { get; }
        LambdaExpression? GetFirstGroupBy<T>();
    }
}
