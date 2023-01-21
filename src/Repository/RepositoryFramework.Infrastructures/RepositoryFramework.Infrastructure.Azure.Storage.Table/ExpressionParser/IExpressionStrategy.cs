using System.Linq.Expressions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal interface IExpressionStrategy
    {
        string? Convert(Expression expression);
        public const string PartitionKey = "PartitionKey";
        public const string RowKey = "RowKey";
        public const string Timestamp = "Timestamp";
    }
}
