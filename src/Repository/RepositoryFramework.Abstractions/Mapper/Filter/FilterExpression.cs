using System.Linq.Expressions;

namespace RepositoryFramework
{
    public sealed class FilterExpression : IFilterExpression
    {
        public static FilterExpression Empty => new();
        public List<FilteringOperation> Operations { get; } = new();
        private SerializableFilter ToSerializableQuery()
        {
            var serialized = new SerializableFilter { Operations = new() };
            foreach (var operation in Operations)
            {
                string? value = null;
                if (operation is LambdaFilterOperation lambda)
                    value = lambda.Expression?.Serialize();
                else if (operation is ValueFilterOperation valueQueryOperation)
                    value = valueQueryOperation.Value.ToString();

                serialized.Operations.Add(new FilterOperationAsString(operation.Operation, value));
            }
            return serialized;
        }
        public SerializableFilter Serialize()
            => ToSerializableQuery();
        public string ToKey()
            => ToSerializableQuery().AsString();
        public IFilterExpression Translate(IRepositoryFilterTranslator translator)
        {
            if (translator != null)
                return ToSerializableQuery().DeserializeAndTranslate(translator);
            return this;
        }
        internal IFilterExpression Where(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.Where, expression));
            return this;
        }
        internal IFilterExpression Take(int top)
        {
            Operations.Add(new ValueFilterOperation(FilterOperations.Top, top));
            return this;
        }
        internal IFilterExpression Skip(int skip)
        {
            Operations.Add(new ValueFilterOperation(FilterOperations.Skip, skip));
            return this;
        }
        internal IFilterExpression OrderBy(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.OrderBy, expression));
            return this;
        }
        internal IFilterExpression OrderByDescending(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.OrderByDescending, expression));
            return this;
        }
        internal IFilterExpression ThenBy(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.ThenBy, expression));
            return this;
        }
        internal IFilterExpression ThenByDescending(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.ThenByDescending, expression));
            return this;
        }
        internal IFilterExpression GroupBy(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.GroupBy, expression));
            return this;
        }
        internal IFilterExpression Select(LambdaExpression expression)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.Select, expression));
            return this;
        }
        public IQueryable<T> Apply<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(enumerable.AsQueryable(), operations);
        public IQueryable<TValue> Apply<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(dictionary.Select(x => x.Value).AsQueryable(), operations);
        public IQueryable<T> Apply<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
        {
            foreach (var operation in Operations
                .Where(operation => operations.HasFlag(operation.Operation)))
            {
                if (operation is LambdaFilterOperation lambda && lambda.Expression != null)
                {
                    queryable = lambda.Operation switch
                    {
                        FilterOperations.Where => queryable.Where(lambda.Expression.AsExpression<T, bool>()).AsQueryable(),
                        FilterOperations.OrderBy => queryable.OrderBy(lambda.Expression),
                        FilterOperations.OrderByDescending => queryable.OrderByDescending(lambda.Expression),
                        FilterOperations.ThenBy => (queryable as IOrderedQueryable<T>)!.ThenBy(lambda.Expression),
                        FilterOperations.ThenByDescending => (queryable as IOrderedQueryable<T>)!.ThenByDescending(lambda.Expression),
                        _ => queryable,
                    };
                }
                else if (operation is ValueFilterOperation value)
                {
                    queryable = value.Operation switch
                    {
                        FilterOperations.Top => queryable.Take(value.Value != null ? (int)value.Value : 0).AsQueryable(),
                        FilterOperations.Skip => queryable.Skip(value.Value != null ? (int)value.Value : 0).AsQueryable(),
                        _ => queryable,
                    };
                }
            }

            return queryable;
        }
        public IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(enumerable, operations).ToAsyncEnumerable();
        public IAsyncEnumerable<T> ApplyAsAsyncEnumerable<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(queryable, operations).ToAsyncEnumerable();
        public IQueryable<dynamic> ApplyAsSelect<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
        {
            var starter = Apply(enumerable, operations);
            IQueryable<dynamic>? queryable = null;
            foreach (var lambda in Operations.Where(x => x.Operation == FilterOperations.Select).Select(x => x as LambdaFilterOperation))
                if (lambda?.Expression != null)
                    queryable = starter.Select(lambda.Expression);
            return queryable ?? starter.Select(x => (dynamic)x!);
        }
        public IQueryable<dynamic> ApplyAsSelect<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsSelect(dictionary.Select(x => x.Value), operations);

        public IQueryable<dynamic> ApplyAsSelect<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsSelect(queryable.AsEnumerable(), operations);
        public IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
        {
            var starter = Apply(enumerable, operations);
            IQueryable<IGrouping<object, T>>? queryable = null;
            foreach (var lambda in Operations.Where(x => x.Operation == FilterOperations.GroupBy).Select(x => x as LambdaFilterOperation))
                if (lambda?.Expression != null)
                    queryable = starter.GroupBy(lambda.Expression);
            return queryable ?? starter.GroupBy(x => (dynamic)x!);
        }
        public IQueryable<IGrouping<dynamic, TValue>> ApplyAsGroupBy<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsGroupBy(dictionary.Select(x => x.Value), operations);

        public IQueryable<IGrouping<dynamic, T>> ApplyAsGroupBy<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => ApplyAsGroupBy(queryable.AsEnumerable(), operations);
        public LambdaExpression? GetFirstSelect<T>()
            => DefaultSelect;
        public LambdaExpression? DefaultSelect
            => (Operations.FirstOrDefault(x => x.Operation == FilterOperations.Select) as LambdaFilterOperation)?.Expression;
        public LambdaExpression? GetFirstGroupBy<T>()
            => DefaultGroupBy;
        public LambdaExpression? DefaultGroupBy
            => (Operations.FirstOrDefault(x => x.Operation == FilterOperations.GroupBy) as LambdaFilterOperation)?.Expression;
    }
}
