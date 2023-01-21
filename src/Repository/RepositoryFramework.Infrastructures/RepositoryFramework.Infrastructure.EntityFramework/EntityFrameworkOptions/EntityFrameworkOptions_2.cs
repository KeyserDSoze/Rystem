using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace RepositoryFramework.Infrastructure.EntityFramework
{
    public sealed class EntityFrameworkOptions<T, TKey, TEntityModel, TContext>
        where TEntityModel : class
        where TKey : notnull
        where TContext : DbContext
    {
        internal static EntityFrameworkOptions<T, TKey, TEntityModel, TContext> Instance { get; } = new();
        public Func<TContext, DbSet<TEntityModel>> DbSet { get; set; } = null!;
        public Func<DbSet<TEntityModel>, IQueryable<TEntityModel>>? References { get; set; }
    }
}
