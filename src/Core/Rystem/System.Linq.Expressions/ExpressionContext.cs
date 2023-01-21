using System.Reflection;

namespace System.Linq.Expressions
{
    internal sealed record ExpressionContext
    {
        public string ExpressionAsString { get; private set; }
        public List<ArgumentValue> Arguments { get; } = new();
        public Dictionary<string, string> FinalSubstitutions { get; } = new();
        public ExpressionContext(Expression expression)
        {
            ExpressionAsString = expression.ToString();
        }
        public void ReplaceWithValue(string key, object? value)
        {
            ExpressionAsString = ExpressionAsString.Replace(key, Interpretate(value), 1);
        }
        public void DirectReplace(string key, string value)
        {
            ExpressionAsString = ExpressionAsString.Replace(key, value, 1);
        }
        public bool IsAnArgument(string name, Type? type)
            => type != null && Arguments.Any(x => x.Name == name && x.Type.IsTheSameTypeOrAParent(type));
        public void CompileAndReplace(Expression argument)
        {
            Try.WithDefaultOnCatch(() =>
            {
                var argumentKey = argument.ToString();
                var value = Expression.Lambda(argument).Compile().DynamicInvoke();
                ReplaceWithValue(argumentKey, value);
            });
        }
        public string Finalize()
        {
            foreach (var method in FinalSubstitutions
                .Where(method => ExpressionAsString.Contains(method.Value)))
            {
                ExpressionAsString = ExpressionAsString.Replace(method.Value, method.Key);
            }
            return ExpressionAsString;
        }
        private static string Interpretate(object? value)
        {
            if (value is null)
                return "null";
            if (value is string)
                return $"\"{value}\"";
            else if (value is Guid)
                return $"Guid.Parse(\"{value}\")";
            else if (value is DateTime)
                return $"Convert.ToDateTime(\"{value}\")";
            else if (value is TimeSpan timeSpan)
                return $"new TimeSpan({timeSpan.Ticks} as long)";
            else if (value is char)
                return $"'{value}'";
            else
                return value.ToString()!;
        }
    }
}