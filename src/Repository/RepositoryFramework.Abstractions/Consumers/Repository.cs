using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal class Repository<T, TKey> : IRepository<T, TKey>, IServiceForFactory
        where TKey : notnull
    {
        private Lazy<Query<T, TKey>> _query;
        private Lazy<Command<T, TKey>> _command;
        private readonly IFactory<IRepositoryPattern<T, TKey>> _repositoryFactory;
        private readonly IRepositoryBusinessManager<T, TKey>? _businessManager;
        private readonly IRepositoryFilterTranslator<T, TKey>? _translator;

        public void SetFactoryName(string name)
        {
            var repository = _repositoryFactory.Create(name);
            _query = new Lazy<Query<T, TKey>>(() => new Query<T, TKey>(null, _businessManager, _translator).SetQuery(repository));
            _command = new Lazy<Command<T, TKey>>(() => new Command<T, TKey>(null, _businessManager).SetCommand(repository));
        }
        public Repository(IFactory<IRepositoryPattern<T, TKey>> repositoryFactory,
            IRepositoryBusinessManager<T, TKey>? businessManager = null,
            IRepositoryFilterTranslator<T, TKey>? translator = null)
        {
            _repositoryFactory = repositoryFactory;
            _businessManager = businessManager;
            _translator = translator;
        }
        public Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
            => _query.Value.ExistAsync(key, cancellationToken);
        public Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
            => _query.Value.GetAsync(key, cancellationToken);
        public IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
            => _query.Value.QueryAsync(filter, cancellationToken);
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
            IFilterExpression filter,
            CancellationToken cancellationToken = default)
           => _query.Value.OperationAsync(operation, filter, cancellationToken);

        public Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => _command.Value.InsertAsync(key, value, cancellationToken);
        public Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => _command.Value.UpdateAsync(key, value, cancellationToken);
        public Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
           => _command.Value.DeleteAsync(key, cancellationToken);
        public IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
            => _command.Value.BatchAsync(operations, cancellationToken);


    }
}
