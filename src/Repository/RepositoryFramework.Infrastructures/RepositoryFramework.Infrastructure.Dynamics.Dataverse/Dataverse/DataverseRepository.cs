using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk.Query;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    internal sealed class DataverseRepository<T, TKey> : IRepository<T, TKey>, IServiceWithOptions<DataverseClientWrapper>
        where TKey : notnull
    {
        private ServiceClient _client => Options!.Client;
        public DataverseOptions<T, TKey> Settings { get; }
        public DataverseClientWrapper? Options { get; set; }
        public DataverseRepository(DataverseOptions<T, TKey> settings)
        {
            Settings = settings;
        }
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var query = new QueryExpression(Settings.LogicalTableName)
            {
                TopCount = 1,
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(LogicalOperator.And)
            };
            query.Criteria.AddCondition(Settings.LogicalPrimaryKey, ConditionOperator.Equal, Settings.KeyIsPrimitive ? key.ToString() : key.ToJson());
            var queryResult = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entityRetrieved = queryResult.Entities.FirstOrDefault();
            if (entityRetrieved != null)
            {
                await _client.DeleteAsync(Settings.LogicalTableName, entityRetrieved.Id, cancellationToken);
                return true;
            }
            return false;
        }

        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var query = new QueryExpression(Settings.LogicalTableName)
            {
                TopCount = 1,
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(LogicalOperator.And)
            };
            query.Criteria.AddCondition(Settings.LogicalPrimaryKey, ConditionOperator.Equal, Settings.KeyIsPrimitive ? key.ToString() : key.ToJson());
            var queryResult = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entityRetrieved = queryResult.Entities.FirstOrDefault();
            return entityRetrieved != null;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var query = new QueryExpression(Settings.LogicalTableName)
            {
                TopCount = 1,
                ColumnSet = Settings.ColumnSet,
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(LogicalOperator.And)
            };
            query.Criteria.AddCondition(Settings.LogicalPrimaryKey, ConditionOperator.Equal, Settings.KeyIsPrimitive ? key.ToString() : key.ToJson());
            var queryResult = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entityRetrieved = queryResult.Entities.FirstOrDefault();
            if (entityRetrieved != null)
            {
                var entity = Activator.CreateInstance<T>();
                Settings.SetEntity(entityRetrieved, entity);
                return entity;
            }
            return default;
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var dataverseEntity = new Microsoft.Xrm.Sdk.Entity(Settings.LogicalTableName);
            Settings.SetDataverseEntity(dataverseEntity, value, key);
            await _client.CreateAsync(dataverseEntity, cancellationToken);
            return new State<T, TKey>(true, value, key);
        }
        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = new QueryExpression(Settings.LogicalTableName)
            {
                TopCount = 100,
                ColumnSet = Settings.ColumnSet,
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(LogicalOperator.And)
            };
            var queryResult = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entities = queryResult.Entities.Select(x =>
            {
                var entity = Activator.CreateInstance<T>();
                var key = Settings.SetEntity(x, entity);
                return (entity, new Entity<T, TKey>(entity, key));
            }).ToDictionary(x => x.entity, x => x.Item2);
            foreach (var entity in filter.Apply(entities.Keys))
                yield return entities[entity];
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
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
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var query = new QueryExpression(Settings.LogicalTableName)
            {
                TopCount = 1,
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(LogicalOperator.And)
            };
            query.Criteria.AddCondition(Settings.LogicalPrimaryKey, ConditionOperator.Equal, Settings.KeyIsPrimitive ? key.ToString() : key.ToJson());
            var queryResult = await _client.RetrieveMultipleAsync(query, cancellationToken);
            var entityRetrieved = queryResult.Entities.FirstOrDefault();
            if (entityRetrieved != null)
            {
                var dataverseEntity = new Microsoft.Xrm.Sdk.Entity(Settings.LogicalTableName)
                {
                    Id = entityRetrieved.Id
                };
                Settings.SetDataverseEntity(dataverseEntity, value, key);
                await _client.UpdateAsync(dataverseEntity, cancellationToken);
                return new State<T, TKey>(true, value, key);
            }
            return false;
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
