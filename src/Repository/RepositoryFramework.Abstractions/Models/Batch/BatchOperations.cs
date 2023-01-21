using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public sealed class BatchOperations<T, TKey>
        where TKey : notnull
    {
        [JsonPropertyName("v")]
        public List<BatchOperation<T, TKey>> Values { get; init; } = new();
        public BatchOperations<T, TKey> AddInsert(TKey key, T value)
        {
            Values.Add(new BatchOperation<T, TKey>(CommandType.Insert, key, value));
            return this;
        }
        public BatchOperations<T, TKey> AddUpdate(TKey key, T value)
        {
            Values.Add(new BatchOperation<T, TKey>(CommandType.Update, key, value));
            return this;
        }
        public BatchOperations<T, TKey> AddDelete(TKey key)
        {
            Values.Add(new BatchOperation<T, TKey>(CommandType.Delete, key));
            return this;
        }
    }
}
