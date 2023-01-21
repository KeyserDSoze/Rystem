using System.Linq.Expressions;
using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public sealed class SearchValue<T> : ISearchValue
    {
        public string? Expression { get; private set; }
        public Expression<Func<T, bool>>? LambdaExpression { get; private set; }
        public required BaseProperty BaseProperty { get; init; }
        public void UpdateLambda(string? expression)
        {
            if (expression != null)
            {
                Expression = expression;
                LambdaExpression = expression.Deserialize<T, bool>();
            }
            else
            {
                Expression = null;
                LambdaExpression = null;
            }
        }
    }
}
