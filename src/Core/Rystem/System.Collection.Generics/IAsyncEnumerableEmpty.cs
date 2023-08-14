namespace System.Collection.Generics
{
    public static class AsyncEnumerable<T>
    {
        public static readonly IAsyncEnumerable<T> Empty = GetEmpty();
        private static async IAsyncEnumerable<T> GetEmpty()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
