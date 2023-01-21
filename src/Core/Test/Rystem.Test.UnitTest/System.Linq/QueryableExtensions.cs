using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rystem.Test.UnitTest.Linq
{
    public static class QueryableExtensions
    {
        public static async Task<IQueryable<T>> GetAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0).NoContext();
            if (cancellationToken.IsCancellationRequested)
                return default!;
            return queryable;
        }
        public static async Task<IQueryable<T>> GetAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0).NoContext();
            if (cancellationToken.IsCancellationRequested)
                return default!;
            return queryable.Where(expression);
        }
    }
}
