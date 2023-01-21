using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal static class QueryStrategy
    {
        internal static string? Create(Expression expression, string partitionKey, string? rowKey, string? timestamp)
        {
            IExpressionStrategy expressionFactory = new BinaryExpressionStrategy(partitionKey, rowKey, timestamp);
            if (expression is MethodCallExpression)
            {
                expressionFactory = new MethodCallerExpressionStrategy(partitionKey, rowKey, timestamp);
            }
            return expressionFactory.Convert(expression);
        }
        internal static string ValueToString(object value)
        {
            if (value is string)
                return $"'{value}'";
            if (value is DateTime time)
                return $"datetime'{time:yyyy-MM-dd}T{time:HH:mm:ss}Z'";
            if (value is DateTimeOffset offset)
                return $"datetime'{offset:yyyy-MM-dd}T{offset:HH:mm:ss}Z'";
            if (value is Guid)
                return $"guid'{value}'";
            if (value is double @double)
                return @double.ToString(new System.Globalization.CultureInfo("en"));
            if (value is float single)
                return single.ToString(new System.Globalization.CultureInfo("en"));
            if (value is decimal @decimal)
                return @decimal.ToString(new System.Globalization.CultureInfo("en"));
            return $"'{value.ToJson()}'";
        }
    }
}
