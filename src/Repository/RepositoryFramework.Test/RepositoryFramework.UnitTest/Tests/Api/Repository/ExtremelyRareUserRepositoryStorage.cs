using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Repository
{
    public class ExtremelyRareUserRepositoryStorage : IRepository<ExtremelyRareUser, string>
    {
        public Task<BatchResults<ExtremelyRareUser, string>> BatchAsync(BatchOperations<ExtremelyRareUser, string> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<ExtremelyRareUser, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("dasdsada");
        }

        public Task<State<ExtremelyRareUser, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Saalsbury");
        }

        public Task<ExtremelyRareUser?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExtremelyRareUser { Id = "2", Name = "d" })!;
        }

        public async Task<State<ExtremelyRareUser, string>> InsertAsync(string key, ExtremelyRareUser value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Entity<ExtremelyRareUser, string>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<State<ExtremelyRareUser, string>> UpdateAsync(string key, ExtremelyRareUser value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return false;
        }
    }
}
