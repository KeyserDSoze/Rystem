namespace RepositoryFramework
{
    public record Page<T, TKey>(List<Entity<T, TKey>> Items, long TotalCount, long Pages)
        where TKey : notnull;
}
