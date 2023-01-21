using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace RepositoryFramework.Infrastructure.MsSql
{
    internal sealed class SqlRepository<T, TKey> : IRepository<T, TKey>, IAsyncDisposable
        where TKey : notnull
    {
        private readonly MsSqlOptions<T, TKey> _settings;
        private readonly SqlConnection _connection;

        public SqlRepository(MsSqlOptions<T, TKey> settings)
        {
            _settings = settings;
            _connection = new SqlConnection(settings.ConnectionString);
        }
        private async Task<SqlCommand> GetCommandAsync(string text, List<SqlParameter>? collection = null)
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
            var command = new SqlCommand(text, _connection);
            if (collection != null)
                foreach (var parameter in collection)
                    command.Parameters.Add(parameter);
            return command;
        }
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = _settings.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_settings.Delete);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return response >= 0;
        }

        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = _settings.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_settings.Exist);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return response > 0;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = _settings.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_settings.Top1);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = await command.ExecuteReaderAsync(cancellationToken);
            while (response != null && await response.ReadAsync(cancellationToken))
            {
                var entity = Activator.CreateInstance<T>();
                _settings.SetEntity(response, entity);
                if (entity != null)
                    return entity;
            }
            return default;
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var parameters = _settings.SetEntityForDatabase(value, key);
            var command = await GetCommandAsync(string.Format(_settings.Insert,
                string.Join(',', parameters.Select(x => $"[{x.ParameterName}]")),
                string.Join(',', parameters.Select(x => $"@{x.ParameterName}"))),
                parameters);
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return new State<T, TKey>(response >= 0, value, key);
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var parameters = _settings.SetEntityForDatabase(value, key);
            var command = await GetCommandAsync(string.Format(_settings.Update,
                string.Join(',', parameters.Select(x => $"[{x.ParameterName}]=@{x.ParameterName}"))),
                parameters);
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return new State<T, TKey>(response >= 0, value, key);
        }
        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var command = await GetCommandAsync(_settings.BaseQuery);
            var response = await command.ExecuteReaderAsync(cancellationToken);
            Dictionary<T, Entity<T, TKey>> entities = new();
            while (response != null && await response.ReadAsync(cancellationToken))
            {
                var entity = Activator.CreateInstance<T>();
                var key = _settings.SetEntity(response, entity);
                entities.Add(entity, new(entity, key));
            }
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

        public ValueTask DisposeAsync()
            => _connection.DisposeAsync();
    }
}
