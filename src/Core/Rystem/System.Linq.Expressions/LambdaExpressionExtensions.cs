namespace System.Linq.Expressions
{
    public static class LambdaExpressionExtensions
    {
        public static LambdaExpression ChangeReturnType(this LambdaExpression expression, Type toChange)
        {
            if (expression.ReturnType != toChange)
                return Expression.Lambda(
                        Expression.Convert(expression.Body, toChange),
                        expression.Parameters);
            return expression;
        }
        public static LambdaExpression ChangeReturnType<T>(this LambdaExpression expression) 
            => expression.ChangeReturnType(typeof(T));
        public static Expression<Func<TReturn>> AsExpression<TReturn>(this LambdaExpression lambdaExpression)
        {
            lambdaExpression = lambdaExpression.ChangeReturnType<TReturn>();
            return Expression.Lambda<Func<TReturn>>(lambdaExpression.Body, lambdaExpression.Parameters);
        }

        public static Expression<Func<T, TReturn>> AsExpression<T, TReturn>(this LambdaExpression lambdaExpression)
        {
            lambdaExpression = lambdaExpression.ChangeReturnType<TReturn>();
            return Expression.Lambda<Func<T, TReturn>>(lambdaExpression.Body, lambdaExpression.Parameters);
        }

        public static Expression<Func<T, T1, TReturn>> AsExpression<T, T1, TReturn>(this LambdaExpression lambdaExpression)
        {
            lambdaExpression = lambdaExpression.ChangeReturnType<TReturn>();
            return Expression.Lambda<Func<T, T1, TReturn>>(lambdaExpression.Body, lambdaExpression.Parameters);
        }

        public static Expression<Func<T, T1, T2, TReturn>> AsExpression<T, T1, T2, TReturn>(this LambdaExpression lambdaExpression)
        {
            lambdaExpression = lambdaExpression.ChangeReturnType<TReturn>();
            return Expression.Lambda<Func<T, T1, T2, TReturn>>(lambdaExpression.Body, lambdaExpression.Parameters);
        }
    }
}
