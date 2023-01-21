using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal sealed class MethodCallExpressionInterpreter : IExpressionInterpreter
    {
        public Type Type { get; } = typeof(MethodCallExpression);

        public IEnumerable<ExpressionBearer> Read(ExpressionBearer bearer, ExpressionContext context)
        {
            List<ExpressionBearer> expressions = new();
            if (bearer.Expression is MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.Method.Attributes.HasFlag(MethodAttributes.Static)
                    && methodCallExpression.Method.DeclaringType != null
                    && !methodCallExpression.Method.IsDefined(typeof(ExtensionAttribute), true))
                {
                    var key = $"{methodCallExpression.Method.DeclaringType.Name}.{methodCallExpression.Method.Name}";
                    if (!context.FinalSubstitutions.ContainsKey(key))
                        context.FinalSubstitutions.Add(key, methodCallExpression.Method.Name);
                }
                if (methodCallExpression.Arguments.Count > 0)
                    foreach (var argument in methodCallExpression.Arguments)
                    {
                        if (argument is Expression expression)
                            expressions.Add(new(expression));
                        else
                            context.CompileAndReplace(argument);
                    }
                else
                    context.CompileAndReplace(methodCallExpression);
            }
            return expressions;
        }
    }
}