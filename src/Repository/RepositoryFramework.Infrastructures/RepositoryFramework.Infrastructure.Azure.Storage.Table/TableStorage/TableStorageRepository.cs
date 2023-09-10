using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Table
{
    internal sealed class TableStorageRepository<T, TKey> : IRepository<T, TKey>, IServiceWithFactoryWithOptions<TableClientWrapper<T, TKey>>
        where TKey : notnull
    {
        public void SetOptions(TableClientWrapper<T, TKey> options)
        {
            _options = options;
        }
        public void SetFactoryName(string name)
        {
            _keyReader = _keyReaderFactory.Create(name);
        }
        private TableClient Client => _options!.Client;
        private TableStorageSettings<T, TKey>? Settings => _options.Settings;
        private readonly IFactory<ITableStorageKeyReader<T, TKey>> _keyReaderFactory;
        private ITableStorageKeyReader<T, TKey> _keyReader;
        private TableClientWrapper<T, TKey>? _options;
        public TableStorageRepository(
            IFactory<ITableStorageKeyReader<T, TKey>> keyReaderFactory,
            ITableStorageKeyReader<T, TKey>? keyReader = null)
        {
            _keyReaderFactory = keyReaderFactory;
            _keyReader = keyReader!;
        }
        private sealed class TableEntity : ITableEntity
        {
            public string PartitionKey { get; set; } = null!;
            public string RowKey { get; set; } = null!;
            public DateTimeOffset? Timestamp { get; set; }
            public string Value { get; set; } = null!;
            public global::Azure.ETag ETag { get; set; }
        }

        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var (partitionKey, rowKey) = _keyReader.Read(key, Settings);
            var response = await Client.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken).NoContext();
            return !response.IsError;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var (partitionKey, rowKey) = _keyReader.Read(key, Settings);
            try
            {
                var response = await Client.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken).NoContext();
                if (response?.Value != null)
                    return JsonSerializer.Deserialize<T>(response.Value.Value);
            }
            catch (RequestFailedException)
            {
                return default;
            }
            return default;
        }
        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var (partitionKey, rowKey) = _keyReader.Read(key, Settings);
            await foreach (var entity in Client.QueryAsync<TableEntity>(
                filter: $"PartitionKey eq '{partitionKey}' and RowKey eq '{rowKey}'", 1, cancellationToken: cancellationToken))
                return State.Default(true, JsonSerializer.Deserialize<T>(entity.Value)!, key);
            return false;
        }

        public Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => UpdateAsync(key, value, cancellationToken);

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var where = (filter.Operations.FirstOrDefault(x => x.Operation == FilterOperations.Where) as LambdaFilterOperation)?.Expression;
            string? filterAsString = null;
            if (where != null)
                filterAsString = QueryStrategy.Create(where.Body, Settings!.PartitionKey, Settings!.RowKey, Settings!.Timestamp);

            var top = (filter.Operations.FirstOrDefault(x => x.Operation == FilterOperations.Top) as ValueFilterOperation)?.Value;
            var skip = (filter.Operations.FirstOrDefault(x => x.Operation == FilterOperations.Skip) as ValueFilterOperation)?.Value;
            var counter = 0;
            var items = new List<T>();

            await foreach (var page in Client.QueryAsync<TableEntity>(filter: filterAsString,
                maxPerPage: 50,
                cancellationToken: cancellationToken).AsPages())
            {
                var haveToBreak = false;
                foreach (var entity in page.Values)
                {
                    counter++;
                    if (skip != null && counter <= skip)
                        continue;
                    haveToBreak = top != null && counter > top + (skip ?? 0);
                    if (haveToBreak)
                        break;
                    var item = JsonSerializer.Deserialize<T>(entity.Value)!;
                    items.Add(item);
                }
                if (haveToBreak)
                    break;
            }
            if (!cancellationToken.IsCancellationRequested)
                foreach (var item in Filter(items.AsQueryable(), filter))
                    yield return Entity.Default(item, _keyReader.Read(item, Settings));
        }
        private static IQueryable<T> Filter(IQueryable<T> queryable, IFilterExpression filter)
        {
            foreach (var operation in filter.Operations)
            {
                if (operation is LambdaFilterOperation lambda)
                {
                    queryable = lambda.Operation switch
                    {
                        FilterOperations.Where => queryable.Where(lambda.Expression!.AsExpression<T, bool>()).AsQueryable(),
                        FilterOperations.OrderBy => queryable.OrderBy(lambda.Expression!),
                        FilterOperations.OrderByDescending => queryable.OrderByDescending(lambda.Expression!),
                        FilterOperations.ThenBy => (queryable as IOrderedQueryable<T>)!.ThenBy(lambda.Expression!),
                        FilterOperations.ThenByDescending => (queryable as IOrderedQueryable<T>)!.ThenByDescending(lambda.Expression!),
                        _ => queryable,
                    };
                }
            }
            return queryable;
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(
          OperationType<TProperty> operation,
          IFilterExpression filter,
          CancellationToken cancellationToken = default)
        {
            List<T> items = new();
            await foreach (var item in QueryAsync(filter, cancellationToken))
                items.Add(item.Value!);
            var selected = filter.ApplyAsSelect(items);
            return (await operation.ExecuteDefaultOperationAsync(
                () => Invoke<TProperty>(selected.Count()),
                () => Invoke<TProperty>(selected.Sum(x => ((object)x).Cast<decimal>())),
                () => Invoke<TProperty>(selected.Max()!),
                () => Invoke<TProperty>(selected.Min()!),
                () => Invoke<TProperty>(selected.Average(x => ((object)x).Cast<decimal>()))))!;
        }
        private static ValueTask<TProperty> Invoke<TProperty>(object value)
            => ValueTask.FromResult((TProperty)Convert.ChangeType(value, typeof(TProperty)));
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var (partitionKey, rowKey) = _keyReader.Read(key, Settings);
            var response = await Client.UpsertEntityAsync(new TableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Value = JsonSerializer.Serialize(value)
            }, TableUpdateMode.Replace, cancellationToken).NoContext();
            return State.Default(!response.IsError, value, key);
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
    }
}
