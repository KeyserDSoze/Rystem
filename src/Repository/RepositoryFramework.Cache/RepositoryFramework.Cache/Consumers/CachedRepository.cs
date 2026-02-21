using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Cache
{
    internal sealed class CachedRepository<T, TKey> : CachedQuery<T, TKey>, IRepository<T, TKey>, IDecoratorService<IRepository<T, TKey>>, IDecoratorService<ICommand<T, TKey>>, IServiceForFactory, IDefaultIntegration
         where TKey : notnull
    {
        private IRepository<T, TKey>? _repository;
        private ICommand<T, TKey>? _command;
        private readonly IFactory<ICommand<T, TKey>>? _commandFactory;

        public void SetDecoratedServices(IEnumerable<IRepository<T, TKey>> services)
        {
            _repository = services.First();
            _query = services.First();
        }
        public void SetDecoratedServices(IEnumerable<ICommand<T, TKey>> services)
        {
            _command = services.First();
        }
        public bool FactoryNameAlreadySetup { get; set; }
        public new void SetFactoryName(string name)
        {
            if (QueryFactory != null && QueryFactory.Exists(name))
                _query = QueryFactory.CreateWithoutDecoration(name);
            if (_commandFactory != null && _commandFactory.Exists(name))
                _command = _commandFactory.CreateWithoutDecoration(name);
            if (RepositoryFactory != null && RepositoryFactory.Exists(name))
            {
                _repository = RepositoryFactory.CreateWithoutDecoration(name);
                if (!(QueryFactory != null && QueryFactory.Exists(name)))
                    _query = _repository;
                if (!(_commandFactory != null && _commandFactory.Exists(name)))
                    _command = _repository;
            }
        }

        public CachedRepository(IDecoratedService<IQuery<T, TKey>>? query = null,
            IDecoratedService<ICommand<T, TKey>>? command = null,
            IDecoratedService<IRepository<T, TKey>>? repository = null,
            IFactory<IQuery<T, TKey>>? queryFactory = null,
            IFactory<ICommand<T, TKey>>? commandFactory = null,
            IFactory<IRepository<T, TKey>>? repositoryFactory = null,
            ICache<T, TKey>? cache = null,
            CacheOptions<T, TKey>? cacheOptions = null,
            IDistributedCache<T, TKey>? distributed = null,
            DistributedCacheOptions<T, TKey>? distributedCacheOptions = null) :
            base(query, repository, queryFactory, repositoryFactory, cache, cacheOptions, distributed, distributedCacheOptions)
        {
            _repository = repository?.Service;
            _command = command?.Service;
            _commandFactory = commandFactory;
        }

        public async IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
        {
            await foreach (var result in (_repository ?? _command!).BatchAsync(operations, cancellationToken))
            {
                if (result.State.IsOk)
                {
                    var method = (RepositoryMethods)(int)(result.Command);
                    if ((Cache != null && CacheOptions.HasCache(method))
                        || (Distributed != null && DistributedCacheOptions.HasCache(method)))
                    {
                        var operation = operations.Values.First(x => x.Key.Equals(result.Key));
                        if (result.Command != CommandType.Delete)
                        {
                            await UpdateExistAndGetCacheAsync(operation.Key, operation.Value!,
                                await (_query ?? _repository!).ExistAsync(operation.Key, cancellationToken).NoContext(),
                                CacheOptions.HasCache(method),
                                DistributedCacheOptions.HasCache(method),
                                cancellationToken).NoContext();
                        }
                        else
                        {
                            await RemoveExistAndGetCacheAsync(operation.Key,
                                CacheOptions.HasCache(RepositoryMethods.Delete),
                                DistributedCacheOptions.HasCache(RepositoryMethods.Delete), cancellationToken).NoContext();
                        }
                    }
                }
                yield return result;
            }
        }

        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if ((Cache != null && CacheOptions.HasCache(RepositoryMethods.Delete))
                || (Distributed != null && DistributedCacheOptions.HasCache(RepositoryMethods.Delete)))
                await RemoveExistAndGetCacheAsync(key,
                    CacheOptions.HasCache(RepositoryMethods.Delete),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Delete), cancellationToken).NoContext();
            return await (_repository ?? _command!).DeleteAsync(key, cancellationToken).NoContext();
        }

        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var result = await (_repository ?? _command!).InsertAsync(key, value, cancellationToken).NoContext();
            if ((Cache != null && CacheOptions.HasCache(RepositoryMethods.Insert))
                || (Distributed != null && DistributedCacheOptions.HasCache(RepositoryMethods.Insert)))
                await UpdateExistAndGetCacheAsync(key, value,
                    await (_query ?? _repository!).ExistAsync(key, cancellationToken).NoContext(),
                    CacheOptions.HasCache(RepositoryMethods.Insert),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Insert),
                    cancellationToken).NoContext();
            return result;
        }

        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var result = await (_repository ?? _command!).UpdateAsync(key, value, cancellationToken).NoContext();
            if ((Cache != null && CacheOptions.HasCache(RepositoryMethods.Update))
                || (Distributed != null && DistributedCacheOptions.HasCache(RepositoryMethods.Update)))
                await UpdateExistAndGetCacheAsync(key, value,
                    await (_query ?? _repository!).ExistAsync(key, cancellationToken).NoContext(),
                    CacheOptions.HasCache(RepositoryMethods.Update),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Update),
                    cancellationToken).NoContext();
            return result;
        }
    }
}
