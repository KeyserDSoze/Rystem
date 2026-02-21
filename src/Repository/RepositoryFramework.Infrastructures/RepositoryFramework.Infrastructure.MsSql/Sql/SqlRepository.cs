using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.MsSql
{
    internal sealed class SqlRepository<T, TKey> : IRepository<T, TKey>, IAsyncDisposable, IDisposable, IServiceWithFactoryWithOptions<MsSqlOptions<T, TKey>>, IDefaultIntegration
        where TKey : notnull
    {
        public bool OptionsAlreadySetup { get; set; }
        public void SetOptions(MsSqlOptions<T, TKey> options)
        {
            _options = options;
            if (_options != null)
            {
                _connection = new SqlConnection(_options.ConnectionString);
            }
        }
        private MsSqlOptions<T, TKey>? _options;
        private SqlConnection _connection = null!;
        public SqlRepository(MsSqlOptions<T, TKey>? options = null)
        {
            _options = options;
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
            var keyAsString = _options!.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_options.Delete);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return State.Default<T, TKey>(response >= 0, default!, key);
        }

        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = _options!.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_options.Exist);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return State.Default<T, TKey>(response > 0, default!, key);
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var keyAsString = _options!.KeyIsPrimitive ? key.ToString() : key.ToJson();
            var command = await GetCommandAsync(_options.Top1);
            command.Parameters.Add(new SqlParameter("Key", keyAsString));
            var response = await command.ExecuteReaderAsync(cancellationToken);
            while (response != null && await response.ReadAsync(cancellationToken))
            {
                var entity = Activator.CreateInstance<T>();
                _options.SetEntity(response, entity);
                if (entity != null)
                    return entity;
            }
            return default;
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var parameters = _options!.SetEntityForDatabase(value);
            var command = await GetCommandAsync(string.Format(_options.Insert,
                string.Join(',', parameters.Select(x => $"[{x.ParameterName}]")),
                string.Join(',', parameters.Select(x => $"@{x.ParameterName}"))),
                parameters);
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return new State<T, TKey>(response >= 0, value, key);
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var parameters = _options!.SetEntityForDatabase(value);
            var command = await GetCommandAsync(string.Format(_options.Update,
                string.Join(',', parameters.Select(x => $"[{x.ParameterName}]=@{x.ParameterName}"))),
                parameters);
            var response = (await command.ExecuteScalarAsync(cancellationToken)).Cast<int>();
            return new State<T, TKey>(response >= 0, value, key);
        }
        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var command = await GetCommandAsync(_options!.BaseQuery);
            var response = await command.ExecuteReaderAsync(cancellationToken);
            Dictionary<T, Entity<T, TKey>> entities = new();
            while (response != null && await response.ReadAsync(cancellationToken))
            {
                var entity = Activator.CreateInstance<T>();
                var key = _options.SetEntity(response, entity);
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
        internal async Task MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync()
        {

        }

        public ValueTask DisposeAsync()
            => _connection.DisposeAsync();

        public void Dispose()
        {
            _ = DisposeAsync();
        }
        public bool FactoryNameAlreadySetup { get; set; }
        public void SetFactoryName(string name)
        {
            return;
        }

        public async ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            if (_options!.PrimaryKey == null)
                throw new ArgumentException($"Please install a key in your repository sql for table {_options.TableName}");
            using SqlConnection sqlConnection = new(_options.ConnectionString);
            sqlConnection.Open();
            var command = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName", sqlConnection);
            command.Parameters.Add(new SqlParameter("TableName", _options.TableName));
            var reader = await command.ExecuteReaderAsync();
            List<string> columns = new();
            while (reader != null && await reader.ReadAsync())
            {
                columns.Add(reader["COLUMN_NAME"].ToString()!);
            }
            await reader!.DisposeAsync();
            if (columns.Count == 0)
            {
                command = new SqlCommand("SELECT count(*) FROM sys.schemas where name=@Name", sqlConnection);
                command.Parameters.Add(new SqlParameter("Name", _options.Schema));
                var response = (await command.ExecuteScalarAsync()).Cast<int>();
                if (response <= 0)
                {
                    command = new SqlCommand($"CREATE SCHEMA {_options.Schema}", sqlConnection);
                    await command.ExecuteNonQueryAsync();
                    command = new SqlCommand("SELECT count(*) FROM sys.schemas where name=@Name", sqlConnection);
                    command.Parameters.Add(new SqlParameter("Name", _options.Schema));
                    response = (await command.ExecuteScalarAsync()).Cast<int>();
                    if (response <= 0)
                        throw new ArgumentException($"It was not possible to create a schema {_options.Schema} for table {_options.TableName}");
                }
                command = new SqlCommand(_options.GetCreationalQueryForTable(), sqlConnection);
                await command.ExecuteNonQueryAsync();
            }
            return true;
        }
    }
}
