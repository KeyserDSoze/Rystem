namespace RepositoryFramework.Web
{
    public interface IRepositoryUiMapper<T, TKey>
        where TKey : notnull
    {
        void Map(IRepositoryPropertyUiHelper<T, TKey> mapper);
    }
}
