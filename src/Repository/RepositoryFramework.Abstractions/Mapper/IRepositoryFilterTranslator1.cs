namespace RepositoryFramework
{
    public interface IRepositoryFilterTranslator<T, TKey> : IRepositoryFilterTranslator
        where TKey : notnull
    {
    }
}
