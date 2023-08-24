namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    internal sealed class DefaultCosmosSqlKeyManager<T, TKey> : ICosmosSqlKeyManager<T, TKey>
        where TKey : notnull
    {
        private readonly Func<T, TKey> _retrievable;
        public DefaultCosmosSqlKeyManager(Func<T, TKey> retrievable)
            => _retrievable = retrievable;

        public string AsString(TKey key)
            => KeySettings<TKey>.Instance.AsString(key);
        public TKey Read(T entity)
            => _retrievable.Invoke(entity);
    }
}
