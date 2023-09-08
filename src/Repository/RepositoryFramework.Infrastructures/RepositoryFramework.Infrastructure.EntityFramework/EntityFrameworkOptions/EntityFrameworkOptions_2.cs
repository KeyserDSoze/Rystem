using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.EntityFramework
{
    public sealed class EntityFrameworkOptions<T, TKey, TEntityModel, TContext> : IFactoryOptions
        where TEntityModel : class
        where TKey : notnull
        where TContext : DbContext
    {
        public Func<TContext, DbSet<TEntityModel>> DbSet { get; set; } = null!;
        public Func<DbSet<TEntityModel>, IQueryable<TEntityModel>>? References { get; set; }
    }
}
