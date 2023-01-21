using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal static class ExpressionTypeExtensions
    {
        internal static string MakeLogic(this ExpressionType type)
            => type switch
            {
                ExpressionType.And => " and ",
                ExpressionType.Or => " or ",
                ExpressionType.OrElse => " or ",
                ExpressionType.LessThan => " lt ",
                ExpressionType.LessThanOrEqual => " le ",
                ExpressionType.GreaterThan => " gt ",
                ExpressionType.GreaterThanOrEqual => " ge ",
                ExpressionType.Equal => " eq ",
                ExpressionType.NotEqual => " ne ",
                _ => " and ",
            };
        internal static bool IsRightASingleValue(this ExpressionType type)
            => type switch
            {
                ExpressionType.AndAlso or ExpressionType.And or ExpressionType.Or or ExpressionType.OrElse => false,
                _ => true,
            };
    }
}
