namespace RepositoryFramework
{
    public sealed class FilterExpressionValue
    {
        public FilterRequest Request { get; init; }
        public FilterOperations Operation { get; init; }
        public Dictionary<string, ExpressionValue> Map { get; } = [];
    }
}
