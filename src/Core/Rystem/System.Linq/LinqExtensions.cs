namespace System.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> RemoveWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var items = source.ToList();
            for (var i = 0; i < items.Count; i++)
            {
                if (predicate(items[i]))
                {
                    items.RemoveAt(i);
                    i--;
                }
            }
            return items;
        }
    }
}
