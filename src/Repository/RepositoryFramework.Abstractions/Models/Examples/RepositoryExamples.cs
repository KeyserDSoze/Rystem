namespace RepositoryFramework.Abstractions
{
    internal sealed class RepositoryExamples<T, TKey> : IRepositoryExamples<T, TKey>
    {
        public T Entity { get; }
        public TKey Key { get; }
        public RepositoryExamples(T entity, TKey key)
        {
            Entity = entity;
            Key = key;
        }
    }
}
