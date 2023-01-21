using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class BinaryExpressionStrategy : IExpressionStrategy
    {
        private readonly string _partitionKey;
        private readonly string? _rowKey;
        private readonly string? _timestamp;
        public BinaryExpressionStrategy(string partitionKey, string? rowKey, string? timestamp)
        {
            _partitionKey = partitionKey;
            _rowKey = rowKey;
            _timestamp = timestamp;
        }
        public string? Convert(Expression expression)
        {
            var binaryExpression = (BinaryExpression)expression;
            if (binaryExpression.NodeType.IsRightASingleValue())
            {
                dynamic leftPart = binaryExpression.Left;
                string name = leftPart.Member.Name;
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
                    var rightPart = Expression.Lambda(binaryExpression.Right).Compile().DynamicInvoke();
                    return $"{name}{binaryExpression.NodeType.MakeLogic()}{QueryStrategy.ValueToString(rightPart!)}";
                }
                else
                    return null;
            }
            return null;
        }
    }
}
