namespace RepositoryFramework
{
    public record ValueFilterOperation(FilterOperations Operation, long? Value) : FilteringOperation(Operation);
}
