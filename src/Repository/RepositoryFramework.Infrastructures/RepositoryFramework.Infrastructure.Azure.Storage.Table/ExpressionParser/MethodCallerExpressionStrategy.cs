using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class MethodCallerExpressionStrategy : IExpressionStrategy
    {
        private readonly string _partitionKey;
        private readonly string? _rowKey;
        private readonly string? _timestamp;
        public MethodCallerExpressionStrategy(string partitionKey, string? rowKey, string? timestamp)
        {
            _partitionKey = partitionKey;
            _rowKey = rowKey;
            _timestamp = timestamp;
        }
        public string? Convert(Expression expression)
        {
            var methodCallExpression = (MethodCallExpression)expression;
            if (methodCallExpression.NodeType.IsRightASingleValue())
            {
                dynamic argument = methodCallExpression.Arguments[0];
                string name = argument.Member.Name;
                var isEntered = true;
                if (name == _partitionKey)
                    name = IExpressionStrategy.PartitionKey;
                else if (name == _rowKey)
                    name = IExpressionStrategy.RowKey;
                else if (name == _timestamp)
                    name = IExpressionStrategy.Timestamp;
                else
                    isEntered = false;

                if (isEntered)
                {
                    var value = Expression.Lambda(methodCallExpression.Arguments[1]).Compile().DynamicInvoke();
                    return $"{name}{((ExpressionType)Enum.Parse(typeof(ExpressionType), methodCallExpression.Method.Name)).MakeLogic()}{QueryStrategy.ValueToString(value!)}";
                }
                else
                    return null;
            }
            return null;
        }
    }
}
