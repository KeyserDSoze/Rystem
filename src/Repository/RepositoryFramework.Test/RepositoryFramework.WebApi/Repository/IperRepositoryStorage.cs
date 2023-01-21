using System.Runtime.CompilerServices;

namespace RepositoryFramework.WebApi
{
    public class IperUser
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; } = null!;
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
    }
    public class IperRepositoryBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<IperUser, string>
    {
        public int Priority => 0;
        public async Task<State<IperUser, string>> BeforeInsertAsync(Entity<IperUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return true;
        }
    }
    public class IperRepositoryStorage : IRepository<IperUser, string>
    {
        public Task<BatchResults<IperUser, string>> BatchAsync(BatchOperations<IperUser, string> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<IperUser, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("dasdsada");
        }

        public Task<State<IperUser, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Saalsbury");
        }

        public Task<IperUser?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IperUser { Id = "2", Name = "d" })!;
        }

        public async Task<State<IperUser, string>> InsertAsync(string key, IperUser value, CancellationToken cancellationToken = default)
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

        public IAsyncEnumerable<Entity<IperUser, string>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<State<IperUser, string>> UpdateAsync(string key, IperUser value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return false;
        }
    }
}
