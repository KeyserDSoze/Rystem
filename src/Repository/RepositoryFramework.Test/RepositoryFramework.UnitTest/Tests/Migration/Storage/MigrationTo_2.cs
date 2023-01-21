using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RepositoryFramework.UnitTest.Migration.Models;

namespace RepositoryFramework.UnitTest.Migration.Storage
{
    internal class SuperMigrationTo : IRepository<SuperMigrationUser, string>
    {
        private static readonly Dictionary<string, SuperMigrationUser> _users = new();
        public async Task<State<SuperMigrationUser, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (_users.ContainsKey(key))
                return _users.Remove(key);
            return true;
        }

        public async Task<State<SuperMigrationUser, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _users.ContainsKey(key);
        }

        public Task<SuperMigrationUser?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_users.ContainsKey(key))
                return Task.FromResult(_users[key])!;
            return Task.FromResult(default(SuperMigrationUser));
        }

        public async Task<State<SuperMigrationUser, string>> InsertAsync(string key, SuperMigrationUser value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _users.Add(key, value);
            return true;
        }

        public async IAsyncEnumerable<Entity<SuperMigrationUser, string>> QueryAsync(IFilterExpression filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var users = filter.ApplyAsAsyncEnumerable(_users.Select(x => x.Value));
            await foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Entity.Default(user, user.Id!);
            }
        }
        public ValueTask<TProperty> OperationAsync<TProperty>(
          OperationType<TProperty> operation,
          IFilterExpression filter,
          CancellationToken cancellationToken = default)
        {
            if (operation.Name == DefaultOperations.Count)
            {
                var users = filter.Apply(_users.Select(x => x.Value));
                return ValueTask.FromResult((TProperty)(object)users.Count());
            }
            else
                throw new NotImplementedException();
        }
        public async Task<State<SuperMigrationUser, string>> UpdateAsync(string key, SuperMigrationUser value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _users[key] = value;
            return true;
        }

        public Task<BatchResults<SuperMigrationUser, string>> BatchAsync(BatchOperations<SuperMigrationUser, string> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
