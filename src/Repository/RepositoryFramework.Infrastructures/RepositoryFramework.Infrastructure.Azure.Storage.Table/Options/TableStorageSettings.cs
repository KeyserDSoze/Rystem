namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public sealed class TableStorageSettings<T, TKey>
        where TKey : notnull
    {
        public Func<T, string> PartitionKeyFunction { get; set; } = null!;
        public Func<TKey, string> PartitionKeyFromKeyFunction { get; set; } = null!;
        public Func<T, string> RowKeyFunction { get; set; } = null!;
        public Func<TKey, string>? RowKeyFromKeyFunction { get; set; }
        public Func<T, DateTime>? TimestampFunction { get; set; }
        public string PartitionKey { get; set; } = null!;
        public string? RowKey { get; set; }
        public string? Timestamp { get; set; }
    }
}
