namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    public sealed class TableStorageSettings<T, TKey>
        where TKey : notnull
    {
        public Func<T, string> PartitionKeyFunction { get; internal set; } = null!;
        public Func<TKey, string> PartitionKeyFromKeyFunction { get; internal set; } = null!;
        public Func<T, string> RowKeyFunction { get; internal set; } = null!;
        public Func<TKey, string>? RowKeyFromKeyFunction { get; internal set; }
        public Func<T, DateTime>? TimestampFunction { get; internal set; }
        public string PartitionKey { get; internal set; } = null!;
        public string? RowKey { get; internal set; }
        public string? Timestamp { get; internal set; }
    }
}
