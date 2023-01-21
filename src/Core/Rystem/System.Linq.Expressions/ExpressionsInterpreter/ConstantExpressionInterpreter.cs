using System.Reflection;

namespace System.Linq.Expressions
{
    internal sealed class ConstantExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(ConstantExpression);

        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            if (bearer.Key != null && bearer.Member != null && bearer.Expression is ConstantExpression constantExpression)
                context.ReplaceWithValue(bearer.Key,
                    (bearer.Member as FieldInfo)!.GetValue(constantExpression.Value)!);
            return Array.Empty<ExpressionBearer>();
        }
    }
}