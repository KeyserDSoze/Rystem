namespace RepositoryFramework.Cache
{
    internal sealed class CachedRepository<T, TKey> : CachedQuery<T, TKey>, IRepository<T, TKey>
         where TKey : notnull
    {
        private readonly IRepository<T, TKey>? _repository;
        private readonly ICommand<T, TKey>? _command;

        public CachedRepository(IRepository<T, TKey>? repository = null,
            ICommand<T, TKey>? command = null,
            IQuery<T, TKey>? query = null,
            ICache<T, TKey>? cache = null,
            CacheOptions<T, TKey>? cacheOptions = null,
            IDistributedCache<T, TKey>? distributed = null,
            DistributedCacheOptions<T, TKey>? distributedCacheOptions = null) :
            base(repository ?? query!, cache, cacheOptions, distributed, distributedCacheOptions)
        {
            _repository = repository;
            _command = command;
        }
        public async Task<BatchResults<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
        {
            var results = await (_repository ?? _command!).BatchAsync(operations, cancellationToken).NoContext();
            foreach (var result in results.Results)
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
                                await (Query ?? _repository!).ExistAsync(operation.Key, cancellationToken).NoContext(),
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
            }
            return results;
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
                    await (Query ?? _repository!).ExistAsync(key, cancellationToken).NoContext(),
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
                    await (Query ?? _repository!).ExistAsync(key, cancellationToken).NoContext(),
                    CacheOptions.HasCache(RepositoryMethods.Update),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Update),
                    cancellationToken).NoContext();
            return result;
        }
    }
}
