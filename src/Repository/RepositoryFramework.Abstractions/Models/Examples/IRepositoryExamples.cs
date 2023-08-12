namespace RepositoryFramework.Abstractions
{
    public interface IRepositoryExamples<T, TKey>
        where TKey : notnull
    {
        T Entity { get; }
        TKey Key { get; }
    }
}
