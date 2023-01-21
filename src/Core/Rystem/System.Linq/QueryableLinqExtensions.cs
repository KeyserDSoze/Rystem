using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    public static class QueryableLinqExtensions
    {
        private sealed record MethodInfoWrapper(MethodInfo Method, int GenericParametersNumber);
        private static readonly ConcurrentDictionary<string, MethodInfoWrapper> Methods = new();
        private static MethodInfo GetMethod(Type entityType, Type sourceType, string methodName, LambdaExpression? expression, bool isAsync)
        {
            int numberOfParameters = 2;
            if (expression == null && !isAsync)
                numberOfParameters = 1;
            else if (expression != null && isAsync)
                numberOfParameters = 3;
            bool expressionIsNull = expression == null;
            string returnTypeName = expression?.ReturnType.Name ?? string.Empty;
            var keyName = $"{methodName}_{sourceType.FullName}_{returnTypeName}_{numberOfParameters}";
            if (!Methods.ContainsKey(keyName))
            {
                MethodInfo? method = null;
                foreach (var m in sourceType.FetchMethods()
                 .Where(m => m.Name == methodName && m.IsGenericMethodDefinition
                 && m.GetParameters().Length == numberOfParameters
                 && (!isAsync || expressionIsNull || (m.ReturnType.Name.StartsWith("Task") || m.ReturnType.Name.StartsWith("ValueTask")))))
                {
                    if (!expressionIsNull)
                    {
                        var checkType = expression!.ReturnType;
                        var lambdaType = m.GetParameters()[!isAsync ? ^1 : ^2].ParameterType;

                        if (!lambdaType.IsTheSameTypeOrASon(typeof(Expression)) || !IsTheRightGeneric(lambdaType))
                            continue;

                        bool IsTheRightGeneric(Type toCheck)
                        {
                            if (toCheck.IsGenericParameter || checkType == toCheck)
                                return true;
                            else if (toCheck.IsGenericType && IsTheRightGeneric(toCheck.GetGenericArguments().Last()))
                                    return true;

                            return false;
                        }
                    }

                    method = m;
                    break;

                }
                if (method == null)
                    throw new InvalidOperationException($"It's not possibile to find a suitable method {methodName} in {sourceType.FullName} with {numberOfParameters} generic parameters.");
                Methods.TryAdd(keyName, new(method, method.GetGenericArguments().Length));
            }
            var methodV = Methods[keyName];
            Type[] genericParameters = methodV.GenericParametersNumber == 2 && !expressionIsNull ?
                new Type[] { entityType, expression!.ReturnType } : new Type[] { entityType };
            MethodInfo genericMethod = methodV.Method.MakeGenericMethod(genericParameters);
            return genericMethod;
        }
        public static TResult CallMethod<TSource, TResult>(this IQueryable<TSource> query, string methodName, LambdaExpression? expression = null, Type? typeWhereToSearchTheMethod = null)
        {
            if (typeWhereToSearchTheMethod == null)
                typeWhereToSearchTheMethod = typeof(Queryable);
            var newQuery = GetMethod(typeof(TSource), typeWhereToSearchTheMethod, methodName, expression, false)
                 .Invoke(null, expression != null ? new object[] { query, expression } : new object[] { query });
            if (newQuery == null)
                return default!;
            if (newQuery is IConvertible)
                return (TResult)Convert.ChangeType(newQuery!, typeof(TResult));
            else
                return (TResult)newQuery!;
        }
        public static ValueTask<TSource> CallMethodAsync<TSource>(this IQueryable<TSource> query, string methodName, CancellationToken cancellation = default)
            => CallMethodAsync<TSource, TSource>(query, methodName, cancellation);
        public static ValueTask<TResult> CallMethodAsync<TSource, TResult>(this IQueryable<TSource> query, string methodName, CancellationToken cancellation = default)
            => CallMethodAsync<TSource, TResult>(query, methodName, null, null, cancellation);
        public static ValueTask<TSource> CallMethodAsync<TSource>(this IQueryable<TSource> query, string methodName, Type? typeWhereToSearchTheMethod = null, CancellationToken cancellation = default)
            => CallMethodAsync<TSource, TSource>(query, methodName, null, typeWhereToSearchTheMethod, cancellation);
        public static ValueTask<TResult> CallMethodAsync<TSource, TResult>(this IQueryable<TSource> query, string methodName, Type? typeWhereToSearchTheMethod = null, CancellationToken cancellation = default)
            => CallMethodAsync<TSource, TResult>(query, methodName, null, typeWhereToSearchTheMethod, cancellation);
        public static async ValueTask<TResult> CallMethodAsync<TSource, TResult>(this IQueryable<TSource> query, string methodName, LambdaExpression? expression = null, Type? typeWhereToSearchTheMethod = null, CancellationToken cancellation = default)
        {
            if (typeWhereToSearchTheMethod == null)
                typeWhereToSearchTheMethod = typeof(Queryable);
            var newQuery = GetMethod(typeof(TSource), typeWhereToSearchTheMethod, methodName, expression, true)
                 .Invoke(null, expression != null ? new object[] { query, expression, cancellation } : new object[] { query, cancellation })!;
            object? result = null;
            if (newQuery is Task<TResult> task)
            {
                await task;
                result = task.Result;
            }
            else if (newQuery is ValueTask<TResult> valueTask)
            {
                await valueTask!;
                result = valueTask!.Result;
            }
            return result.Cast<TResult>()!;
        }
        public static decimal Average<TSource>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.CallMethod<TSource, decimal>(nameof(Average), selector);
        public static int Count<TSource>(this IQueryable<TSource> source, LambdaExpression predicate)
            => source.CallMethod<TSource, int>(nameof(Count), predicate);
        public static IQueryable<TSource> DistinctBy<TSource>(this IQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IQueryable<TSource>>(nameof(DistinctBy), keySelector);
        public static IQueryable<IGrouping<object, TSource>> GroupBy<TSource>(this IQueryable<TSource> source, LambdaExpression keySelector)
            => source.GroupBy<object, TSource>(keySelector).AsQueryable();
        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TKey, TSource>(this IQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IQueryable<IGrouping<TKey, TSource>>>(nameof(GroupBy), keySelector.ChangeReturnType<TKey>());
        public static long LongCount<TSource>(this IQueryable<TSource> source, LambdaExpression predicate)
            => source.CallMethod<TSource, long>(nameof(LongCount), predicate);
        public static object? Max<TSource>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.Max<TSource, object>(selector);
        public static object? Max<TSource, TResult>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.CallMethod<TSource, TResult>(nameof(Max), selector.ChangeReturnType<TResult>());
        public static object? Min<TSource>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.Min<TSource, object>(selector);
        public static object? Min<TSource, TResult>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.CallMethod<TSource, TResult>(nameof(Min), selector.ChangeReturnType<TResult>());
        public static IOrderedQueryable<TSource> OrderByDescending<TSource>(this IQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IOrderedQueryable<TSource>>(nameof(OrderByDescending), keySelector);
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IOrderedQueryable<TSource>>(nameof(OrderBy), keySelector);
        public static IQueryable<object> Select<TSource>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.Select<TSource, object>(selector);
        public static IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.CallMethod<TSource, IQueryable<TResult>>(nameof(Select), selector.ChangeReturnType<TResult>());
        public static decimal Sum<TSource>(this IQueryable<TSource> source, LambdaExpression selector)
            => source.CallMethod<TSource, decimal>(nameof(Sum), selector);
        public static IOrderedQueryable<TSource> ThenByDescending<TSource>(this IOrderedQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IOrderedQueryable<TSource>>(nameof(ThenByDescending), keySelector);
        public static IOrderedQueryable<TSource> ThenBy<TSource>(this IOrderedQueryable<TSource> source, LambdaExpression keySelector)
            => source.CallMethod<TSource, IOrderedQueryable<TSource>>(nameof(ThenBy), keySelector);
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, LambdaExpression predicate)
            => source.CallMethod<TSource, IQueryable<TSource>>(nameof(Where), predicate);
    }
}
