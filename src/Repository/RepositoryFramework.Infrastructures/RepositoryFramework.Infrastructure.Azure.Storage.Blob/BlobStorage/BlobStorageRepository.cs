using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    internal sealed class BlobStorageRepository<T, TKey> : IRepository<T, TKey>, IServiceWithFactoryWithOptions<BlobContainerClientWrapper>
        where TKey : notnull
    {
        public void SetOptions(BlobContainerClientWrapper options)
        {
            Options = options;
        }
        public BlobContainerClientWrapper? Options { get; set; }
        public BlobStorageRepository(BlobContainerClientWrapper? options = null)
        {
            Options = options;
        }
        private string GetFileName(TKey key)
            => $"{Options?.Prefix}{KeySettings<TKey>.Instance.AsString(key)}";
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var response = await Options!.Client.DeleteBlobAsync(GetFileName(key), cancellationToken: cancellationToken).NoContext();
            return !response.IsError;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var blobClient = Options!.Client.GetBlobClient(GetFileName(key));
            if (await blobClient.ExistsAsync(cancellationToken).NoContext())
            {
                var blobData = await blobClient.DownloadContentAsync(cancellationToken).NoContext();
                return JsonSerializer.Deserialize<Entity<T, TKey>>(blobData.Value.Content)!.Value;
            }
            return default;
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var blobClient = Options!.Client.GetBlobClient(GetFileName(key));
            return (await blobClient.ExistsAsync(cancellationToken).NoContext()).Value;
        }

        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var blobClient = Options!.Client.GetBlobClient(GetFileName(key));
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
            var entities = new Dictionary<T, Entity<T, TKey>>();
            await foreach (var blob in Options!.Client.GetBlobsAsync(prefix: Options?.Prefix, cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var blobClient = Options!.Client.GetBlobClient(blob.Name);
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
            var blobClient = Options!.Client.GetBlobClient(GetFileName(key));
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
        public async IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var operation in operations.Values)
            {
                switch (operation.Command)
                {
                    case CommandType.Delete:
                        yield return BatchResult<T, TKey>.CreateDelete(operation.Key, await DeleteAsync(operation.Key, cancellationToken).NoContext());
                        break;
                    case CommandType.Insert:
                        yield return BatchResult<T, TKey>.CreateInsert(operation.Key, await InsertAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                    case CommandType.Update:
                        yield return BatchResult<T, TKey>.CreateUpdate(operation.Key, await UpdateAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                }
            }
        }
        public void SetFactoryName(string name)
        {
            return;
        }
        //todo: implement this method and avoid the use of the AddFactoryAsync and the IOptionsBuilderAsync
        public ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);
    }
}
