using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    internal sealed class CosmosSqlRepository<T, TKey> : IRepository<T, TKey>, IServiceWithFactoryWithOptions<CosmosSqlClient>
        where TKey : notnull
    {
        public void SetOptions(CosmosSqlClient options)
        {
            Options = options;
        }
        private Container Client => Options!.Container;
        private PropertyInfo[] Properties => Options!.Properties;
        private readonly ICosmosSqlKeyManager<T, TKey> _keyManager;
        public CosmosSqlClient? Options { get; set; }

        public CosmosSqlRepository(ICosmosSqlKeyManager<T, TKey> keyManager,
            CosmosSqlClient? options = null)
        {
            _keyManager = keyManager;
            Options = options;
        }
        private static string GetKeyAsString(TKey key)
            => KeySettings<TKey>.Instance.AsString(key);
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(key);
            var response = await Client.DeleteItemAsync<T>(keyAsString, new PartitionKey(keyAsString), cancellationToken: cancellationToken).NoContext();
            return State.Default<T, TKey>(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(key);
            var parameterizedQuery = new QueryDefinition(query: Options!.ExistsQuery)
            .WithParameter("@id", keyAsString);
            using var filteredFeed = Client.GetItemQueryIterator<T>(queryDefinition: parameterizedQuery);
            var response = await filteredFeed.ReadNextAsync(cancellationToken);
            return State.Default<T, TKey>(response.Any());
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(key);
            try
            {
                var response = await Client.ReadItemAsync<T>(keyAsString, new PartitionKey(keyAsString), cancellationToken: cancellationToken).NoContext();
                if (response.StatusCode == HttpStatusCode.OK)
                    return response.Resource;
                return default;
            }
            catch (Exception)
            {
                return default;
            }
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(key);
            var flexible = new ExpandoObject();
            flexible.TryAdd("id", keyAsString);
            foreach (var property in Properties)
                flexible.TryAdd(property.Name, property.GetValue(value));
            var response = await Client.CreateItemAsync(flexible, new PartitionKey(keyAsString), cancellationToken: cancellationToken).NoContext();
            return State.Default<T, TKey>(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created, value);
        }
        private const FilterOperations AvailableOperations = FilterOperations.Where;
        private const FilterOperations NotAvailableOperations = FilterOperations.OrderBy | FilterOperations.Top | FilterOperations.Skip |
            FilterOperations.OrderByDescending | FilterOperations.ThenBy | FilterOperations.ThenByDescending;

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var queryable = filter.Apply(Client.GetItemLinqQueryable<T>(), AvailableOperations);
            var entities = new List<T>();
            using var iterator = queryable.ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                foreach (var item in await iterator.ReadNextAsync(cancellationToken).NoContext())
                    entities.Add(item);
            }
            foreach (var item in filter.Apply(entities, NotAvailableOperations))
                yield return Entity.Default(item, _keyManager.Read(item));
        }
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
            IFilterExpression filter,
            CancellationToken cancellationToken = default)
        {
            var queryable = filter.Apply(Client.GetItemLinqQueryable<T>());
            var select = filter.GetFirstSelect<T>();
            return operation.ExecuteDefaultOperationAsync(
                async () => (await queryable.CountAsync(cancellationToken)!).Resource,
                async () => (await queryable.Select(select!).Select(x => (decimal)x).AsQueryable().SumAsync()).Resource,
                async () => (await queryable.Select(select!).AsQueryable().MaxAsync(cancellationToken).NoContext()).Resource,
                async () => (await queryable.Select(select!).AsQueryable().MinAsync(cancellationToken).NoContext()).Resource,
                async () => (await queryable.Select(select!).Select(x => (decimal)x).AsQueryable().AverageAsync()).Resource
                )!;
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var keyAsString = GetKeyAsString(key);
            var flexible = new ExpandoObject();
            flexible.TryAdd("id", keyAsString);
            foreach (var property in Properties)
                flexible.TryAdd(property.Name, property.GetValue(value));
            var response = await Client.UpsertItemAsync(flexible, new PartitionKey(keyAsString), cancellationToken: cancellationToken).NoContext();
            return State.Default<T, TKey>(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created, value);
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
    }
}
