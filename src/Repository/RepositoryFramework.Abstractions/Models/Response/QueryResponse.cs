namespace RepositoryFramework
{
    public sealed record QueryResponse<T>(IAsyncEnumerable<T> Items, ValueTask<decimal> NumericResponse)
    {
        public QueryResponse(IAsyncEnumerable<T> items) : this(items, ValueTask.FromResult(0M)) { }
        public QueryResponse(ValueTask<decimal> numericResponse) : this(AsyncEnumerable.Empty<T>(), numericResponse) { }
    }
}