namespace System.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> RemoveWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (var item in source)
                if (!predicate(item))
                    yield return item;
        }
        public static TSource[] RemoveWhere<TSource>(this TSource[] source, Func<TSource, bool> predicate)
        {
            var count = 0;
            var realIndex = 0;
            var length = source.Length;
            for (var i = 0; i < length; i++)
            {
                if (!predicate(source[i]))
                {
                    source[realIndex] = source[i];
                    realIndex++;
                }
                else
                    count++;
            }
            Array.Clear(source, realIndex, length - realIndex);
            Array.Resize(ref source, realIndex);
            return source;
        }
        public static int RemoveWhere<TSource>(this ICollection<TSource> source, Func<TSource, bool> predicate)
        {
            if (source is List<TSource> list)
                return list.RemoveAll(x => predicate(x));
            else if (source is ICollection<TSource> iCollection)
            {
                var count = 0;
                for (var i = 0; i < iCollection.Count; i++)
                {
                    var item = iCollection.ElementAt(i);
                    if (predicate(item))
                    {
                        iCollection.Remove(item);
                        i--;
                        count++;
                    }
                }
                return count;
            }
            else
            {
                throw new NotSupportedException($"{source.GetType().FullName} collection type is not supported.");
            }
        }
    }
}
