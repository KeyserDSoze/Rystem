using System.Linq.Expressions;

namespace RepositoryFramework
{
    public sealed class ExpressionValue
    {
        public object? Value { get; set; }
        public ExpressionType Operation { get; set; }
    }
}
