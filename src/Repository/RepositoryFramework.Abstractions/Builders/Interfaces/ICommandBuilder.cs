namespace RepositoryFramework
{
    public interface ICommandBuilder<T, TKey> : IRepositoryBaseBuilder<T, TKey, ICommandPattern<T, TKey>, ICommandBuilder<T, TKey>>
        where TKey : notnull
    {

    }
}
