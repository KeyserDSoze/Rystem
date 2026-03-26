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
            if (expression is not BinaryExpression binaryExpression)
                return null;
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

            // For AndAlso/And: try to extract a server-side PartitionKey/RowKey filter from either
            // branch. The other branch will be evaluated in-memory. This ensures that expressions
            // like "x => !x.IsPublic && x.UserId == userId" still push a PartitionKey filter to
            // Azure Table Storage instead of doing a full-table scan.
            // OrElse is intentionally NOT handled here because filtering by one side of an OR would
            // exclude valid rows from the other side.
            if (binaryExpression.NodeType is ExpressionType.AndAlso or ExpressionType.And)
            {
                var leftFilter = Convert(binaryExpression.Left);
                if (leftFilter != null) return leftFilter;
                return Convert(binaryExpression.Right);
            }

            return null;
        }
    }
}
