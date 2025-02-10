using System.Collections.ObjectModel;
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
                serialized.Operations.Add(new FilterOperationAsString(operation.Operation, operation.Request, value));
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
        internal IFilterExpression Where(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.Where, request, expression));
            return this;
        }
        internal IFilterExpression Take(int top, FilterRequest request)
        {
            Operations.Add(new ValueFilterOperation(FilterOperations.Top, request, top));
            return this;
        }
        internal IFilterExpression Skip(int skip, FilterRequest request)
        {
            Operations.Add(new ValueFilterOperation(FilterOperations.Skip, request, skip));
            return this;
        }
        internal IFilterExpression OrderBy(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.OrderBy, request, expression));
            return this;
        }
        internal IFilterExpression OrderByDescending(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.OrderByDescending, request, expression));
            return this;
        }
        internal IFilterExpression ThenBy(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.ThenBy, request, expression));
            return this;
        }
        internal IFilterExpression ThenByDescending(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.ThenByDescending, request, expression));
            return this;
        }
        internal IFilterExpression GroupBy(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.GroupBy, request, expression));
            return this;
        }
        internal IFilterExpression Select(LambdaExpression expression, FilterRequest request)
        {
            if (expression != null)
                Operations.Add(new LambdaFilterOperation(FilterOperations.Select, request, expression));
            return this;
        }
        public IQueryable<T> Apply<T>(IEnumerable<T> enumerable, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(enumerable.AsQueryable(), operations);
        public IQueryable<TValue> Apply<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, FilterOperations operations = IFilterExpression.DefaultOperations)
            => Apply(dictionary.Select(x => x.Value).AsQueryable(), operations);
        public IQueryable<T> Apply<T>(IQueryable<T> queryable, FilterOperations operations = IFilterExpression.DefaultOperations)
        {
            foreach (var operation in Operations
                .Where(operation => operation.Request == FilterRequest.Entity && operations.HasFlag(operation.Operation)))
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
            foreach (var lambda in Operations.Where(x => x.Request == FilterRequest.Entity && x.Operation == FilterOperations.Select).Select(x => x as LambdaFilterOperation))
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
            foreach (var lambda in Operations.Where(x => x.Request == FilterRequest.Entity && x.Operation == FilterOperations.GroupBy).Select(x => x as LambdaFilterOperation))
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
            => (Operations.FirstOrDefault(x => x.Request == FilterRequest.Entity && x.Operation == FilterOperations.Select) as LambdaFilterOperation)?.Expression;
        public LambdaExpression? GetFirstGroupBy<T>()
            => DefaultGroupBy;
        public LambdaExpression? DefaultGroupBy
            => (Operations.FirstOrDefault(x => x.Request == FilterRequest.Entity && x.Operation == FilterOperations.GroupBy) as LambdaFilterOperation)?.Expression;
        public List<FilterExpressionValue> GetFilters(FilterOperations? filter = null, FilterRequest? request = null)
        {
            var filters = new List<FilterExpressionValue>();
            foreach (var operation in Operations)
            {
                if ((filter == null || filter == operation.Operation) && (request == null || request == operation.Request))
                {
                    var existingFilter = filters.FirstOrDefault(x => x.Operation == operation.Operation && x.Request == operation.Request);
                    if (existingFilter == null)
                    {
                        existingFilter = new FilterExpressionValue
                        {
                            Operation = operation.Operation,
                            Request = operation.Request
                        };
                        filters.Add(existingFilter);
                    }
                    if (operation is LambdaFilterOperation lambda && lambda.Expression != null)
                    {
                        ExtractValues(lambda.Expression.Body, false, existingFilter.Map, lambda.Expression.Parameters, null);
                    }
                    else if (operation is ValueFilterOperation value)
                    {
                        var key = value.Operation.ToString();
                        var expressionValue = new ExpressionValue
                        {
                            Value = value.Value != null ? (int)value.Value : 0,
                            Operation = ExpressionType.Default
                        };
                        if (!existingFilter.Map.TryAdd(key, expressionValue))
                            existingFilter.Map[key] = expressionValue;
                    }
                }
            }
            return filters;
        }
        private static void ExtractValues(Expression expression, bool fromRight, Dictionary<string, ExpressionValue> values, ReadOnlyCollection<ParameterExpression>? parameters, ExpressionType? expressionType)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    ExtractValues(binaryExpression.Left, false, values, parameters, binaryExpression.NodeType);
                    ExtractValues(binaryExpression.Right, true, values, parameters, binaryExpression.NodeType);
                    break;
                case MemberExpression memberExpression:
                    if (!fromRight)
                    {
                        var propertyName = memberExpression.Member.Name;
                        values[propertyName] = new ExpressionValue
                        {
                            Value = null,
                            Operation = expressionType ?? ExpressionType.Default
                        };
                    }
                    else
                    {
                        var propertyValue = EvaluateExpression(memberExpression);
                        AddOrReplaceLastNullValue(values, propertyValue, expressionType);
                    }
                    break;
                case ConstantExpression constantExpression:
                    AddOrReplaceLastNullValue(values, constantExpression.Value, expressionType);
                    break;
                case UnaryExpression unaryExpression:
                    ExtractValues(unaryExpression.Operand, false, values, parameters, expressionType);
                    break;
                case MethodCallExpression methodCallExpression:
                    if (!fromRight)
                    {
                        var methodName = methodCallExpression.Method.Name;
                        values[methodName] = new ExpressionValue
                        {
                            Value = null,
                            Operation = expressionType ?? ExpressionType.Default
                        };
                    }
                    else
                    {
                        var methodValue = EvaluateExpression(methodCallExpression);
                        AddOrReplaceLastNullValue(values, methodValue, expressionType);
                    }
                    break;
                default:
                    break;
            }
        }
        private static object? EvaluateExpression(Expression expression)
        {
            try
            {
                var lambda = Expression.Lambda(expression);
                var value = lambda.Compile().DynamicInvoke();
                return value;
            }
            catch
            {
                return null;
            }
        }
        private static void AddOrReplaceLastNullValue(Dictionary<string, ExpressionValue> values, object? newValue, ExpressionType? expressionType)
        {
            if (newValue != null)
            {
                var lastValueKey = values.Keys.LastOrDefault();
                if (lastValueKey != null)
                {
                    var lastValue = values[lastValueKey];
                    lastValue.Value ??= newValue;
                }
            }
        }
    }
}
