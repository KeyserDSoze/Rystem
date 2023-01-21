namespace RepositoryFramework
{
    /// <summary>
    /// Interface for your Repository pattern, with Command and Query methods.
    /// This is the interface injected by the framework and that you may use for your purpose.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    public interface IRepository<T, TKey> : IRepositoryPattern<T, TKey>, IQuery<T, TKey>, ICommand<T, TKey>
        where TKey : notnull
    {

    }
}
