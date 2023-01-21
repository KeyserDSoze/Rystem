using RepositoryFramework.Migration;
using RepositoryFramework.UnitTest.Migration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.UnitTest.Migration.Storage
{
    internal class SuperMigrationFrom : IMigrationSource<SuperMigrationUser, string>
    {
        private readonly Dictionary<string, SuperMigrationUser> _users = new()
        {
            { "1", new SuperMigrationUser { Id = "1", Name = "Ale", Email = "Ale@gmail.com", IsAdmin = true } },
            { "2", new SuperMigrationUser { Id = "2", Name = "Alekud", Email = "Alu@gmail.com", IsAdmin = false } },
            { "3", new SuperMigrationUser { Id = "3", Name = "Alessia", Email = "Alo@gmail.com", IsAdmin = false } },
            { "4", new SuperMigrationUser { Id = "4", Name = "Alisandro", Email = "Ali@gmail.com", IsAdmin = false } },
        };
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
            var users = filter.Apply(_users.Select(x => x.Value));
            await foreach (var user in users.ToAsyncEnumerable())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Entity.Default(user, user.Id!);
            }
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

        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
