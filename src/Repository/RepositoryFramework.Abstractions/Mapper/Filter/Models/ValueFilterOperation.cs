namespace RepositoryFramework
{
    public record ValueFilterOperation(FilterOperations Operation, FilterRequest Request, long? Value) : FilteringOperation(Operation, Request);
}
