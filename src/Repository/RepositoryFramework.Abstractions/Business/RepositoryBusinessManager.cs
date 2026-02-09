using System.Runtime.CompilerServices;

namespace RepositoryFramework
{
    internal class RepositoryBusinessManager<T, TKey> : IRepositoryBusinessManager<T, TKey>
        where TKey : notnull
    {
        private static readonly List<IRepositoryBusinessBeforeInsert<T, TKey>> s_defaultBeforeInserted = new();
        private static readonly List<IRepositoryBusinessAfterInsert<T, TKey>> s_defaultAfterInserted = new();
        private static readonly List<IRepositoryBusinessBeforeUpdate<T, TKey>> s_defaultBeforeUpdated = new();
        private static readonly List<IRepositoryBusinessAfterUpdate<T, TKey>> s_defaultAfterUpdated = new();
        private static readonly List<IRepositoryBusinessBeforeDelete<T, TKey>> s_defaultBeforeDeleted = new();
        private static readonly List<IRepositoryBusinessAfterDelete<T, TKey>> s_defaultAfterDeleted = new();
        private static readonly List<IRepositoryBusinessBeforeBatch<T, TKey>> s_defaultBeforeBatched = new();
        private static readonly List<IRepositoryBusinessAfterBatch<T, TKey>> s_defaultAfterBatched = new();
        private static readonly List<IRepositoryBusinessBeforeGet<T, TKey>> s_defaultBeforeGotten = new();
        private static readonly List<IRepositoryBusinessAfterGet<T, TKey>> s_defaultAfterGotten = new();
        private static readonly List<IRepositoryBusinessBeforeExist<T, TKey>> s_defaultBeforeExisted = new();
        private static readonly List<IRepositoryBusinessAfterExist<T, TKey>> s_defaultAfterExisted = new();
        private static readonly List<IRepositoryBusinessBeforeQuery<T, TKey>> s_defaultBeforeQueried = new();
        private static readonly List<IRepositoryBusinessAfterQuery<T, TKey>> s_defaultAfterQueried = new();
        private static readonly List<IRepositoryBusinessBeforeOperation<T, TKey>> s_defaultBeforeOperation = new();
        private static readonly List<IRepositoryBusinessAfterOperation<T, TKey>> s_defaultAfterOperation = new();

        private readonly IEnumerable<IRepositoryBusinessBeforeInsert<T, TKey>> _beforeInserted;
        private readonly IEnumerable<IRepositoryBusinessAfterInsert<T, TKey>> _afterInserted;
        private readonly IEnumerable<IRepositoryBusinessBeforeUpdate<T, TKey>> _beforeUpdated;
        private readonly IEnumerable<IRepositoryBusinessAfterUpdate<T, TKey>> _afterUpdated;
        private readonly IEnumerable<IRepositoryBusinessBeforeDelete<T, TKey>> _beforeDeleted;
        private readonly IEnumerable<IRepositoryBusinessAfterDelete<T, TKey>> _afterDeleted;
        private readonly IEnumerable<IRepositoryBusinessBeforeBatch<T, TKey>> _beforeBatched;
        private readonly IEnumerable<IRepositoryBusinessAfterBatch<T, TKey>> _afterBatched;
        private readonly IEnumerable<IRepositoryBusinessBeforeGet<T, TKey>> _beforeGotten;
        private readonly IEnumerable<IRepositoryBusinessAfterGet<T, TKey>> _afterGotten;
        private readonly IEnumerable<IRepositoryBusinessBeforeExist<T, TKey>> _beforeExisted;
        private readonly IEnumerable<IRepositoryBusinessAfterExist<T, TKey>> _afterExisted;
        private readonly IEnumerable<IRepositoryBusinessBeforeQuery<T, TKey>> _beforeQueried;
        private readonly IEnumerable<IRepositoryBusinessAfterQuery<T, TKey>> _afterQueried;
        private readonly IEnumerable<IRepositoryBusinessBeforeOperation<T, TKey>> _beforeOperation;
        private readonly IEnumerable<IRepositoryBusinessAfterOperation<T, TKey>> _afterOperation;
        public RepositoryBusinessManager(
            IEnumerable<IRepositoryBusinessBeforeInsert<T, TKey>>? beforeInserted = null,
            IEnumerable<IRepositoryBusinessAfterInsert<T, TKey>>? afterInserted = null,
            IEnumerable<IRepositoryBusinessBeforeUpdate<T, TKey>>? beforeUpdated = null,
            IEnumerable<IRepositoryBusinessAfterUpdate<T, TKey>>? afterUpdated = null,
            IEnumerable<IRepositoryBusinessBeforeDelete<T, TKey>>? beforeDeleted = null,
            IEnumerable<IRepositoryBusinessAfterDelete<T, TKey>>? afterDeleted = null,
            IEnumerable<IRepositoryBusinessBeforeBatch<T, TKey>>? beforeBatched = null,
            IEnumerable<IRepositoryBusinessAfterBatch<T, TKey>>? afterBatched = null,
            IEnumerable<IRepositoryBusinessBeforeGet<T, TKey>>? beforeGotten = null,
            IEnumerable<IRepositoryBusinessAfterGet<T, TKey>>? afterGotten = null,
            IEnumerable<IRepositoryBusinessBeforeExist<T, TKey>>? beforeExisted = null,
            IEnumerable<IRepositoryBusinessAfterExist<T, TKey>>? afterExisted = null,
            IEnumerable<IRepositoryBusinessBeforeQuery<T, TKey>>? beforeQueried = null,
            IEnumerable<IRepositoryBusinessAfterQuery<T, TKey>>? afterQueried = null,
            IEnumerable<IRepositoryBusinessBeforeOperation<T, TKey>>? beforeOperation = null,
            IEnumerable<IRepositoryBusinessAfterOperation<T, TKey>>? afterOperation = null)
        {
            _beforeInserted = beforeInserted ?? s_defaultBeforeInserted;
            _afterInserted = afterInserted ?? s_defaultAfterInserted;
            _beforeUpdated = beforeUpdated ?? s_defaultBeforeUpdated;
            _afterUpdated = afterUpdated ?? s_defaultAfterUpdated;
            _beforeDeleted = beforeDeleted ?? s_defaultBeforeDeleted;
            _afterDeleted = afterDeleted ?? s_defaultAfterDeleted;
            _beforeBatched = beforeBatched ?? s_defaultBeforeBatched;
            _afterBatched = afterBatched ?? s_defaultAfterBatched;
            _beforeGotten = beforeGotten ?? s_defaultBeforeGotten;
            _afterGotten = afterGotten ?? s_defaultAfterGotten;
            _beforeExisted = beforeExisted ?? s_defaultBeforeExisted;
            _afterExisted = afterExisted ?? s_defaultAfterExisted;
            _beforeQueried = beforeQueried ?? s_defaultBeforeQueried;
            _afterQueried = afterQueried ?? s_defaultAfterQueried;
            _beforeOperation = beforeOperation ?? s_defaultBeforeOperation;
            _afterOperation = afterOperation ?? s_defaultAfterOperation;
        }
        public bool HasBusinessBeforeInsert => _beforeInserted.Any();
        public bool HasBusinessAfterInsert => _afterInserted.Any();
        public bool HasBusinessBeforeUpdate => _beforeUpdated.Any();
        public bool HasBusinessAfterUpdate => _afterUpdated.Any();
        public bool HasBusinessBeforeDelete => _beforeDeleted.Any();
        public bool HasBusinessAfterDelete => _afterDeleted.Any();
        public bool HasBusinessBeforeBatch => _beforeBatched.Any();
        public bool HasBusinessAfterBatch => _afterBatched.Any();
        public bool HasBusinessBeforeGet => _beforeGotten.Any();
        public bool HasBusinessAfterGet => _afterGotten.Any();
        public bool HasBusinessBeforeExist => _beforeExisted.Any();
        public bool HasBusinessAfterExist => _afterExisted.Any();
        public bool HasBusinessBeforeQuery => _beforeQueried.Any();
        public bool HasBusinessAfterQuery => _afterQueried.Any();
        public bool HasBusinessBeforeOperation => _beforeOperation.Any();
        public bool HasBusinessAfterOperation => _afterOperation.Any();
        public async Task<State<T, TKey>> InsertAsync(ICommandPattern<T, TKey> command, TKey key, T value, CancellationToken cancellationToken = default)
        {
            var entity = Entity.Default(value, key);
            var result = entity.ToOkState();

            foreach (var business in _beforeInserted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.BeforeInsertAsync(result.Entity!, cancellationToken);
                CheckEntityNull(result, value, key);
                if (!result.IsOk)
                    return result;
            }

            result = await command.InsertAsync(result.Entity!.Key!, result.Entity!.Value!, cancellationToken);
            entity = result.Entity;
            CheckEntityNull(result, value, key);

            foreach (var business in _afterInserted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.AfterInsertAsync(result, result.Entity!, cancellationToken);
                CheckEntityNull(result, value, key);
            }


            return result;
        }

        private static void CheckEntityNull(State<T, TKey> result, T? startingValue, TKey startingKey)
        {
            if (!result.HasEntity)
                result.Entity = Entity.Default(startingValue!, startingKey);
            if (result.Entity!.Key == null)
                result.Entity.Key = startingKey;
            if (result.Entity.Value == null)
                result.Entity.Value = startingValue;
        }
        public async Task<State<T, TKey>> UpdateAsync(ICommandPattern<T, TKey> command, TKey key, T value, CancellationToken cancellationToken = default)
        {
            var entity = Entity.Default(value, key);
            var result = entity.ToOkState();

            foreach (var business in _beforeUpdated
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.BeforeUpdateAsync(result.Entity!, cancellationToken);
                CheckEntityNull(result, value, key);
                if (!result.IsOk)
                    return result;
            }

            result = await command.UpdateAsync(result.Entity!.Key!, result.Entity!.Value!, cancellationToken);
            entity = result.Entity!;
            CheckEntityNull(result, value, key);

            foreach (var business in _afterUpdated
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.AfterUpdateAsync(result, result.Entity!, cancellationToken);
                CheckEntityNull(result, value, key);
            }

            return result;
        }
        public async Task<State<T, TKey>> DeleteAsync(ICommandPattern<T, TKey> command, TKey key, CancellationToken cancellationToken = default)
        {
            var entity = Entity.Default(default(T)!, key);
            var result = entity.ToOkState();

            foreach (var business in _beforeDeleted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.BeforeDeleteAsync(result.Entity!.Key!, cancellationToken);
                CheckEntityNull(result, default!, key);
                if (!result.IsOk)
                    return result;
            }

            result = await command.DeleteAsync(result.Entity!.Key!, cancellationToken);
            CheckEntityNull(result, default!, key);

            foreach (var business in _afterDeleted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.AfterDeleteAsync(result, result.Entity!.Key!, cancellationToken);
                CheckEntityNull(result, default!, key);
            }

            return result;
        }
        public async IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(
            ICommandPattern<T, TKey> command,
            BatchOperations<T, TKey> operations,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var business in _beforeBatched
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                operations = await business.BeforeBatchAsync(operations, cancellationToken).NoContext();
            }

            if (operations.Values.Count > 0 && HasBusinessAfterBatch)
            {
                var afterBatchList = _afterBatched
                    .OrderBy(x => x.Priority)
                    .GroupBy(x => x.Priority)
                    .Select(x => x.First())
                    .ToList();
                await foreach (var result in command.BatchAsync(operations, cancellationToken))
                {
                    var itemResult = result;
                    foreach (var afterBatch in afterBatchList)
                        itemResult = await afterBatch.AfterBatchAsync(itemResult, operations, cancellationToken).NoContext();
                    yield return itemResult;
                }
            }
            else
            {
                await foreach (var result in command.BatchAsync(operations, cancellationToken))
                    yield return result;
            }
        }

        public async Task<State<T, TKey>> ExistAsync(IQueryPattern<T, TKey> query, TKey key, CancellationToken cancellationToken = default)
        {
            var entity = Entity.Default(default(T)!, key);
            var result = entity.ToOkState();

            foreach (var business in _beforeExisted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.BeforeExistAsync(result.Entity!.Key!, cancellationToken);
                CheckEntityNull(result, default!, key);
                if (!result.IsOk)
                    return result;
            }

            result = await query.ExistAsync(result.Entity!.Key!, cancellationToken);
            CheckEntityNull(result, default!, key);

            foreach (var business in _afterExisted
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.AfterExistAsync(result, result.Entity!.Key!, cancellationToken);
                CheckEntityNull(result, default!, key);
            }

            return result;
        }

        public async Task<T?> GetAsync(IQueryPattern<T, TKey> query, TKey key, CancellationToken cancellationToken = default)
        {
            var entity = Entity.Default(default(T)!, key);
            var result = entity.ToOkState();

            foreach (var business in _beforeGotten
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                result = await business.BeforeGetAsync(result.Entity!.Key!, cancellationToken);
                CheckEntityNull(result, default!, key);
                if (!result.IsOk)
                    return default;
            }

            var response = await query.GetAsync(result.Entity!.Key!, cancellationToken);

            foreach (var business in _afterGotten
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
            {
                response = await business.AfterGetAsync(response, result.Entity!.Key!, cancellationToken);
            }

            return response;
        }

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IQueryPattern<T, TKey> queryPattern, IFilterExpression filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var business in _beforeQueried
                .OrderBy(x => x.Priority)
                .GroupBy(x => x.Priority)
                .Select(x => x.First()))
                filter = await business.BeforeQueryAsync(filter, cancellationToken);

            if (HasBusinessAfterQuery)
            {
                var afterBusiness = _afterQueried
                            .OrderBy(x => x.Priority)
                            .GroupBy(x => x.Priority)
                            .Select(x => x.First())
                            .ToList();

                await foreach (var item in queryPattern.QueryAsync(filter, cancellationToken))
                {
                    var entity = item;
                    foreach (var business in afterBusiness)
                        entity = await business.AfterQueryAsync(entity, filter, cancellationToken).NoContext();
                    yield return entity;
                }
            }
            else
            {
                await foreach (var item in queryPattern.QueryAsync(filter, cancellationToken))
                    yield return item;
            }
        }

        public async ValueTask<TProperty> OperationAsync<TProperty>(IQueryPattern<T, TKey> queryPattern, OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            (OperationType<TProperty> Operation, IFilterExpression Filter) operationFilter = (operation, filter);

            foreach (var business in _beforeOperation
                        .OrderBy(x => x.Priority)
                        .GroupBy(x => x.Priority)
                        .Select(x => x.First()))
                operationFilter = await business.BeforeOperationAsync(operationFilter.Operation, operationFilter.Filter, cancellationToken);

            var response = await queryPattern.OperationAsync(operationFilter.Operation, operationFilter.Filter, cancellationToken);

            foreach (var business in _afterOperation
                        .OrderBy(x => x.Priority)
                        .GroupBy(x => x.Priority)
                        .Select(x => x.First()))
                response = await business.AfterOperationAsync(response, operationFilter.Operation, operationFilter.Filter, cancellationToken);

            return response;
        }
    }
}
