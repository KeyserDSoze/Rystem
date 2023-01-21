using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;

namespace RepositoryFramework.Test.Infrastructure.EntityFramework
{
    internal sealed class AppUserStorage : IRepository<AppUser, AppUserKey>
    {
        private readonly SampleContext _context;

        public AppUserStorage(SampleContext context)
        {
            _context = context;
        }

        public async Task<BatchResults<AppUser, AppUserKey>> BatchAsync(BatchOperations<AppUser, AppUserKey> operations, CancellationToken cancellationToken = default)
        {
            BatchResults<AppUser, AppUserKey> results = new();
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

        public async ValueTask<TProperty> OperationAsync<TProperty>(
          OperationType<TProperty> operation,
          IFilterExpression filter,
          CancellationToken cancellationToken = default)
        {
            var context = filter.Apply(_context.Users);
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
                var select = filter.GetFirstSelect<User>();
                result = await context.SumAsync(select!.AsExpression<User, decimal>(), cancellationToken).NoContext();
            }
            else if (operation.Name == DefaultOperations.Average)
            {
                var select = filter.GetFirstSelect<User>();
                result = await context.AverageAsync(select!.AsExpression<User, decimal>(), cancellationToken).NoContext();
            }
            return result.Cast<TProperty>() ?? default!;
        }

        public async Task<State<AppUser, AppUserKey>> DeleteAsync(AppUserKey key, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Identificativo == key.Id, cancellationToken);
            if (user != null)
            {
                _context.Users.Remove(user);
                return await _context.SaveChangesAsync(cancellationToken) > 0;
            }
            return false;
        }

        public async Task<State<AppUser, AppUserKey>> ExistAsync(AppUserKey key, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Identificativo == key.Id, cancellationToken);
            return user != null;
        }

        public async Task<AppUser?> GetAsync(AppUserKey key, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Identificativo == key.Id, cancellationToken);
            if (user != null)
                return new AppUser(user.Identificativo, user.Nome, user.IndirizzoElettronico, new(), default);
            return default;
        }

        public async Task<State<AppUser, AppUserKey>> InsertAsync(AppUserKey key, AppUser value, CancellationToken cancellationToken = default)
        {
            var user = new User
            {
                Identificativo = key.Id,
                IndirizzoElettronico = value.Email,
                Nome = value.Username,
                Cognome = string.Empty,
            };
            _context.Users.Add(user);
            return State.Default(
                await _context.SaveChangesAsync(cancellationToken) > 0,
                value with { Id = user.Identificativo },
                key);
        }

        public async IAsyncEnumerable<Entity<AppUser, AppUserKey>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var user in filter.ApplyAsAsyncEnumerable(_context.Users))
                yield return Entity.Default(new AppUser(user.Identificativo, user.Nome, user.IndirizzoElettronico, new(), default),
                    new AppUserKey(user.Identificativo));
        }

        public async Task<State<AppUser, AppUserKey>> UpdateAsync(AppUserKey key, AppUser value, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Identificativo == key.Id, cancellationToken);
            if (user != null)
            {
                user.Nome = value.Username;
                user.IndirizzoElettronico = value.Email;
                return await _context.SaveChangesAsync(cancellationToken) > 0;
            }
            return false;
        }
    }
}
