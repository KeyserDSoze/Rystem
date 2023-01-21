namespace RepositoryFramework
{
    public sealed class BatchResults<T, TKey>
        where TKey : notnull
    {
        public static BatchResults<T, TKey> Empty => new();
        public List<BatchResult<T, TKey>> Results { get; } = new();
        public BatchResults<T, TKey> AddInsert(TKey key, State<T, TKey> state)
        {
            Results.Add(new BatchResult<T, TKey>(CommandType.Insert, key, state));
            return this;
        }
        public BatchResults<T, TKey> AddUpdate(TKey key, State<T, TKey> state)
        {
            Results.Add(new BatchResult<T, TKey>(CommandType.Update, key, state));
            return this;
        }
        public BatchResults<T, TKey> AddDelete(TKey key, State<T, TKey> state)
        {
            Results.Add(new BatchResult<T, TKey>(CommandType.Delete, key, state));
            return this;
        }
    }
}
