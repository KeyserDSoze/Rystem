namespace RepositoryFramework
{
    /// <summary>
    /// Interface for your CQRS pattern, with Get, Query, Operation (like Count, Sum, Max) and Exist methods.
    /// This is the interface that you need to extend if you want to create your query pattern.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to retrieve your data from repository.</typeparam>
    public interface IQuery<T, TKey> : IQueryPattern<T, TKey>
        where TKey : notnull
    {

    }
}
