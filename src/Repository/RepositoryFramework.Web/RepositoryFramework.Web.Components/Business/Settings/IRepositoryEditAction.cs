namespace RepositoryFramework.Web.Components
{
    public interface IRepositoryEditAction<T, TKey>
        where TKey : notnull
    {
        string Name { get; }
        string? IconName { get; }
        ValueTask<bool> InvokeAsync(Entity<T, TKey> entity);
    }
}
