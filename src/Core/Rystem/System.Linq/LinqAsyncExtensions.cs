using System.Linq.Expressions;

namespace System.Linq
{
    public static class LinqAsyncExtensions
    {
        public static async ValueTask<bool> AllAsync<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, ValueTask<bool>>> expression)
        {
            var func = expression.Compile();
            foreach (var item in source)
                if (!await func(item))
                    return false;
            return true;
        }
        public static async ValueTask<bool> AllAsync<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, Task<bool>>> expression)
        {
            var func = expression.Compile();
            foreach (var item in source)
                if (!await func(item))
                    return false;
            return true;
        }
        public static async ValueTask<bool> AnyAsync<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, ValueTask<bool>>> expression)
        {
            var func = expression.Compile();
            foreach (var item in source)
                if (await func(item))
                    return true;
            return false;
        }
        public static async ValueTask<bool> AnyAsync<TSource>(this IEnumerable<TSource> source, Expression<Func<TSource, Task<bool>>> expression)
        {
            var func = expression.Compile();
            foreach (var item in source)
                if (await func(item))
                    return true;
            return false;
        }
    }
}
