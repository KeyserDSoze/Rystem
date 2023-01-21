namespace System.Linq.Expressions
{
    internal sealed class LambdaExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(LambdaExpression);

        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            if (bearer.Expression is LambdaExpression lambdaExpression)
            {
                context.Arguments.AddRange(lambdaExpression.Parameters.Select(x => new ArgumentValue(x.ToString(), x.Type)));
                return new List<ExpressionBearer>() { new ExpressionBearer(lambdaExpression.Body) };
            }
            return Array.Empty<ExpressionBearer>();
        }
    }
}