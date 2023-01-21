namespace RepositoryFramework
{
    internal class Query<T, TKey> : IQuery<T, TKey>
        where TKey : notnull
    {
        private readonly IQueryPattern<T, TKey> _query;
        private readonly IRepositoryBusinessManager<T, TKey>? _businessManager;
        private readonly IRepositoryFilterTranslator<T, TKey>? _translator;

        public Query(IQueryPattern<T, TKey> query,
            IRepositoryBusinessManager<T, TKey>? businessManager = null,
            IRepositoryFilterTranslator<T, TKey>? translator = null)
        {
            _query = query;
            _businessManager = businessManager;
            _translator = translator;
        }

        public Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
            => _businessManager?.HasBusinessBeforeExist == true || _businessManager?.HasBusinessAfterExist == true ?
                _businessManager.ExistAsync(_query, key, cancellationToken) : _query.ExistAsync(key, cancellationToken);
        public Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
            => _businessManager?.HasBusinessBeforeGet == true || _businessManager?.HasBusinessAfterGet == true ?
                _businessManager.GetAsync(_query, key, cancellationToken) : _query.GetAsync(key, cancellationToken);
        public IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            var filterExpression = filter;
            if (_translator != null)
                filterExpression = filterExpression.Translate(_translator);
            if (_businessManager?.HasBusinessBeforeQuery == true || _businessManager?.HasBusinessAfterQuery == true)
                return _businessManager.QueryAsync(_query, filterExpression, cancellationToken);
            else
                return _query.QueryAsync(filterExpression, cancellationToken);
        }
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
            IFilterExpression filter,
            CancellationToken cancellationToken = default)
        {
            var filterExpression = filter;
            if (_translator != null)
                filterExpression = filterExpression.Translate(_translator);
            if (_businessManager?.HasBusinessBeforeOperation == true || _businessManager?.HasBusinessAfterOperation == true)
                return _businessManager.OperationAsync(_query, operation, filterExpression, cancellationToken);
            else
                return _query.OperationAsync(operation, filterExpression, cancellationToken);
        }
    }
}
