namespace RepositoryFramework
{
    public record MetadataFilterOperations(FilterOperations Operation, FilterRequest Request, string Key, string Value) : FilteringOperation(Operation, Request);
}
