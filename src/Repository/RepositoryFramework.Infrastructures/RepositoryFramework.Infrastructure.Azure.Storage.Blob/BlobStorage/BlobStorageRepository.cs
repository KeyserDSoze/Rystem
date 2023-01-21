using Azure.Storage.Blobs;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    internal sealed class BlobStorageRepository<T, TKey> : IRepository<T, TKey>
        where TKey : notnull
    {
        private readonly BlobContainerClient _client;
        private readonly BlobStorageSettings<T, TKey>? _settings;
        public BlobStorageRepository(BlobServiceClientFactory clientFactory, BlobStorageSettings<T, TKey>? settings = null)
        {
            _client = clientFactory.Get(typeof(T).Name);
            _settings = settings;
        }
        private static string GetFileName(TKey key)
        {
            if (key is IKey keyAsString)
                return keyAsString.AsString();
            return key.ToString()!;
        }
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var response = await _client.DeleteBlobAsync(GetFileName(key), cancellationToken: cancellationToken).NoContext();
            return !response.IsError;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var blobClient = _client.GetBlobClient(GetFileName(key));
            if (await blobClient.ExistsAsync(cancellationToken).NoContext())
            {
                var blobData = await blobClient.DownloadContentAsync(cancellationToken).NoContext();
                return JsonSerializer.Deserialize<Entity<T, TKey>>(blobData.Value.Content)!.Value;
            }
            return default;
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var blobClient = _client.GetBlobClient(GetFileName(key));
            return (await blobClient.ExistsAsync(cancellationToken).NoContext()).Value;
        }

        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var blobClient = _client.GetBlobClient(GetFileName(key));
            var entityWithKey = Entity.Default(value, key);
            var response = await blobClient.UploadAsync(new BinaryData(entityWithKey.ToJson()), cancellationToken).NoContext();
            return State.Default<T, TKey>(response.Value != null, value);
        }

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Func<T, bool> predicate = x => true;
#warning to check well, check a new way to create the query
            var where = (filter.Operations.FirstOrDefault(x => x.Operation == FilterOperations.Where) as LambdaFilterOperation)?.Expression;
            if (where != null)
                predicate = where.AsExpression<T, bool>().Compile();
            Dictionary<T, Entity<T, TKey>> entities = new();
            await foreach (var blob in _client.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var blobClient = _client.GetBlobClient(blob.Name);
                var blobData = await blobClient.DownloadContentAsync(cancellationToken).NoContext();
                var item = JsonSerializer.Deserialize<Entity<T, TKey>>(blobData.Value.Content);
                if (item != null && !item.HasValue)
                    continue;
                if (!predicate.Invoke(item!.Value!))
                    continue;
                entities.Add(item.Value!, item);
            }
            foreach (var item in filter.Apply(entities.Values.Select(x => x.Value)))
                yield return entities[item!];
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var blobClient = _client.GetBlobClient(GetFileName(key));
            var entityWithKey = Entity.Default(value, key);
            var response = await blobClient.UploadAsync(new BinaryData(entityWithKey.ToJson()), true, cancellationToken).NoContext();
            return State.Default<T, TKey>(response.Value != null, value);
        }

        public async ValueTask<TProperty> OperationAsync<TProperty>(
         OperationType<TProperty> operation,
         IFilterExpression filter,
         CancellationToken cancellationToken = default)
        {
#warning to refactor
            List<T> items = new();
            await foreach (var item in QueryAsync(filter, cancellationToken))
                items.Add(item.Value!);
            var select = filter.GetFirstSelect<T>();
            return (await operation.ExecuteDefaultOperationAsync(
                () => items.Count,
                () => items.Sum(x => select!.InvokeAndTransform<decimal>(x!)!),
                () => items.Select(x => select!.InvokeAndTransform<object>(x!)).Max(),
                () => items.Select(x => select!.InvokeAndTransform<object>(x!)).Min(),
                () => items.Average(x => select!.InvokeAndTransform<decimal>(x!))))!;
        }
        public async Task<BatchResults<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
        {
            BatchResults<T, TKey> results = new();
            foreach (var operation in operations.Values)
            {
                switch (operation.Command)
                {
                    case CommandType.Delete:
                        results.AddDelete(operation.Key, await DeleteAsync(operation.Key, cancellationToken).NoContext());
                        break;
                    case CommandType.Insert:
                        results.AddInsert(operation.Key, await InsertAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                    case CommandType.Update:
                        results.AddUpdate(operation.Key, await UpdateAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                }
            }
            return results;
        }
    }
}
