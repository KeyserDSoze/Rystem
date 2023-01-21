using System.Runtime.CompilerServices;

namespace RepositoryFramework.Cache
{
    internal class CachedQuery<T, TKey> : IQuery<T, TKey>
         where TKey : notnull
    {
        private protected readonly IQuery<T, TKey> Query;
        private protected readonly ICache<T, TKey>? Cache;
        private protected readonly CacheOptions<T, TKey> CacheOptions;
        private protected readonly IDistributedCache<T, TKey>? Distributed;
        private protected readonly DistributedCacheOptions<T, TKey> DistributedCacheOptions;
        private readonly string _cacheName;

        public CachedQuery(IQuery<T, TKey> query,
            ICache<T, TKey>? cache = null,
            CacheOptions<T, TKey>? cacheOptions = null,
            IDistributedCache<T, TKey>? distributed = null,
            DistributedCacheOptions<T, TKey>? distributedCacheOptions = null)
        {
            Query = query;
            Cache = cache;
            CacheOptions = cacheOptions ?? CacheOptions<T, TKey>.Default;
            Distributed = distributed;
            DistributedCacheOptions = distributedCacheOptions ?? DistributedCacheOptions<T, TKey>.Default;
            _cacheName = typeof(T).Name;
        }
        private string GetKeyAsString(RepositoryMethods method, TKey key)
        {
            if (key is IKey customKey)
                return $"{method}_{_cacheName}_{customKey.AsString()}";
            return $"{method}_{_cacheName}_{key}";
        }
        private protected Task RemoveExistAndGetCacheAsync(TKey key, bool inMemory, bool inDistributed, CancellationToken cancellationToken = default)
        {
            var existKeyAsString = GetKeyAsString(RepositoryMethods.Exist, key);
            var getKeyAsString = GetKeyAsString(RepositoryMethods.Get, key);
            List<Task> toDelete = new();
            if (inMemory && Cache != null)
            {
                if (CacheOptions.HasCache(RepositoryMethods.Get))
                    toDelete.Add(Cache.DeleteAsync(getKeyAsString, cancellationToken));
                if (CacheOptions.HasCache(RepositoryMethods.Exist))
                    toDelete.Add(Cache.DeleteAsync(existKeyAsString, cancellationToken));
            }
            if (inDistributed && Distributed != null)
            {
                if (DistributedCacheOptions.HasCache(RepositoryMethods.Get))
                    toDelete.Add(Distributed.DeleteAsync(getKeyAsString, cancellationToken));
                if (DistributedCacheOptions.HasCache(RepositoryMethods.Exist))
                    toDelete.Add(Distributed.DeleteAsync(existKeyAsString, cancellationToken));
            }
            return Task.WhenAll(toDelete);
        }
        private protected Task UpdateExistAndGetCacheAsync(TKey key, T value, State<T, TKey> state, bool inMemory, bool inDistributed, CancellationToken cancellationToken = default)
        {
            var existKeyAsString = GetKeyAsString(RepositoryMethods.Exist, key);
            var getKeyAsString = GetKeyAsString(RepositoryMethods.Get, key);
            List<Task> toUpdate = new();
            if (Cache != null || Distributed != null)
            {
                toUpdate.Add(SaveOnCacheAsync(getKeyAsString, value, Source.Repository,
                    inMemory && CacheOptions?.HasCache(RepositoryMethods.Get) == true,
                    inDistributed && DistributedCacheOptions?.HasCache(RepositoryMethods.Get) == true,
                    cancellationToken));
                toUpdate.Add(SaveOnCacheAsync(existKeyAsString, state, Source.Repository,
                    inMemory && CacheOptions?.HasCache(RepositoryMethods.Exist) == true,
                    inDistributed && DistributedCacheOptions?.HasCache(RepositoryMethods.Exist) == true,
                    cancellationToken));
            }
            return Task.WhenAll(toUpdate);
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(RepositoryMethods.Exist, key);
            var value = await RetrieveValueAsync(RepositoryMethods.Exist, keyAsString,
                () => Query.ExistAsync(key, cancellationToken)!,
                null, cancellationToken).NoContext();

            if (Cache != null || Distributed != null)
                await SaveOnCacheAsync(keyAsString, value.Response, value.Source,
                    CacheOptions.HasCache(RepositoryMethods.Exist),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Exist),
                    cancellationToken).NoContext();

            return value.Response!;
        }
        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(RepositoryMethods.Get, key);
            var value = await RetrieveValueAsync<T?>(RepositoryMethods.Get, keyAsString,
                () => Query.GetAsync(key, cancellationToken),
                null, cancellationToken).NoContext();

            if (Cache != null || Distributed != null)
                await SaveOnCacheAsync(keyAsString, value.Response, value.Source,
                    CacheOptions.HasCache(RepositoryMethods.Get),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Get),
                    cancellationToken).NoContext();

            return value.Response;
        }
        private static readonly List<Entity<T, TKey>> s_empty = new();
        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var keyAsString = $"{nameof(RepositoryMethods.Query)}_{_cacheName}_{filter.ToKey()}";

            var value = await RetrieveValueAsync(RepositoryMethods.Query, keyAsString,
                async () =>
                {
                    List<Entity<T, TKey>> items = new();
                    await foreach (var item in Query.QueryAsync(filter, cancellationToken)!)
                        items.Add(item);
                    return items;
                },
                null, cancellationToken).NoContext();

            if (Cache != null || Distributed != null)
                await SaveOnCacheAsync(keyAsString, value.Response, value.Source,
                    CacheOptions.HasCache(RepositoryMethods.Query),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Query),
                    cancellationToken).NoContext();

            foreach (var item in value.Response ?? s_empty)
                yield return item;
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(
            OperationType<TProperty> operation,
            IFilterExpression filter,
            CancellationToken cancellationToken = default)
        {
            var keyAsString = $"{nameof(RepositoryMethods.Operation)}_{operation.Name}_{_cacheName}_{filter.ToKey()}";

            var value = await RetrieveValueAsync(RepositoryMethods.Operation, keyAsString,
                null,
                () => Query.OperationAsync(operation, filter, cancellationToken)!, cancellationToken).NoContext();

            if (Cache != null || Distributed != null)
                await SaveOnCacheAsync(keyAsString, value.Response, value.Source,
                    CacheOptions.HasCache(RepositoryMethods.Query),
                    DistributedCacheOptions.HasCache(RepositoryMethods.Query),
                    cancellationToken).NoContext();

            return value.Response;
        }
        private Task SaveOnCacheAsync<TResponse>(string key, TResponse response, Source source, bool inMemory, bool inDistributed, CancellationToken cancellationToken)
        {
            List<Task> cacheSaverTasks = new();
            if (inMemory && Cache != null && source != Source.InMemory)
                cacheSaverTasks.Add(Cache.SetAsync(key, response, CacheOptions, cancellationToken));
            if (inDistributed && Distributed != null && source != Source.Distributed)
                cacheSaverTasks.Add(Distributed.SetAsync(key, response, DistributedCacheOptions, cancellationToken));
            return Task.WhenAll(cacheSaverTasks);
        }
        private async Task<(Source Source, TValue? Response)> RetrieveValueAsync<TValue>(
            RepositoryMethods method,
            string key,
            Func<Task<TValue?>>? action,
            Func<ValueTask<TValue?>>? actionFromValueTask,
            CancellationToken cancellationToken)
        {
            if (Cache != null && CacheOptions.HasCache(method))
            {
                var (isPresent, response) = await Cache.RetrieveAsync<TValue>(key, cancellationToken).NoContext();
                if (isPresent)
                    return (Source.InMemory, response);
            }
            if (Distributed != null && DistributedCacheOptions.HasCache(method))
            {
                var (isPresent, response) = await Distributed.RetrieveAsync<TValue>(key, cancellationToken).NoContext();
                if (isPresent)
                    return (Source.Distributed, response);
            }
            return (Source.Repository,
                action == null ?
                await actionFromValueTask!.Invoke().NoContext() :
                await action.Invoke().NoContext());
        }
        private enum Source
        {
            InMemory,
            Distributed,
            Repository
        }
    }
}
