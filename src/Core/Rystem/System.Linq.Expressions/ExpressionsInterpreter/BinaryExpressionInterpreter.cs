namespace System.Linq.Expressions
{
    internal sealed class BinaryExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(BinaryExpression);
        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            var expressions = new List<ExpressionBearer>();
            if (bearer.Expression is BinaryExpression binaryExpression)
            {
                expressions.Add(new(binaryExpression.Left));
                expressions.Add(new(binaryExpression.Right));
            }
            return expressions;
        }
    }
}