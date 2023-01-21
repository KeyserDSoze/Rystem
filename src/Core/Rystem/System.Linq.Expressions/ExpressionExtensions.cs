using System.Reflection;

namespace System.Linq.Expressions
{
    public static partial class ExpressionExtensions
    {
        public static string Serialize(this Expression expression)
            => ExpressionSerializer.Serialize(expression);
        public static Expression<Func<T, TResult>> Deserialize<T, TResult>(this string expressionAsString)
            => ExpressionSerializer.Deserialize<T, TResult>(expressionAsString);
        public static Expression<Func<TResult>> Deserialize<TResult>(this string expressionAsString)
            => ExpressionSerializer.Deserialize<TResult>(expressionAsString);
        public static Func<T, TResult> DeserializeAndCompile<T, TResult>(this string expressionAsString)
            => ExpressionSerializer.Deserialize<T, TResult>(expressionAsString).Compile();
        public static Func<TResult> DeserializeAndCompile<TResult>(this string expressionAsString)
            => ExpressionSerializer.Deserialize<TResult>(expressionAsString).Compile();
        public static LambdaExpression DeserializeAsDynamic<T>(this string expressionAsString)
            => ExpressionSerializer.DeserializeAsDynamic<T>(expressionAsString);
        public static LambdaExpression DeserializeAsDynamic(this string expressionAsString, Type inputType)
            => Generics.WithStatic(typeof(ExpressionExtensions), nameof(DeserializeAsDynamic), inputType).Invoke<LambdaExpression>(expressionAsString)!;
        public static Delegate DeserializeAndCompileAsDynamic<T>(this string expressionAsString)
            => ExpressionSerializer.DeserializeAsDynamic<T>(expressionAsString).Compile();
        public static (LambdaExpression Expression, Type Type) DeserializeAsDynamicAndRetrieveType<T>(this string expressionAsString)
            => ExpressionSerializer.DeserializeAsDynamicAndRetrieveType<T>(expressionAsString);
        private static readonly List<string> PossibleNames = new()
        {
            "System.Threading.Tasks.ValueTask",
            "System.Threading.Tasks.Task"
        };
        public static async ValueTask InvokeAsync(this LambdaExpression lambdaExpression, params object[] args)
        {
            var compiledLambda = lambdaExpression.Compile();
            if (compiledLambda != null)
            {
                var executedLambda = compiledLambda.DynamicInvoke(args);
                if (executedLambda is Task task)
                    await task.NoContext();
                else if (executedLambda is ValueTask valueTask)
                    await valueTask.NoContext();
            }
        }
        public static ValueTask<object?> InvokeAsync(this LambdaExpression lambdaExpression, Type type, params object[] args)
            => lambdaExpression.Compile().InvokeAsync(type, args);
        public static async ValueTask<object?> InvokeAsync(this Delegate method, Type type, params object[] args)
        {
            var result = await Generics
                .WithStatic(typeof(ExpressionExtensions), nameof(InvokeAsync), type)
                .InvokeAsync(method, args);
            return result;
        }
        public static async ValueTask<T?> InvokeAsync<T>(this Delegate method, params object[] args)
        {
            var executedLambda = method.DynamicInvoke(args);
            if (executedLambda == null)
                return default;
            var type = executedLambda.GetType();
            var assemblyName = type.AssemblyQualifiedName;
            var assemblyBaseName = type.BaseType?.AssemblyQualifiedName;
            object? returnValue = null;
            if (assemblyName != null && assemblyBaseName != null &&
                (PossibleNames.Any(x => assemblyName.Contains(x)) || PossibleNames.Any(x => assemblyBaseName.Contains(x))))
                returnValue = await (dynamic)executedLambda;
            else
                returnValue = executedLambda;
            if (returnValue == null)
                return default;
            Type currentType = returnValue.GetType();
            Type wantedType = typeof(T);
            if (currentType == wantedType)
                return (T)returnValue;
            else
                return (T)Convert.ChangeType(returnValue, wantedType);
        }
        public static ValueTask<T?> InvokeAsync<T>(this LambdaExpression lambdaExpression, params object[] args)
            => lambdaExpression.Compile().InvokeAsync<T>(args);
        public static TResult? InvokeAndTransform<T, TSource, TResult>(this Expression<Func<T, TSource>> expression, T entity)
        {
            var value = Convert.ChangeType(expression.Compile().Invoke(entity), typeof(TResult));
            if (value == null)
                return default;
            return (TResult)value;
        }
        public static TResult? InvokeAndTransform<TResult>(this LambdaExpression expression, params object[] args)
        {
            var value = Convert.ChangeType(expression.Compile().DynamicInvoke(args), typeof(TResult));
            if (value == null)
                return default;
            return (TResult)value;
        }
        public static async ValueTask<TResult?> InvokeAndTransformAsync<TResult>(this LambdaExpression expression, params object[] args)
        {
            if (expression.ReturnType.GenericTypeArguments.Length != 1)
                return expression.InvokeAndTransform<TResult>(args);
            var value = Convert.ChangeType(await expression.InvokeAsync(expression.ReturnType.GenericTypeArguments.First(), args), typeof(TResult));
            if (value == null)
                return default;
            return (TResult)value;
        }
        public static PropertyInfo? GetPropertyFromExpression<Tx, Ty>(this Expression<Func<Tx, Ty>> lambda)
        {
            MemberExpression? expression = null;
            if (lambda.Body is UnaryExpression unary)
            {
                if (unary.Operand is MemberExpression member)
                {
                    expression = member;
                }
            }
            else if (lambda.Body is MemberExpression member)
            {
                expression = member;
            }
            return expression?.Member as PropertyInfo;
        }
    }
}