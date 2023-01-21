using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.EntityFramework
{
    internal sealed class EntityFrameworkRepository<T, TKey, TEntityModel, TContext> : IRepository<T, TKey>
        where TEntityModel : class
        where TKey : notnull
        where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly IRepositoryMapper<T, TKey, TEntityModel> _mapper;
        private readonly DbSet<TEntityModel> _dbSet;
        private readonly IQueryable<TEntityModel> _includingDbSet;
        public EntityFrameworkRepository(
            IServiceProvider serviceProvider,
            EntityFrameworkOptions<T, TKey, TEntityModel, TContext> settings,
            IRepositoryMapper<T, TKey, TEntityModel> mapper)
        {
            _mapper = mapper;
            _context = serviceProvider.GetService<TContext>()!;
            _dbSet = settings.DbSet(_context);
            _includingDbSet = settings.References != null ? settings.References(_dbSet) : _dbSet;
        }
        public async Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { key },
                cancellationToken: cancellationToken);
            if (entity != null)
            {
                _context.Remove(entity);
                return await _context.SaveChangesAsync(cancellationToken) > 0;
            }
            return false;
        }

        public async Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet.AnyAsync(_mapper.FindById(key), cancellationToken);
            return result;
        }

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await _includingDbSet.FirstOrDefaultAsync(_mapper.FindById(key), cancellationToken);
            return _mapper.Map(entity);
        }
        public async Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.AddAsync(_mapper.Map(value, key)!, cancellationToken);
            var enteredEntity = _mapper.Map(entity.Entity);
            var enteredKey = _mapper.RetrieveKey(entity.Entity);
            return new State<T, TKey>(await _context.SaveChangesAsync(cancellationToken) > 0, enteredEntity, enteredKey);
        }
        public async Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
        {
            _dbSet!.Update(_mapper.Map(value, key)!);
            return new State<T, TKey>(await _context.SaveChangesAsync(cancellationToken) > 0, value, key);
        }
        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entity in filter.ApplyAsAsyncEnumerable(_includingDbSet))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entityFromDatabase = _mapper.Map(entity);
                var keyFromDatabase = _mapper.RetrieveKey(entity);
                yield return new Entity<T, TKey>(entityFromDatabase, keyFromDatabase);
            }
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation,
            IFilterExpression filter,
            CancellationToken cancellationToken = default)
        {
            var context = filter.Apply(_dbSet);
            object? result = null;
            if (operation.Name == DefaultOperations.Count)
            {
                result = await context.CountAsync(cancellationToken);
            }
            else if (operation.Name == DefaultOperations.Min)
            {
                result = await filter.ApplyAsSelect(context).MinAsync(cancellationToken).NoContext();
            }
            else if (operation.Name == DefaultOperations.Max)
            {
                result = await filter.ApplyAsSelect(context).MaxAsync(cancellationToken).NoContext();
            }
            else if (operation.Name == DefaultOperations.Sum)
            {
                var select = filter.GetFirstSelect<TEntityModel>();
                result = await context.SumAsync(select!.AsExpression<TEntityModel, decimal>(), cancellationToken).NoContext();
            }
            else if (operation.Name == DefaultOperations.Average)
            {
                var select = filter.GetFirstSelect<TEntityModel>();
                result = await context.AverageAsync(select!.AsExpression<TEntityModel, decimal>(), cancellationToken).NoContext();
            }
            return result.Cast<TProperty>() ?? default!;
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
