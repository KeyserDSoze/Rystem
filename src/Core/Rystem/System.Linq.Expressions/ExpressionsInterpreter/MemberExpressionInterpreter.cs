namespace System.Linq.Expressions
{
    internal sealed class MemberExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(MemberExpression);

        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            var expressions = new List<ExpressionBearer>();
            if (bearer.Expression is MemberExpression memberExpression)
            {
                var memberExpressionAsString = memberExpression.ToString();
                string name = memberExpressionAsString.Split('.').First();
                if (!context.IsAnArgument(name, memberExpression.Member.DeclaringType))
                {
                    if (memberExpression.Expression == null || name.StartsWith("value("))
                        context.CompileAndReplace(memberExpression);
                    else
                        expressions.Add(new(memberExpression.Expression)
                        {
                            Key = memberExpressionAsString,
                            Member = memberExpression.Member,
                        });
                }
            }
            return expressions;
        }
    }
}