namespace System.Linq.Expressions
{
    internal sealed class ParameterExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(ParameterExpression);

        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            //if (parameterExpression.IsByRef)
            //{
            //}
            return Array.Empty<ExpressionBearer>();
        }
    }
}