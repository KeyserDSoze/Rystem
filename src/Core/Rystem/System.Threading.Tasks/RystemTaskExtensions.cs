using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    public static class RystemTaskExtensions
    {
        public static ConfiguredTaskAwaitable NoContext(this Task task)
            => task.ConfigureAwait(RystemTask.WaitYourStartingThread);
        public static ConfiguredTaskAwaitable<T> NoContext<T>(this Task<T> task)
            => task.ConfigureAwait(RystemTask.WaitYourStartingThread);
        public static ConfiguredValueTaskAwaitable NoContext(this ValueTask task)
          => task.ConfigureAwait(RystemTask.WaitYourStartingThread);
        public static ConfiguredValueTaskAwaitable<T> NoContext<T>(this ValueTask<T> task)
            => task.ConfigureAwait(RystemTask.WaitYourStartingThread);
        public static ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items)
            => items.ToListAsync(CancellationToken.None);
        public static void ToResult(this Task task)
            => task.NoContext().GetAwaiter().GetResult();
        public static T ToResult<T>(this Task<T> task)
            => task.NoContext().GetAwaiter().GetResult();
        public static void ToResult(this ValueTask task)
            => task.NoContext().GetAwaiter().GetResult();
        public static T ToResult<T>(this ValueTask<T> task)
            => task.NoContext().GetAwaiter().GetResult();
    }
}