namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public interface IBlobStorageRepositoryBuilder<T, TKey>
        where TKey : notnull
    {
        BlobStorageConnectionSettings Settings { get; }
    }
}
