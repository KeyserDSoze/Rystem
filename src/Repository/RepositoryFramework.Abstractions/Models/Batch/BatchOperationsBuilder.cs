namespace RepositoryFramework
{
    public sealed class BatchOperationsBuilder<T, TKey>
        where TKey : notnull
    {
        private readonly BatchOperations<T, TKey> _batchOperations = new();
        private readonly ICommandPattern<T, TKey>? _command;
        internal BatchOperationsBuilder(ICommandPattern<T, TKey>? command)
        {
            _command = command;
        }
        public BatchOperationsBuilder<T, TKey> AddInsert(TKey key, T value)
        {
            _batchOperations.AddInsert(key, value);
            return this;
        }
        public BatchOperationsBuilder<T, TKey> AddUpdate(TKey key, T value)
        {
            _batchOperations.AddUpdate(key, value);
            return this;
        }
        public BatchOperationsBuilder<T, TKey> AddDelete(TKey key)
        {
            _batchOperations.AddDelete(key);
            return this;
        }
        public Task<BatchResults<T, TKey>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_batchOperations.Values.Count > 0 && _command != null)
                return _command.BatchAsync(_batchOperations, cancellationToken);
            else
                return Task.FromResult(BatchResults<T, TKey>.Empty);
        }
    }
}
